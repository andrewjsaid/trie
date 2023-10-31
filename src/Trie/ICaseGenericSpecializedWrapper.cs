using System.Runtime.CompilerServices;

namespace Trie;

internal interface ICaseGenericSpecializedWrapper
{
    static abstract int CaseTransform(int c);

    static abstract bool StringEqual(string s1, string s2);
}
internal struct CaseSensitiveGenericSpecializedWrapper : ICaseGenericSpecializedWrapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CaseTransform(int c) => c;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StringEqual(string s1, string s2) => string.Equals(s1, s2);
}

internal struct CaseInsensitiveGenericSpecializedWrapper : ICaseGenericSpecializedWrapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CaseTransform(int c) =>
        // Possible optimization: just do c | 32. But first need to do analysis that none of the branches have conflicts
        (c >= 'a' | c <= 'z' ? c | 32 : c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StringEqual(string s1, string s2) => StringComparer.OrdinalIgnoreCase.Equals(s1, s2);
}