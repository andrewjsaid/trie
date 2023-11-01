## Summary
I wanted to challenge myself to implement a [Trie](https://en.wikipedia.org/wiki/Trie) which can compete with .NET 8 `FrozenDictionary`. This is the result which I came up with. 

The implementation was done from my memory of what Tries are from my studies.
After initial development of this project, I researched Tries again and in more detail.
I found that my implementation has similarities with:
1. [PATRICIA Trie](https://en.wikipedia.org/wiki/Radix_tree#Variants) in the sense that it stores the next position to compare to skip ahead, and then does a full key comparison at the end.
2. [Array Mapped Trie](https://en.wikipedia.org/wiki/Bitwise_trie_with_bitmap) in the sense that it attempts to compress the branches. The technique I chose is probably faster as it avoids branching entirely, but uses more space.

I don't intend to continue this experiment as I no longer believe that trees can outperform hashes in the general case and I am personally satisfied with how close the results are.
The "Contrived" benchmark results (shown below) demonstrates a scenario deliberately designed to play into the strengths of the Trie and into the weaknesses of the `FrozenDictionary` (as of .NET 8 launch).

## Limitations
1. Only supports ASCII.
2. Only supports Ordinal/OrdinalIgnoreCase comparers.
3. No input validation - can cause exceptions.

## Further Work

**Optimizations:**
1. Allow skipping comparison of an index if a latter one is better.
2. Optimize IgnoreCase to avoid branching if there's no collisions in the case that we always do `| 32`.
3. Optimize construction - currently no effort whatsoever has been put into that.
3. Investigate if a canonical implementation of [Array Mapped Trie](https://en.wikipedia.org/wiki/Bitwise_trie_with_bitmap) improves performance.

**General:**
1. Document the code.
2. More benchmarks.

## Benchmark Results

```
Contrived

| Method                             | Mean         | Error      | StdDev     |
|----------------------------------- |-------------:|-----------:|-----------:|
| FrozenDictionary_TryGetValue_True  | 1,784.862 ns | 79.3097 ns | 20.5965 ns |
| Trie_TryGetValue_True              | 1,352.496 ns | 61.9586 ns | 16.0905 ns |
| FrozenDictionary_TryGetValue_False |     6.985 ns |  0.3589 ns |  0.0932 ns |
| Trie_TryGetValue_False             |     5.159 ns |  0.0366 ns |  0.0095 ns |

| Method                  | Mean     | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------ |---------:|---------:|---------:|-------:|-------:|----------:|
| FrozenDictionary_Create | 46.71 us | 2.755 us | 0.426 us | 1.4648 |      - |  24.53 KB |
| Trie_Create             | 39.26 us | 2.269 us | 0.589 us | 8.8501 | 0.7324 | 144.79 KB |
```

```
Numbers

| Method                             | Mean      | Error    | StdDev   |
|----------------------------------- |----------:|---------:|---------:|
| FrozenDictionary_TryGetValue_True  |  95.89 ns | 2.246 ns | 0.348 ns |
| Trie_TryGetValue_True              | 119.96 ns | 5.309 ns | 1.379 ns |
| FrozenDictionary_TryGetValue_False |  62.45 ns | 0.776 ns | 0.120 ns |
| Trie_TryGetValue_False             |  28.80 ns | 0.178 ns | 0.028 ns |

| Method                  | Mean       | Error     | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------ |-----------:|----------:|---------:|-------:|-------:|----------:|
| FrozenDictionary_Create |   199.0 ns |   8.42 ns |  2.19 ns | 0.0305 |      - |     512 B |
| Trie_Create             | 4,935.0 ns | 365.50 ns | 94.92 ns | 1.2207 | 0.0153 |   20528 B |
```