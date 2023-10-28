using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Trie;

public abstract class Trie<TValue>
{
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value);
}

internal abstract class InternalTrie<TValue, TCase>
    : Trie<TValue> 
    where TCase : struct, InternalTrie<TValue, TCase>.IGenericSpecializedWrapper
{
    private readonly struct TrieEntry(
        int skipLength,
        int continuationIndex,
        int continuationIndexRShift,
        int continuationIndexMask,
        int resultIndex
    )
    {
        public readonly int SkipLength = skipLength;
        public readonly int ContinuationIndex = continuationIndex; // -1 for leaf node
        public readonly int ContinuationIndexRShift = continuationIndexRShift;
        public readonly int ContinuationIndexMask = continuationIndexMask;
        public readonly int ResultIndex = resultIndex;
    }

    private readonly TrieEntry[] _trie;
    private readonly string[] _keys;
    private readonly TValue[] _values;
    private readonly int _minLength = int.MaxValue;
    private readonly int _maxLength;
    private readonly ulong _lengthFilter;

    protected InternalTrie(Dictionary<string, TValue> values)
    {
        _keys = values.Keys.ToArray();
        _values = values.Values.ToArray();
        
        foreach (var value in values)
        {
            var keyLength = value.Key.Length;
            if (keyLength < _minLength) _minLength = keyLength;
            if (keyLength > _maxLength) _maxLength = keyLength;
            _lengthFilter |= 1ul << (keyLength & 63);
        }

        List<TrieEntry> trie = new() { new TrieEntry() };

        var entries = _keys.Select((k, i) => (i, k)).ToList();
        trie[0] = BuildTrieEntry(entries, 0, trie, isRoot: true);

        _trie = trie.ToArray();
    }

    private TrieEntry BuildTrieEntry(List<(int index, string key)> entries, int comparisonIndex, List<TrieEntry> trie, bool isRoot)
    {
        if (entries.Count == 1)
        {
            return new TrieEntry(
                skipLength: 1,
                continuationIndex: -1,
                continuationIndexRShift: 0,
                continuationIndexMask: -1,
                resultIndex: entries[0].index
                );
        }

        var nextDifferentCharIndex = comparisonIndex;
        if (isRoot)
        {
            // Specifically for the root entry, we don't know that the first characters
            // are all equal like we do for every other node.
            // However starting at 1 allows us to guarantee progression of the algorithm.
            // Thus we compromise - only start at 0 for the root node (more for guaranteeing
            // termination, than performance).
            nextDifferentCharIndex--;
        }

        var terminalEntryIndex = -1;
        var foundNextDifferentCharIndex = false;
        while (!foundNextDifferentCharIndex)
        {
            nextDifferentCharIndex++;

            if (entries[0].key.Length == nextDifferentCharIndex)
            {
                terminalEntryIndex = 0;
                break;
            }

            var c = TCase.CaseTransform(entries[0].key[nextDifferentCharIndex]);
            for (var i = 1; i < entries.Count; i++)
            {
                var entryKey = entries[i].key;
                if (nextDifferentCharIndex == entryKey.Length)
                {
                    terminalEntryIndex = i;
                    foundNextDifferentCharIndex = true;
                    break;
                }

                if (TCase.CaseTransform(entryKey[nextDifferentCharIndex]) != TCase.CaseTransform(c))
                {
                    // There's variation at this index
                    foundNextDifferentCharIndex = true;
                    // don't break from the loop in case there's another node ending here
                }
            }
        }

        var nodeResultIndex = -1;
        if (terminalEntryIndex > -1)
        {
            nodeResultIndex = entries[terminalEntryIndex].index;
            entries.RemoveAt(terminalEntryIndex);
        }
        var skipLength = nextDifferentCharIndex - comparisonIndex;

        var (cntIndex, cntRShift, cntMask) = BuildJumpTable(entries, nextDifferentCharIndex, trie);

        return new(
            skipLength: skipLength,
            continuationIndex: cntIndex,
            continuationIndexRShift: cntRShift,
            continuationIndexMask: cntMask,
            resultIndex: nodeResultIndex
            );
    }

    private (int cntIndex, int cntRShift, int cntMask) BuildJumpTable(IReadOnlyList<(int index, string key)> entries, int comparisonIndex, List<TrieEntry> trie)
    {
        var groups = entries.GroupBy(e => TCase.CaseTransform(e.key[comparisonIndex])).ToList();

        var (cntRShift, cntMask, cntLength) = OptimizeContinuationTable(groups);

        var cntIndex = trie.Count;
        trie.AddRange(Enumerable.Repeat(new TrieEntry(int.MinValue, -1, -1, -1, -1), cntLength));

        foreach (var group in groups)
        {
            var offset = (TCase.CaseTransform(group.Key) >> cntRShift) & cntMask;
            trie[cntIndex + offset] = BuildTrieEntry(group.ToList(), comparisonIndex, trie, isRoot: false);
        }

        return (cntIndex, cntRShift, cntMask);
    }

    private (int rShift, int mask, int length) OptimizeContinuationTable(List<IGrouping<int, (int index, string key)>> groups)
    {
        if (groups.Count == 1)
        {
            return (0, 0, 1);
        }

        var rShift = 0;
        var mask = 0;
        var length = 1;

        while (length < groups.Count)
        {
            mask = mask << 1 | 1;
            length *= 2;
        }

        var hasConflict = true;
        var conflict = new BitArray(128);
        while (hasConflict && length <= 128)
        {
            rShift = -1;
            while (hasConflict && ++rShift < 6)
            {
                conflict.SetAll(false);
                hasConflict = false;

                foreach (var group in groups)
                {
                    var index = (TCase.CaseTransform(group.Key) >> rShift) & mask;
                    if (conflict[index])
                    {
                        hasConflict = true;
                        break;
                    }

                    conflict[index] = true;
                }
            }

            if (hasConflict)
            {
                // We need more space
                mask = mask << 1 | 1;
                length *= 2;
            }
        }

        return (rShift, mask, length);
    }

    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        if ((_lengthFilter & (1UL << (key.Length % 64))) == 0UL
            || key.Length < _minLength
            || key.Length > _maxLength)
        {
            value = default;
            return false;
        }

        var trie = _trie;

        ref var trieEntry = ref trie[0];

        var keyIndex = trieEntry.SkipLength;

        while (keyIndex <= key.Length)
        {
            var foundSingleCandidate = keyIndex == key.Length | trieEntry.ContinuationIndex == -1;
            if (foundSingleCandidate)
            {
                var resultIndex = trieEntry.ResultIndex;
                if (resultIndex >= 0)
                {
                    var candidateKey = _keys[resultIndex];
                    if (TCase.StringEqual(candidateKey, key))
                    {
                        value = _values[resultIndex];
                        return true;
                    }
                }

                break;
            }

            var c = key[keyIndex];
            var trieIndex = ((TCase.CaseTransform(c)) >> trieEntry.ContinuationIndexRShift) & trieEntry.ContinuationIndexMask;
            trieEntry = ref trie[trieEntry.ContinuationIndex + trieIndex];

            keyIndex += trieEntry.SkipLength;
        }

        value = default;
        return false;
    }

    internal interface IGenericSpecializedWrapper
    {
        static abstract int CaseTransform(int c);

        static abstract bool StringEqual(string s1, string s2);
    }
}

internal sealed class OrdinalTrie<TValue> : InternalTrie<TValue, OrdinalTrie<TValue>.CaseGenericSpecializedWrapper>
{
    public OrdinalTrie(Dictionary<string, TValue> values) : base(values) { }

    internal struct CaseGenericSpecializedWrapper : IGenericSpecializedWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CaseTransform(int c) => c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StringEqual(string s1, string s2) => string.Equals(s1, s2);
    }
}

internal sealed class OrdinalIgnoreCaseTrie<TValue> : InternalTrie<TValue, OrdinalIgnoreCaseTrie<TValue>.CaseGenericSpecializedWrapper>
{
    public OrdinalIgnoreCaseTrie(Dictionary<string, TValue> values) : base(values) { }

    internal struct CaseGenericSpecializedWrapper : IGenericSpecializedWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CaseTransform(int c) =>
            // Possible optimization: just do c | 32. But first need to do analysis that none of the branches have conflicts
            (c >= 'a' | c <= 'z' ? c | 32 : c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StringEqual(string s1, string s2) => string.Equals(s1, s2);
    }
}
