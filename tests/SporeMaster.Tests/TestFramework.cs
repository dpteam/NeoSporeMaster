using System.Diagnostics;

namespace SporeMaster.Tests;

/// <summary>Marks a public static method as a test case.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SporeTestAttribute : Attribute
{
    public SporeTestAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
}

/// <summary>Assertion helpers for the dependency-free test harness.</summary>
public static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new AssertionException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new AssertionException(message ?? "Expected condition to be false.");
        }
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException(message ?? $"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null)
        {
            throw new AssertionException(message ?? "Expected a non-null value.");
        }
    }

    public static void Contains(string haystack, string needle)
    {
        if (haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new AssertionException($"Expected '{haystack}' to contain '{needle}'.");
        }
    }

    public static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name}, got {ex.GetType().Name}.");
        }

        throw new AssertionException($"Expected {typeof(TException).Name}, but nothing was thrown.");
    }
}

public sealed class AssertionException : Exception
{
    public AssertionException(string message)
        : base(message)
    {
    }
}

/// <summary>Runs every <see cref="SporeTestAttribute"/> method and reports the outcome.</summary>
public static class TestRunner
{
    public static int Run(string? filter)
    {
        var methods = typeof(TestRunner).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Select(m => (Method: m, Attribute: m.GetCustomAttributes(typeof(SporeTestAttribute), false).FirstOrDefault() as SporeTestAttribute))
            .Where(x => x.Attribute is not null)
            .Select(x => (x.Method, Name: x.Attribute!.Name ?? $"{x.Method.DeclaringType?.Name}.{x.Method.Name}"))
            .Where(x => filter is null || x.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        int passed = 0;
        var failures = new List<(string Name, Exception Error)>();
        var stopwatch = Stopwatch.StartNew();

        foreach ((var method, string name) in methods)
        {
            try
            {
                method.Invoke(null, null);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("PASS ");
                Console.ResetColor();
                Console.WriteLine(name);
                passed++;
            }
            catch (Exception ex)
            {
                Exception error = ex is System.Reflection.TargetInvocationException tie && tie.InnerException is not null
                    ? tie.InnerException
                    : ex;
                failures.Add((name, error));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL ");
                Console.ResetColor();
                Console.WriteLine(name);
                Console.WriteLine($"     {error.Message}");
            }
        }

        stopwatch.Stop();
        Console.WriteLine();
        Console.WriteLine($"{passed}/{methods.Count} test(s) passed in {stopwatch.ElapsedMilliseconds} ms.");

        if (failures.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{failures.Count} failure(s):");
            foreach ((string name, Exception error) in failures)
            {
                Console.WriteLine($"  - {name}: {error.Message}");
            }

            Console.ResetColor();
            return 1;
        }

        return 0;
    }
}
