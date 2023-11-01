using BenchmarkDotNet.Attributes;

namespace Trie.MicroBenchmarks;

public class NumbersTryGetValue : BaseTryGetValueBenchmarks<int>
{
    [GlobalSetup]
    public void Setup()
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
        Setup(dictionary,
            positive: dictionary.Keys.ToArray(),
            negative: new[] { "one", "one hundred", "zero", "ones", "stwo", "thre", "fiveteen" });
    }
}

public class NumbersCreate : BaseConstructionBenchmark<int>
{
    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
            { "four", 4 },
            { "five", 5 },
            { "six", 6 },
            { "seven", 7 },
            { "eight", 8 },
            { "nine", 9 },
            { "ten", 10 },
            { "eleven", 11 },
            { "twelve", 12 },
            { "thirteen", 13 },
            { "fourteen", 14 },
            { "fifeteen", 15 },
            { "sixteen", 16 },
            { "seventeen", 17 },
            { "eighteen", 18 },
            { "nineteen", 19 },
            { "twenty", 20 },
        };
    }
}