using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.CronSchedulerService;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class ElectrostoreCronJobTests
{
    private readonly Mock<CronJobsGrpc.CronJobsGrpcClient> _apiClient = new();
    private readonly Mock<ILogger<ElectrostoreCronJob>> _logger = new();

    private ElectrostoreCronJob CreateJob() => new(_apiClient.Object, _logger.Object);

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static AsyncUnaryCall<TResponse> CreateFailingAsyncUnaryCall<TResponse>(Exception exception)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => Status.DefaultCancelled,
            () => new Metadata(),
            () => { });
    }

    private static Mock<IJobExecutionContext> CreateContext(int id, CronJobAction? action, DateTimeOffset? nextFireTimeUtc = null)
    {
        var dataMap = new JobDataMap();
        dataMap.Put(ElectrostoreCronJob.KeyId, id);
        if (action.HasValue)
        {
            dataMap.Put(ElectrostoreCronJob.KeyAction, (int)action.Value);
        }
        dataMap.Put(ElectrostoreCronJob.KeyParams, string.Empty);

        var jobDetail = new Mock<IJobDetail>();
        jobDetail.SetupGet(d => d.JobDataMap).Returns(dataMap);

        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.JobDetail).Returns(jobDetail.Object);
        context.SetupGet(c => c.NextFireTimeUtc).Returns(nextFireTimeUtc);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
