using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;

namespace BackEnd_UnitTest._Shared;

/// <summary>
/// Initializes Hangfire's <see cref="JobStorage.Current"/> exactly once per process so any test
/// touching <c>BackgroundJob.Schedule(...)</c> (e.g. <c>ExamStatusJob.ScheduleExamJobs</c>) does not
/// throw "JobStorage.Current property value has not been initialized".
/// </summary>
public static class HangfireTestSetup
{
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            JobStorage.Current = new MemoryStorage();
        }
    }
}
