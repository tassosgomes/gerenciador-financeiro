using System.Diagnostics;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

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
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
