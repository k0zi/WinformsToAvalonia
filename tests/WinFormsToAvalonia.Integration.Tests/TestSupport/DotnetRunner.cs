using System.Diagnostics;

namespace WinFormsToAvalonia.Integration.Tests.TestSupport;

/// <summary>
/// Runs the SDK against a generated project - the thing every test here ultimately asserts on,
/// since the tool's own build never sees the code it emits.
/// </summary>
/// <remarks>
/// <para>
/// <b>Node reuse is off, and that is not a tuning knob.</b> MSBuild's persistent worker nodes
/// outlive the build that started them, and they inherit the redirected stdout/stderr handles.
/// A reader draining those pipes therefore never reaches end-of-stream, so the test hangs forever
/// on a build that has already finished - which is exactly what a `dotnet build` of a *solution*
/// did here, while single-project builds happened not to spawn a node that survived.
/// </para>
/// <para>
/// The timeout is the second half of the same lesson: a hung test is worse than a failing one,
/// because it says nothing about what went wrong.
/// </para>
/// </remarks>
internal static class DotnetRunner
{
    /// <param name="timeout">
    /// Defaults to five minutes, which is generous for an ordinary build. A browser head is the
    /// exception: its first build runs the WebAssembly SDK's native link step, and that alone can
    /// take longer than the whole default budget.
    /// </param>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string arguments, string workingDirectory, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo("dotnet", $"{arguments} -nodeReuse:false")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return (-1, await stdOutTask, $"Timed out running `dotnet {arguments}` in '{workingDirectory}'.");
        }

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
