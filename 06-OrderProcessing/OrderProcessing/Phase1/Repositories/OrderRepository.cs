
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Security.Cryptography;

namespace OrderProcessing.Phase1.Repositories;

public sealed class OrderRepository
{
    private readonly JsonFileStore _store;
    private readonly List<int> _failSaveForOrderIds;


    public OrderRepository(JsonFileStore store, List<int> failSaveForOrderIds,bool enableFailures)
    {
        _store = store;
        _failSaveForOrderIds = enableFailures ? failSaveForOrderIds : new List<int>();
        
    }

    public async Task SaveAsync(Order order,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to orders.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync("Beginning save transaction...",100, 100, ct);

        await SimulatedLatency.DelayAsync($"Inserting/Updating Order #{order.Id}...",400, 400, ct);
        foreach (var failId in _failSaveForOrderIds)
        {
            if (order.Id == failId && order.Status == OrderStatus.Completed)
            {
                throw new DatabaseWriteException("Connection lost during commit");
            }
        }

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
    public  void Save(Order order)
    {
        Thread.Sleep(200);
        Thread.Sleep(100);
        Thread.Sleep(400);
        foreach (var failId in _failSaveForOrderIds)
        {
            if (order.Id == failId && order.Status == OrderStatus.Completed)
            {
                throw new DatabaseWriteException("Connection lost during commit");
            }
        }

         _store.Update<Order>("orders.json", orders =>
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
        });

      Thread.Sleep(200);

        Log.Success($"Order #{order.Id} saved.");
    }

}
