## Summary
I wanted to challenge myself to implement a [Trie](https://en.wikipedia.org/wiki/Trie) which can compete with .NET 8 `FrozenDictionary`. This is the result which I came up with. 

The implementation was done from my memory of what Tries are from my studies.
After initial development of this project, I researched Tries again and in more detail.
I found that my implementation has similarities with:
1. [PATRICIA Trie](https://en.wikipedia.org/wiki/Radix_tree#Variants) in the sense that it stores the next position to compare to skip ahead, and then does a full key comparison at the end.
2. [Array Mapped Trie](https://en.wikipedia.org/wiki/Bitwise_trie_with_bitmap) in the sense that it attempts to compress the branches. The technique I chose is probably faster as it avoids branching entirely, but uses more space.

I then implemented the trie as a code generator purely to maximize performance. This is an unfair comparison but it got the job done; the `FrozenDictionary` was beaten.

I don't intend to continue this experiment as I no longer believe that trees can outperform hashes in most practical cases and I am personally satisfied with how close the results are.
The "Contrived" benchmark results (shown below) demonstrates a scenario deliberately designed to play into the strengths of the Trie and into the weaknesses of the `FrozenDictionary` (as of .NET 8 launch).

## Limitations
1. Only supports ASCII.
2. Only supports Ordinal/OrdinalIgnoreCase comparers.
3. No input validation - can cause exceptions.
4. Doesn't support string.Empty as a Key. This can be added but it's not in scope.

## Further Work

**Optimizations:**
1. Allow skipping comparison of an index if a latter one is better.
2. Optimize IgnoreCase to avoid branching if there's no collisions in the case that we always do `| 32`.
3. Optimize construction - currently no effort whatsoever has been put into that.
4. Investigate if a canonical implementation of [Array Mapped Trie](https://en.wikipedia.org/wiki/Bitwise_trie_with_bitmap) improves performance.

**General:**
1. Document the code.
2. More benchmarks.

## Benchmark Results

```
Contrived

| Method                             | Mean         | Error      | StdDev     |
|----------------------------------- |-------------:|-----------:|-----------:|
| FrozenDictionary_TryGetValue_True  | 1,776.995 ns | 64.1839 ns | 16.6684 ns |
| Trie_TryGetValue_True              | 1,395.883 ns | 15.5391 ns |  4.0355 ns |
| Trie_Compiled_TryGetValue_True     | 1,141.779 ns | 41.9825 ns | 10.9027 ns |
| FrozenDictionary_TryGetValue_False |     7.071 ns |  0.0221 ns |  0.0057 ns |
| Trie_TryGetValue_False             |     8.303 ns |  0.0702 ns |  0.0109 ns |
| Trie_Compiled_TryGetValue_False    |     4.882 ns |  0.1038 ns |  0.0161 ns |

| Method                  | Mean     | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------ |---------:|---------:|---------:|-------:|-------:|----------:|
| FrozenDictionary_Create | 46.71 us | 2.755 us | 0.426 us | 1.4648 |      - |  24.53 KB |
| Trie_Create             | 39.26 us | 2.269 us | 0.589 us | 8.8501 | 0.7324 | 144.79 KB |
```

```
Numbers

| Method                             | Mean      | Error    | StdDev   |
|----------------------------------- |----------:|---------:|---------:|
| FrozenDictionary_TryGetValue_True  |  93.54 ns | 2.210 ns | 0.574 ns |
| Trie_TryGetValue_True              | 110.41 ns | 1.396 ns | 0.216 ns |
| Trie_Compiled_TryGetValue_True     |  63.47 ns | 1.501 ns | 0.390 ns |
| FrozenDictionary_TryGetValue_False |  57.40 ns | 1.391 ns | 0.361 ns |
| Trie_TryGetValue_False             |  28.54 ns | 0.052 ns | 0.008 ns |
| Trie_Compiled_TryGetValue_False    |  22.35 ns | 0.696 ns | 0.181 ns |

| Method                  | Mean       | Error     | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------ |-----------:|----------:|---------:|-------:|-------:|----------:|
| FrozenDictionary_Create |   199.0 ns |   8.42 ns |  2.19 ns | 0.0305 |      - |     512 B |
| Trie_Create             | 4,935.0 ns | 365.50 ns | 94.92 ns | 1.2207 | 0.0153 |   20528 B |
```