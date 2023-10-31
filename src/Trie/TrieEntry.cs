namespace Trie;

internal readonly struct TrieEntry(
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
