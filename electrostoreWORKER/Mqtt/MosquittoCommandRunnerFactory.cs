using Docker.DotNet;
using k8s;

namespace ElectrostoreWORKER.Mqtt;

/// <summary>
/// Picks how the WORKER reaches the Mosquitto broker to manage MQTT users.
/// <c>MQTT:UserManagement</c> = <c>auto</c> (default) | <c>docker</c> | <c>kubernetes</c> | <c>none</c>.
/// In <c>auto</c> mode: Docker if a Docker socket is available (docker-compose), else Kubernetes if the
/// pod runs in a cluster, else MQTT user management is disabled (no runner).
/// </summary>
internal static class MosquittoCommandRunnerFactory
{
    private const string UnixDockerSocket = "/var/run/docker.sock";
    private const string ServiceAccountNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    private const string DefaultPodSelector = "app.kubernetes.io/component=mqtt";

    public static IMosquittoCommandRunner? Create(IConfiguration configuration, ILogger logger)
    {
        var mode = (configuration["MQTT:UserManagement"] ?? "auto").Trim().ToLowerInvariant();
        var containerName = configuration["MQTT:ContainerName"] ?? "electrostore-mqtt";

        IMosquittoCommandRunner? runner = mode switch
        {
            "none" or "disabled" or "off" => null,
            "docker" => CreateDocker(containerName, logger),
            "kubernetes" or "k8s" => CreateKubernetes(configuration, logger),
            "auto" => IsDockerAvailable()
                ? CreateDocker(containerName, logger)
                : KubernetesClientConfiguration.IsInCluster() ? CreateKubernetes(configuration, logger) : null,
            _ => throw new InvalidOperationException($"Invalid MQTT:UserManagement value '{mode}'. Expected auto, docker, kubernetes or none.")
        };

        if (runner is null)
        {
            logger.LogWarning(
                "MQTT user management is disabled (MQTT:UserManagement={Mode}; no Docker socket or Kubernetes cluster available). " +
                "'mqtt-user-events' messages are acknowledged but ignored: manage the broker users manually.", mode);
        }
        else
        {
            logger.LogInformation("MQTT user management enabled through {Runner}.", runner.Name);
        }
        return runner;
    }

    private static bool IsDockerAvailable()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"))
            || File.Exists(UnixDockerSocket)
            || OperatingSystem.IsWindows();
    }

    private static DockerMosquittoCommandRunner CreateDocker(string containerName, ILogger logger)
    {
        return new DockerMosquittoCommandRunner(new DockerClientConfiguration().CreateClient(), containerName, logger);
    }

    private static KubernetesMosquittoCommandRunner CreateKubernetes(IConfiguration configuration, ILogger logger)
    {
        var client = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
        var ns = configuration["MQTT:KubernetesNamespace"];
        if (string.IsNullOrWhiteSpace(ns))
        {
            ns = File.Exists(ServiceAccountNamespaceFile) ? File.ReadAllText(ServiceAccountNamespaceFile).Trim() : "default";
        }
        var selector = configuration["MQTT:KubernetesPodSelector"];
        if (string.IsNullOrWhiteSpace(selector))
        {
            selector = DefaultPodSelector;
        }
        return new KubernetesMosquittoCommandRunner(client, ns, selector, configuration["MQTT:KubernetesContainer"], logger);
    }
}
