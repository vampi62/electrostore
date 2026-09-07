using System.Collections.Concurrent;

namespace ElectrostoreCRON.Services.CronJobExecutionRegistry;

public class CronJobExecutionRegistry : ICronJobExecutionRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _running = new();

    public CancellationToken Register(int idCronjob, CancellationToken schedulerToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(schedulerToken);
        _running[idCronjob] = cts;
        return cts.Token;
    }

    public void Unregister(int idCronjob)
    {
        if (_running.TryRemove(idCronjob, out var cts))
        {
            cts.Dispose();
        }
    }

    public bool RequestStop(int idCronjob)
    {
        if (_running.TryGetValue(idCronjob, out var cts))
        {
            cts.Cancel();
            return true;
        }
        return false;
    }
}
