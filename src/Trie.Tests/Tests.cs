namespace Trie.Tests;

public class Tests
{

    [Fact]
    public void Numbers()
    {
        Test(Enumerable.Range(0, 100).ToDictionary(i => i.ToString()));
        Test(Enumerable.Range(0, 999).ToDictionary(i => i.ToString().PadLeft(3, '0')));
        Test(new Dictionary<string, int>
        {
            { "One", 1 },
            { "Two", 1 },
            { "Three", 1 },
            { "Four", 1 },
            { "Five", 1 },
            { "Six", 1 },
            { "Seven", 1 },
            { "Eight", 1 },
            { "Nine", 1 },
            { "Ten", 1 },
        });
    }

    [Fact]
    public void Enums()
    {
        TestEnum<System.ConsoleColor>();
        TestEnum<System.StringComparison>();
        TestEnum<System.Reflection.BindingFlags>();
        TestEnum<System.Reflection.CallingConventions>();
        void TestEnum<T>() where T : struct, Enum
        {
            var values = Enum.GetValues<T>();
            var dictionary = values.ToDictionary(v => v.ToString());
            Test(dictionary);
        }
    }

    [Fact]
    public void Aaaaaa()
    {
        Test(Enumerable.Range(1, 100).Select(count => new string('a', count)).ToDictionary(s => s));
    }

    [Fact]
    public void Banana()
    {
        Test(new[] {
            "Banana", "banana", "banal", "BANAL",
            "banan", "anabanana", "anaBan", "anbana",
            "nabanana", "nanaba", "naNa"
        }.ToDictionary(s => s));
    }

    private static void Test<T>(Dictionary<string, T> items)
    {
        var trieCaseSensitive = items.ToTrie(ignoreCase: false);
        var trieIgnoreCase = items.ToTrie(ignoreCase: true);

        foreach (var (key, value) in items)
        {
            var ignoreForCase = items.Keys.Any(other => !key.Equals(other) && key.Equals(other, StringComparison.OrdinalIgnoreCase));

            T? caseSensitiveValue;
            T? ignoreCaseValue;

            Assert.True(trieCaseSensitive.TryGetValue(key, out caseSensitiveValue));
            Assert.Equal(value, caseSensitiveValue);

            if (!ignoreForCase)
            {
                Assert.True(trieIgnoreCase.TryGetValue(key, out ignoreCaseValue));
                Assert.Equal(value, ignoreCaseValue);
            }

            var lower = key.ToLower();
            if (!items.ContainsKey(lower))
            {
                Assert.False(trieCaseSensitive.TryGetValue(lower, out caseSensitiveValue));
                Assert.Equal(default, caseSensitiveValue);
                
                if (!ignoreForCase)
                {
                    Assert.True(trieIgnoreCase.TryGetValue(lower, out ignoreCaseValue));
                    Assert.Equal(value, ignoreCaseValue);
                }
            }

            var upper = key.ToUpper();
            if (!items.ContainsKey(upper))
            {
                Assert.False(trieCaseSensitive.TryGetValue(upper, out caseSensitiveValue));
                Assert.Equal(default, caseSensitiveValue);

                if (!ignoreForCase)
                {
                    Assert.True(trieIgnoreCase.TryGetValue(upper, out ignoreCaseValue));
                    Assert.Equal(value, ignoreCaseValue);
                }
            }

            if (key.Length > 1)
            {
                for (var i = 0; i < key.Length - 1; i++)
                {
                    var newKey = key[..i] + key[i + 1] + key[(i + 1)..];
                    if (!items.ContainsKey(newKey))
                    {
                        Assert.False(trieCaseSensitive.TryGetValue(newKey, out caseSensitiveValue));
                        Assert.Equal(default, caseSensitiveValue);

                    }

                    if (!ignoreForCase)
                    {
                        if (!items.Keys.Any(k => string.Equals(k, newKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            Assert.False(trieIgnoreCase.TryGetValue(newKey, out ignoreCaseValue));
                            Assert.Equal(default, ignoreCaseValue);
                        }
                    }
                }
            }
        }

    }
}
