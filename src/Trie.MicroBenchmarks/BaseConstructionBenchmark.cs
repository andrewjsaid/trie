using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;

namespace Trie.MicroBenchmarks;

[MemoryDiagnoser]
public abstract class BaseConstructionBenchmark<T>
{
    protected Dictionary<string, T> _dictionary;

    private (bool ignoreCase, IEqualityComparer<string> comparer) GetComparison() => (ReferenceEquals(_dictionary.Comparer, StringComparer.OrdinalIgnoreCase), _dictionary.Comparer);

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void FrozenDictionary_Create()
    {
        var (_, comparer) = GetComparison();
        _dictionary.ToFrozenDictionary(comparer);
    }

    [Benchmark, IterationCount(BenchmarkConfig.IterationCount)]
    public void Trie_Create()
    {
        var (ignoreCase, _) = GetComparison();
        _dictionary.ToTrie(ignoreCase);
    }
}