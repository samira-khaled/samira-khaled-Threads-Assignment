namespace OrderProcessing.Shared.Logging;

public static class SimulatedLatency
{
    public static async Task DelayAsync(string message,int minMilliseconds,int maxMilliseconds,CancellationToken ct = default)
    {
        Log.Info(message);// بدانا فتح اتصال

        int delay = Random.Shared.Next(minMilliseconds,maxMilliseconds + 1);

        await Task.Delay(delay, ct);
    }
}