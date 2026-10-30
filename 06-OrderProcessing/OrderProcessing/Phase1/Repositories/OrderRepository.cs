
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1.Repositories;

public sealed class OrderRepository
{
    private readonly JsonFileStore _store;

    public OrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task SaveAsync(Order order,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to orders.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync("Beginning save transaction...",100, 100, ct);

        await SimulatedLatency.DelayAsync($"Inserting/Updating Order #{order.Id}...",400, 400, ct);

        await _store.UpdateAsync<Order>("orders.json",orders =>
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    if (orders[i].Id == order.Id)
                    {
                        orders[i] = order;
                        return;
                    }
                }

                orders.Add(order);
            },
            ct);

        await SimulatedLatency.DelayAsync($"Committing Order #{order.Id}...",200, 200, ct);

        Log.Success($"Order #{order.Id} saved.");
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to orders.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync("Executing query for all orders...",300, 600, ct);

        var orders = await _store.ReadAsync<Order>("orders.json", ct);

        Log.Success($"Retrieved {orders.Count} orders.");

        return orders;
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus status,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to orders.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync($"Executing query for status {status}...",300, 600, ct);

        var orders = await _store.ReadAsync<Order>("orders.json", ct);

        var result = new List<Order>();

        foreach (var order in orders)
        {
            if (order.Status == status)
            {
                result.Add(order);
            }
        }

        Log.Success($"Retrieved {result.Count} orders with status {status}.");

        return result;
    }
}
