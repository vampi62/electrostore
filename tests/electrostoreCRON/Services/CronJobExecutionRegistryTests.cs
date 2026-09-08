using ElectrostoreCRON.Services.CronJobExecutionRegistry;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class CronJobExecutionRegistryTests
{
    [Fact]
    public void Register_ReturnsToken_ThatIsNotCancelled()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();

        // Act
        var token = registry.Register(1, CancellationToken.None);

        // Assert
        Assert.False(token.IsCancellationRequested);
    }

    [Fact]
    public void Register_ReturnedToken_IsCancelled_WhenSchedulerTokenIsCancelled()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();
        using var schedulerCts = new CancellationTokenSource();

        // Act
        var token = registry.Register(1, schedulerCts.Token);
        schedulerCts.Cancel();

        // Assert
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void RequestStop_ReturnsTrue_AndCancelsToken_WhenJobIsRegistered()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();
        var token = registry.Register(1, CancellationToken.None);

        // Act
        var result = registry.RequestStop(1);

        // Assert
        Assert.True(result);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void RequestStop_ReturnsFalse_WhenJobIsNotRegistered()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();

        // Act
        var result = registry.RequestStop(42);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequestStop_DoesNotAffectOtherRegisteredJobs()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();
        var tokenA = registry.Register(1, CancellationToken.None);
        var tokenB = registry.Register(2, CancellationToken.None);

        // Act
        registry.RequestStop(1);

        // Assert
        Assert.True(tokenA.IsCancellationRequested);
        Assert.False(tokenB.IsCancellationRequested);
    }

    [Fact]
    public void Unregister_RemovesJob_SoSubsequentRequestStopReturnsFalse()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();
        registry.Register(1, CancellationToken.None);

        // Act
        registry.Unregister(1);
        var result = registry.RequestStop(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Unregister_OfUnknownJob_DoesNotThrow()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();

        // Act
        var exception = Record.Exception(() => registry.Unregister(99));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Register_SameId_Twice_ReplacesPreviousRegistration()
    {
        // Arrange
        var registry = new CronJobExecutionRegistry();
        var firstToken = registry.Register(1, CancellationToken.None);

        // Act
        var secondToken = registry.Register(1, CancellationToken.None);
        var stopped = registry.RequestStop(1);

        // Assert
        Assert.True(stopped);
        Assert.True(secondToken.IsCancellationRequested);
        Assert.False(firstToken.IsCancellationRequested);
    }
}
