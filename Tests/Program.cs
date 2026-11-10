using System;
using StreetMatch.Tests;

internal static class Program
{
    private static void Main()
    {
        int count = GameRulesSuite.RunAll();
        Console.WriteLine("PASS: " + count + " assertions (catalog, progression, duplicates, scoring, hints, mastery, review, saves).");
    }
}
