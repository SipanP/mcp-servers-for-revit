using System.ComponentModel;
using System.Diagnostics;

namespace RevitMcpServer.Setup;

public sealed record ProcessResult(bool Started, int ExitCode, string Output)
{
    public bool Succeeded => Started && ExitCode == 0;

    public static ProcessResult NotFound { get; } = new(false, -1, string.Empty);
}

/// <summary>Runs external commands. Abstracted so setup can be tested without launching anything.</summary>
public interface IProcessRunner
{
    ProcessResult Run(string fileName, IReadOnlyList<string> arguments);
}

public sealed class ProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return ProcessResult.NotFound;
            }

            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(milliseconds: 60_000);
            return new ProcessResult(true, process.ExitCode, output.Trim());
        }
        catch (Win32Exception)
        {
            // The executable is not on PATH.
            return ProcessResult.NotFound;
        }
    }
}
