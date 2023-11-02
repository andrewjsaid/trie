using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Trie;

internal class CompiledTrieStaticMethods
{
    public static readonly MethodInfo StringLengthMethod = typeof(string).GetProperty("Length", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!;
    public static readonly MethodInfo StringIndexerMethod = typeof(string).GetMethod("get_Chars", BindingFlags.Instance | BindingFlags.Public)!;
}

internal class CompiledTrie<TValue> : Trie<TValue>
{
    private readonly Func<string, int> _func;
    private readonly string[] _keys;
    private readonly TValue[] _values;
    private readonly int _minLength = int.MaxValue;
    private readonly int _maxLength;
    private readonly ulong _lengthFilter;

    public CompiledTrie(string[] keys, TValue[] values, TrieEntry[] entries)
    {
        _keys = keys;
        _values = values;
        _func = CreateMethod(keys, entries);

        foreach (var key in _keys)
        {
            if (key.Length < _minLength) _minLength = key.Length;
            if (key.Length > _maxLength) _maxLength = key.Length;
            _lengthFilter |= 1ul << (key.Length % 64);
        }
    }

    private static Func<string, int> CreateMethod(string[] keys, TrieEntry[] entries)
    {
        var auxInfo = GetAuxInfo(keys, entries);

        var method = new DynamicMethod(
            "GetValueIndex",
            typeof(int),
            new[] { typeof(string) });

        var il = method.GetILGenerator();
        GenerateMethodBody(il, keys, entries, auxInfo);

#if IL_EMIT_SAVE_ASSEMBLY
        SaveAssembly(keys, entries, auxInfo);
#endif

        return (Func<string, int>)method.CreateDelegate(typeof(Func<string, int>));
    }

#if IL_EMIT_SAVE_ASSEMBLY
    private static void SaveAssembly(
        string[] keys,
        TrieEntry[] entries,
        TrieEntryAuxInfo[] auxInfo)
    {
        var assemblyName = "Trie.IlEmit" + DateTime.Now.Ticks;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName);
        var type = module.DefineType("ILEmitTrie");
        var method = type.DefineMethod(
            "GetValueIndex",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(int),
            new[] { typeof(string) });

        GenerateMethodBody(method.GetILGenerator(), keys, entries, auxInfo);

        type.CreateTypeInfo();

        var fileName = assemblyName + ".dll";
        var generator = new Lokad.ILPack.AssemblyGenerator();
        generator.GenerateAssembly(assembly, fileName);
    }
#endif

