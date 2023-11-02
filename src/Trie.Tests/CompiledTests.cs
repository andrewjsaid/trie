namespace Trie.Tests;

public class CompiledTests
{

    [Fact]
    public void Compiled_TryGetValue()
    {
        var dictionary = new Dictionary<string, int>
        {
            { "One", 1 },
            { "Two", 2 },
            { "Three", 3 },
            { "Four", 4 },
            { "Five", 5 },
            { "Six", 6 },
            { "Seven", 7 },
            { "Eight", 8 },
            { "Nine", 9 },
            { "Ten", 10 },
            { "Eleven", 11 },
            { "Twelve", 12 },
            { "Thirteen", 13 },
            { "Fourteen", 14 },
            { "Fifeteen", 15 },
            { "Sixteen", 16 },
            { "Seventeen", 17 },
            { "Eighteen", 18 },
            { "Nineteen", 19 },
            { "Twenty", 20 },
        };

        var trie = dictionary.ToTrie(ignoreCase: false, new()
        {
            Compiled = true
        });

        foreach (var (key, value) in dictionary)
        {
            Assert.True(trie.TryGetValue(key, out var result));
            Assert.Equal(value, result);

            Assert.False(trie.TryGetValue(key.ToUpper(), out _));
            Assert.False(trie.TryGetValue(key.ToLower(), out _));
        }

        foreach (var key in new[] { "one", "one hundred", "zero", "ones", "stwo", "thre", "fiveteen" })
        {
            Assert.False(trie.TryGetValue(key, out _));
        }
    }

}