using Docker.DotNet;
using Docker.DotNet.Models;
using ElectrostoreWORKER.Mqtt;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using k8s.Autorest;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests.Mqtt;

public class MosquittoCommandRunnerTests
{
    private readonly Mock<ILogger> _logger = new();

    private static IConfiguration Config(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();
    }

    // ---------- MosquittoCommandRunnerFactory ----------

    [Theory]
    [InlineData("none")]
    [InlineData("disabled")]
    [InlineData("OFF")]
    public void Factory_ShouldReturnNull_WhenUserManagementIsDisabled(string mode)
    {
        var runner = MosquittoCommandRunnerFactory.Create(Config(("MQTT:UserManagement", mode)), _logger.Object);

        Assert.Null(runner);
    }

    [Fact]
    public void Factory_ShouldThrow_WhenUserManagementValueIsInvalid()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MosquittoCommandRunnerFactory.Create(Config(("MQTT:UserManagement", "podman")), _logger.Object));
    }

    [Fact]
    public void Factory_ShouldReturnDockerRunner_WhenDockerIsForced()
    {
        var runner = MosquittoCommandRunnerFactory.Create(Config(("MQTT:UserManagement", "docker")), _logger.Object);

        Assert.NotNull(runner);
        Assert.Equal("docker", runner!.Name);
    }

    // ---------- DockerMosquittoCommandRunner ----------

    [Fact]
    public async Task DockerRunner_ShouldReturnFalse_WhenContainerIsNotFound()
    {
        var containers = new Mock<IContainerOperations>();
        containers
            .Setup(c => c.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerListResponse>());
        var docker = new Mock<IDockerClient>();
        docker.Setup(d => d.Containers).Returns(containers.Object);
        var runner = new DockerMosquittoCommandRunner(docker.Object, "electrostore-mqtt", _logger.Object);

        var result = await runner.ExecuteAsync("kill -HUP 1", CancellationToken.None);

        Assert.False(result);
    }

    // ---------- KubernetesMosquittoCommandRunner ----------

    [Fact]
    public async Task KubernetesRunner_ShouldReturnFalse_WhenNoRunningPodMatchesTheSelector()
    {
        var coreV1 = new Mock<ICoreV1Operations>();
        coreV1
            .Setup(c => c.ListNamespacedPodWithHttpMessagesAsync(
                "ns", It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                "app.kubernetes.io/component=mqtt", It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1PodList> { Body = new V1PodList { Items = [] } });
        var client = new Mock<IKubernetes>();
        client.Setup(k => k.CoreV1).Returns(coreV1.Object);
        var runner = new KubernetesMosquittoCommandRunner(client.Object, "ns", "app.kubernetes.io/component=mqtt", null, _logger.Object);

        var result = await runner.ExecuteAsync("kill -HUP 1", CancellationToken.None);

        Assert.False(result);
        Assert.Equal("kubernetes", runner.Name);
    }
}
