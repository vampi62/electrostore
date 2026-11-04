namespace ElectrostoreWORKER.Mqtt;

/// <summary>
/// Runs a shell command inside the Mosquitto broker (used to manage MQTT users with
/// <c>mosquitto_passwd</c> and to reload the broker). The implementation depends on where the
/// stack runs: the Docker API (docker-compose) or the Kubernetes API (k3s/k8s).
/// </summary>
internal interface IMosquittoCommandRunner
{
    /// <summary>Short name used in logs (<c>docker</c> or <c>kubernetes</c>).</summary>
    string Name { get; }

    /// <summary>Runs <paramref name="command"/> through <c>sh -c</c> inside the broker. Returns false if it could not be executed.</summary>
    Task<bool> ExecuteAsync(string command, CancellationToken ct);
}
