using Docker.DotNet;
using Docker.DotNet.Models;

namespace ElectrostoreWORKER.Mqtt;

/// <summary>Runs commands in the Mosquitto container through the Docker API (docker.sock mounted by docker-compose).</summary>
internal sealed class DockerMosquittoCommandRunner : IMosquittoCommandRunner
{
    private readonly IDockerClient _dockerClient;
    private readonly string _containerName;
    private readonly ILogger _logger;

    public DockerMosquittoCommandRunner(IDockerClient dockerClient, string containerName, ILogger logger)
    {
        _dockerClient = dockerClient;
        _containerName = containerName;
        _logger = logger;
    }

    public string Name => "docker";

    public async Task<bool> ExecuteAsync(string command, CancellationToken ct)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true }, ct);
        var mosquittoContainer = containers.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/').Equals(_containerName, StringComparison.OrdinalIgnoreCase)));
        if (mosquittoContainer is null)
        {
            _logger.LogError("Mosquitto container not found. Cannot execute command.");
            return false;
        }
        try
        {
            var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(mosquittoContainer.ID, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = ["sh", "-c", command]
            }, ct);
            await _dockerClient.Exec.StartContainerExecAsync(execCreateResponse.ID, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in Mosquitto container: {Message}", ex.Message);
            return false;
        }
    }
}
