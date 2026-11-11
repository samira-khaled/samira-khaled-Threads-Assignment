
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
// Add the namespace containing Order

namespace OrderProcessing.Phase1.Repositories;

public sealed class IncomingOrderRepository
{
    private readonly JsonFileStore _store;

    public IncomingOrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to incoming_orders.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync("Executing query for incoming orders...",300, 600, ct);

        var orders = await _store.ReadAsync<Order>("incoming_orders.json", ct);

        Log.Success($"Retrieved {orders.Count} incoming orders.");

        return orders;
    }
}
