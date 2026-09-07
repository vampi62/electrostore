namespace ElectrostoreCRON.Services.CronJobExecutionRegistry;

public interface ICronJobExecutionRegistry
{
    // Registers a running job instance and returns a token that is cancelled either when the
    // scheduler cancels the job, or when RequestStop is called for the same id_cronjob.
    CancellationToken Register(int idCronjob, CancellationToken schedulerToken);

    void Unregister(int idCronjob);

    // Returns true if a running instance was found and asked to stop.
    bool RequestStop(int idCronjob);
}
