using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;

namespace Trie.MicroBenchmarks;

public abstract class BaseTryGetValueBenchmarks<T>
{
    private FrozenDictionary<string, T> _frozen;
    private Trie<T> _trie;
    private string[] _positive;
    private string[] _negative;

    protected void Setup(Dictionary<string, T> dictionary, string[] positive, string[] negative)
    {
        _frozen = dictionary.ToFrozenDictionary(dictionary.Comparer);
        _trie = dictionary.ToTrie(ReferenceEquals(dictionary.Comparer, StringComparer.OrdinalIgnoreCase));
        _positive = positive;
        _negative = negative;
    }

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void FrozenDictionary_TryGetValue_True()
    {
        foreach (var item in _positive)
        {
            _frozen.TryGetValue(item, out _);
        }
    }

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void Trie_TryGetValue_True()
    {
        foreach (var item in _positive)
        {
            _trie.TryGetValue(item, out _);
        }
    }

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void FrozenDictionary_TryGetValue_False()
    {
        foreach (var item in _negative)
        {
            _frozen.TryGetValue(item, out _);
        }
    }

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void Trie_TryGetValue_False()
    {
        foreach (var item in _negative)
        {
            _trie.TryGetValue(item, out _);
        }
    }
}