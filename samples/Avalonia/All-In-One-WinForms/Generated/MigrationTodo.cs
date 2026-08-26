using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace All_In_One_WinForms.Generated;

/// <summary>
/// Marks a generated member whose original WinForms body has not been migrated yet. Every
/// such member keeps that body as a comment and then calls <see cref="NotMigrated"/>.
/// </summary>
/// <remarks>
/// Reporting rather than throwing is deliberate: these members are invoked by the
/// framework, including during XAML initialization (a TabControl selects its first tab,
/// a Window raises Loaded), so throwing would take the app down before it is visible.
/// Set <see cref="ThrowOnUnmigratedCall"/> to true - e.g. in a test run - to get the
/// strict behaviour back once you want un-migrated code to fail loudly.
/// </remarks>
public static class MigrationTodo
{
    private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);

    /// <summary>Throw a <see cref="NotImplementedException"/> instead of reporting.</summary>
    public static bool ThrowOnUnmigratedCall { get; set; }

    /// <summary>Every member reported so far, in first-call order - handy in a smoke test.</summary>
    public static IReadOnlyCollection<string> ReportedMembers
    {
        get
        {
            lock (Reported)
            {
                return Reported.ToArray();
            }
        }
    }

    /// <param name="member">The generated member that ran, e.g. nameof(button1_Click).</param>
    /// <param name="originalWinFormsMember">The WinForms method its body came from.</param>
    public static void NotMigrated(string member, string originalWinFormsMember)
    {
        var message =
            $"TODO(Winforms2Avalonia): '{member}' is not migrated yet - the original WinForms body of " +
            $"'{originalWinFormsMember}' is preserved inside it as a comment.";

        if (ThrowOnUnmigratedCall)
        {
            throw new NotImplementedException(message);
        }

        bool isFirstCall;
        lock (Reported)
        {
            isFirstCall = Reported.Add(member);
        }

        if (isFirstCall)
        {
            // Both, on purpose: stderr is what you see running `dotnet run` from a
            // terminal, Debug output is what you see attached to a debugger on Windows,
            // where a WinExe has no console at all.
            Console.Error.WriteLine(message);
            Debug.WriteLine(message);
        }
    }
}