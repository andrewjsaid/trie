using System.Diagnostics.CodeAnalysis;

namespace Trie;

public abstract class Trie<TValue>
{
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value);
}

internal sealed class Trie<TValue, TCase>
    : Trie<TValue>
    where TCase : struct, ICaseGenericSpecializedWrapper
{
    private readonly TrieEntry[] _trie;
    private readonly string[] _keys;
    private readonly TValue[] _values;
    private readonly int _minLength = int.MaxValue;
    private readonly int _maxLength;
    private readonly ulong _lengthFilter;

    public Trie(string[] keys, TValue[] values, TrieEntry[] trieEntries)
    {
        _keys = keys.ToArray();
        _values = values.ToArray();

        foreach (var key in _keys)
        {
            if (key.Length < _minLength) _minLength = key.Length;
            if (key.Length > _maxLength) _maxLength = key.Length;
            _lengthFilter |= 1ul << (key.Length % 64);
        }

        _trie = trieEntries;
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
}
