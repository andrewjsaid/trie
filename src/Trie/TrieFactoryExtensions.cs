namespace Trie;

public static class TrieFactoryExtensions
{
    private static readonly TrieEntriesAnalyzer<CaseSensitiveGenericSpecializedWrapper> CaseSensitiveAnalyzer = new(ignoreCase: false);

    private static readonly TrieEntriesAnalyzer<CaseInsensitiveGenericSpecializedWrapper> CaseInsensitiveAnalyzer = new(ignoreCase: true);

    public static Trie<TValue> ToTrie<TValue>(this IEnumerable<KeyValuePair<string, TValue>> entries, bool ignoreCase)
    {
        var wrapped = new Dictionary<string, TValue>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var (key, value) in entries)
        {
            wrapped[key] = value;
        }

        var keys = wrapped.Keys.ToArray();
        var values = wrapped.Values.ToArray();

        if (!ignoreCase)
        {
            var trieEntries = CaseSensitiveAnalyzer.Create(keys);
            return new Trie<TValue, CaseSensitiveGenericSpecializedWrapper>(keys, values, trieEntries);
        }
        else
        {
            var trieEntries = CaseInsensitiveAnalyzer.Create(keys);
            return new Trie<TValue, CaseInsensitiveGenericSpecializedWrapper>(keys, values, trieEntries);
        }
    }
}