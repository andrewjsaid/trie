using System.Collections;

namespace Trie;

internal class TrieEntriesAnalyzer<TCase> where TCase : struct, ICaseGenericSpecializedWrapper
{
    private bool _ignoreCase;

    internal TrieEntriesAnalyzer(bool ignoreCase)
    {
        _ignoreCase = ignoreCase;
    }

    public TrieEntry[] Create(string[] keys)
    {
        List<TrieEntry> trie = new() { new TrieEntry() };

        var entries = keys.Select((k, i) => (i, k)).ToList();
        trie[0] = BuildTrieEntry(entries, 0, trie, isRoot: true);

        return trie.ToArray();
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

    private static (int rShift, int mask, int length) OptimizeContinuationTable(List<IGrouping<int, (int index, string key)>> groups)
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

}