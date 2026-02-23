using System.Diagnostics;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

public sealed class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            Skip = "Docker nao esta disponivel neste ambiente.";
        }
    }
}

internal static class DockerAvailabilityChecker
{
    private static readonly Lazy<bool> DockerAvailability = new(CheckDockerAvailability);

    public static bool IsDockerAvailable => DockerAvailability.Value;

    private static bool CheckDockerAvailability()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
