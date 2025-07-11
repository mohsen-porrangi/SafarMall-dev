namespace BuildingBlocks.Extensions;

public static class TaskExtensions
{
    public static void FireAndForgetSafeAsync(this Task task, Action<Exception>? onException = null)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.Exception != null)
                onException?.Invoke(t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
