using System.Diagnostics;

namespace BTCPayTranslator.Tests;

internal static class CliTestHost
{
    public static async Task<CliResult> RunAsync(
        IReadOnlyList<string> args,
        IDictionary<string, string?>? environmentVariables = null,
        int timeoutMilliseconds = 60000)
    {
        var projectDirectory = ResolveTranslatorProjectDirectory();
        
        // Build & Run the Solution
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectDirectory);
        startInfo.ArgumentList.Add("--");
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        startInfo.Environment["OPENROUTER_API_KEY"] = "test-key";
        startInfo.Environment["OPENROUTER_MODEL"] = "test-model";

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeoutMilliseconds);
        
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { /* already exited */ }
            catch (System.ComponentModel.Win32Exception) { /* exiting / access denied */ }

            await Task.WhenAny(Task.WhenAll(stdOutTask, stdErrTask), Task.Delay(2000));
            var partialOut = stdOutTask.IsCompletedSuccessfully ? stdOutTask.Result : string.Empty;
            var partialErr = stdErrTask.IsCompletedSuccessfully ? stdErrTask.Result : string.Empty;
            throw new TimeoutException(
                $"CLI did not exit within {timeoutMilliseconds} ms.\nStdOut: {partialOut}\nStdErr: {partialErr}");
        }

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return new CliResult(process.ExitCode, stdOut, stdErr);
    }

    private static string ResolveTranslatorProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Translator", "BTCPayTranslator.csproj");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Translator project directory.");
    }
}

internal sealed record CliResult(int ExitCode, string StdOut, string StdErr)
{
    public string CombinedOutput => StdOut + Environment.NewLine + StdErr;
}
