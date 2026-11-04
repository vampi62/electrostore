using k8s;
using k8s.Models;

namespace ElectrostoreWORKER.Mqtt;

/// <summary>
/// Runs commands in the Mosquitto pod through the Kubernetes API (<c>pods/exec</c>), using the pod's
/// own ServiceAccount. Needs a Role allowing <c>get,list</c> on <c>pods</c> and <c>create</c> on <c>pods/exec</c>.
/// </summary>
internal sealed class KubernetesMosquittoCommandRunner : IMosquittoCommandRunner
{
    private readonly IKubernetes _client;
    private readonly string _namespace;
    private readonly string _podSelector;
    private readonly string? _containerName;
    private readonly ILogger _logger;

    public KubernetesMosquittoCommandRunner(IKubernetes client, string @namespace, string podSelector, string? containerName, ILogger logger)
    {
        _client = client;
        _namespace = @namespace;
        _podSelector = podSelector;
        _containerName = containerName;
        _logger = logger;
    }

    public string Name => "kubernetes";

    public async Task<bool> ExecuteAsync(string command, CancellationToken ct)
    {
        try
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(_namespace, labelSelector: _podSelector, cancellationToken: ct);
            var pod = pods.Items.FirstOrDefault(p => p.Status?.Phase == "Running");
            if (pod is null)
            {
                _logger.LogError("No running Mosquitto pod found in namespace {Namespace} (selector '{Selector}'). Cannot execute command.", _namespace, _podSelector);
                return false;
            }
            var container = _containerName ?? pod.Spec.Containers[0].Name;
            var stderr = string.Empty;
            var exitCode = await _client.NamespacedPodExecAsync(
                pod.Metadata.Name,
                _namespace,
                container,
                ["sh", "-c", command],
                false,
                async (_, _, stdErr) =>
                {
                    using var reader = new StreamReader(stdErr);
                    stderr = await reader.ReadToEndAsync(ct);
                },
                ct);
            if (exitCode != 0)
            {
                _logger.LogError("Command failed in Mosquitto pod {Pod} (exit code {ExitCode}): {Stderr}", pod.Metadata.Name, exitCode, stderr.Trim());
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in Mosquitto pod: {Message}", ex.Message);
            return false;
        }
    }
}
