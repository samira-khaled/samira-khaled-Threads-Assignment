using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System.Diagnostics;

namespace OrderProcessing.Phase4;

public class ShippingQuoteService
{
    private readonly IEnumerable<IShippingCarrier> _carriers;

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = carriers;
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(Order order,CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource();

        using var linkedCts =CancellationTokenSource.CreateLinkedTokenSource(ct,timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();

        var carrierTasks = new List<Task<ShippingQuote>>();

        foreach (var carrier in _carriers)
        {
            var task = GetCarrierQuoteAsync(carrier,order,linkedCts.Token);

            carrierTasks.Add(task);
        }

        var remainingTasks = new List<Task<ShippingQuote>>(carrierTasks);

        Task timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(1200),ct);

        while (remainingTasks.Count > 0)
        {
            var waitingTasks = new List<Task>();

            foreach (var task in remainingTasks)
            {
                waitingTasks.Add(task);
            }

            waitingTasks.Add(timeoutTask);

            Task completedTask = await Task.WhenAny(waitingTasks);

            if (completedTask == timeoutTask)
            {
                ct.ThrowIfCancellationRequested();

                timeoutCts.Cancel();

                break;
            }

            var completedQuoteTask =(Task<ShippingQuote>)completedTask;

            remainingTasks.Remove(completedQuoteTask);

            try
            {
                var quote = await completedQuoteTask;

                linkedCts.Cancel();

                return quote;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // الشركة فشلت، نكمل مع باقي الشركات
            }
        }

        linkedCts.Cancel();

        ct.ThrowIfCancellationRequested();

        if (stopwatch.ElapsedMilliseconds >= 1200)
        {
            return new ShippingQuote
            {
                Carrier = "Flat rate",
                Price = 25.00m,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        throw new ShippingProviderUnavailableException("All carriers failed");
    }

    private async Task<ShippingQuote> GetCarrierQuoteAsync(IShippingCarrier carrier,Order order,CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            decimal price = await carrier.GetQuoteAsync(order, ct);

            return new ShippingQuote
            {
                Carrier = carrier.GetType().Name,
                Price = price,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            Log.Info($"[{carrier.GetType().Name}] cancelled");
            throw;
        }
    }
}