    private static void GenerateMethodBody(
        ILGenerator il,
        string[] keys,
        TrieEntry[] entries,
        TrieEntryAuxInfo[] auxInfo)
    {
        il.DeclareLocal(typeof(int));

        GenerateMethodBody(il, keys, entries, auxInfo,
            entryIndex: 0,
            readIndex: entries[0].SkipLength,
            checkedMinLength: 0);

        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ret);
    }

    private static void GenerateMethodBody(
        ILGenerator il,
        string[] keys,
        TrieEntry[] entries,
        TrieEntryAuxInfo[] auxInfo,
        int entryIndex,
        int readIndex,
        int checkedMinLength)
    {
        var entry = entries[entryIndex];
        var entryAux = auxInfo[entryIndex];

        if (entry.ContinuationIndex == -1)
        {
            il.Emit(OpCodes.Ldc_I4, entry.ResultIndex);
            il.Emit(OpCodes.Ret);
            return;
        }

        if (entryAux.MinDescendentLength > checkedMinLength)
        {
            // emit: if(arg0.Length < entryAux.MinDescendentLength) return -1
            var endIfLength = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, CompiledTrieStaticMethods.StringLengthMethod);
            il.Emit(OpCodes.Ldc_I4, entryAux.MinDescendentLength);
            il.Emit(OpCodes.Bge, endIfLength);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Ret);
            il.MarkLabel(endIfLength);

            checkedMinLength = entryAux.MinDescendentLength;
        }

        if (checkedMinLength == readIndex)
        {
            // if (key.Length == keyIndex) return entry.ResultIndex
            var endifSingleCandidate = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, CompiledTrieStaticMethods.StringLengthMethod);
            il.Emit(OpCodes.Ldc_I4, readIndex);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Brfalse, endifSingleCandidate);
            il.Emit(OpCodes.Ldc_I4, entry.ResultIndex);
            il.Emit(OpCodes.Ret);
            il.MarkLabel(endifSingleCandidate);
        }

        if (entry.ContinuationIndexMask > 0)
        {
            // jump = (key[keyIndex] >> RShift) & Mask
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, readIndex);
            il.Emit(OpCodes.Call, CompiledTrieStaticMethods.StringIndexerMethod);
            il.Emit(OpCodes.Ldc_I4, entry.ContinuationIndexRShift);
            il.Emit(OpCodes.Shr);
            il.Emit(OpCodes.Ldc_I4, entry.ContinuationIndexMask);
            il.Emit(OpCodes.And);
            il.Emit(OpCodes.Stloc_0);

            var numJumps = 1 + ((127 >> entry.ContinuationIndexRShift) & entry.ContinuationIndexMask);
            for (var i = 0; i < numJumps; i++)
            {
                var continuationIndex = entry.ContinuationIndex + i;
                var isBlank = entries[continuationIndex] is { ContinuationIndex: -1, ResultIndex: -1 };
                if (!isBlank)
                {
                    var skipLabel = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse, skipLabel);

                    GenerateMethodBody(il, keys, entries, auxInfo,
                        entryIndex: continuationIndex,
                        readIndex: readIndex + entries[continuationIndex].SkipLength,
                        checkedMinLength: checkedMinLength);

                    il.MarkLabel(skipLabel);
                }
            }
        }
        else
        {
            var continuationIndex = entry.ContinuationIndex;
            var isBlank = entries[continuationIndex] is { ContinuationIndex: -1, ResultIndex: -1 };
            if (!isBlank)
            {
                GenerateMethodBody(il, keys, entries, auxInfo,
                    entryIndex: continuationIndex,
                    readIndex: readIndex + entries[continuationIndex].SkipLength,
                    checkedMinLength: checkedMinLength);
            }
        }

        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ret);
    }

    private static TrieEntryAuxInfo[] GetAuxInfo(string[] keys, TrieEntry[] entries)
    {
        var results = new TrieEntryAuxInfo[entries.Length];

        LoadAuxInfo(0);

        TrieEntryAuxInfo LoadAuxInfo(int index)
        {
            var entry = entries[index];
            var minDescendentLength = entry.ResultIndex >= 0 ? keys[entry.ResultIndex].Length : int.MaxValue;

            if (entry.ContinuationIndex >= 0)
            {
                var numJumps = 1 + ((127 >> entry.ContinuationIndexRShift) & entry.ContinuationIndexMask);
                for (var i = 0; i < numJumps; i++)
                {
                    var continuationIndex = entry.ContinuationIndex + i;

                    var child = LoadAuxInfo(continuationIndex);
                    if (minDescendentLength > child.MinDescendentLength)
                    {
                        minDescendentLength = child.MinDescendentLength;
                    }
                }
            }

            results[index] = new(minDescendentLength: minDescendentLength);
            return results[index];
        }

        return results.ToArray();
    }

    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        var failsLengthCheck = (_lengthFilter & (1UL << (key.Length % 64))) == 0UL
                || key.Length < _minLength
                || key.Length > _maxLength;
        if (!failsLengthCheck)
        {
            var valueIndex = _func(key);
            if (valueIndex > -1)
            {
                var candidateKey = _keys[valueIndex];
                if (string.Equals(key, candidateKey))
                {
                    value = _values[valueIndex];
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private readonly struct TrieEntryAuxInfo(int minDescendentLength)
    {
        public readonly int MinDescendentLength = minDescendentLength;
    }
}