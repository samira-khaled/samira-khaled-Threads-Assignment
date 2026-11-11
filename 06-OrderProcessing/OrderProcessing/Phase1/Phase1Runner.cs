using System.Diagnostics;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public sealed class Phase1Runner
{
    private readonly DataSeeder _seeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private readonly OrderRepository _orderRepository;
    private readonly SyncOrderValidator _syncValidator;
    private readonly OrderValidator _asyncValidator;

    public Phase1Runner(DataSeeder seeder,IncomingOrderRepository incomingOrderRepository,OrderRepository orderRepository,SyncOrderValidator syncValidator,OrderValidator asyncValidator)
    {
        _seeder = seeder;
        _incomingOrderRepository = incomingOrderRepository;
        _orderRepository = orderRepository;
        _syncValidator = syncValidator;
        _asyncValidator = asyncValidator;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Reset and load orders
        await _seeder.ResetAsync();
        var orders = await _incomingOrderRepository.GetAllAsync();

        // Sync run
        var syncOrders = orders.Select(order => new Order
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Items = order.Items,
                CouponCode = order.CouponCode,
                Status = order.Status,
                FailureReason = order.FailureReason
            })
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        foreach (var order in syncOrders)
        {
            _syncValidator.ValidateOrder(order);
        }

        stopwatch.Stop();
        var syncTime = stopwatch.Elapsed;

        // Reset again
        await _seeder.ResetAsync();

        // Async run
        stopwatch.Restart();

        foreach (var order in orders)
        {
            await _asyncValidator.ValidateOrderAsync(order, ct);
        }

        stopwatch.Stop();
        var asyncTime = stopwatch.Elapsed;

        // Read results from orders.json
        var validated = await _orderRepository.GetByStatusAsync(OrderStatus.Validated);
        var rejected = await _orderRepository.GetByStatusAsync(OrderStatus.Rejected);
        var allOrders = await _orderRepository.GetAllAsync();

        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {allOrders.Count}");
        Console.WriteLine($"Validated:    {validated.Count}");
        Console.WriteLine($"Rejected:     {rejected.Count}");

        foreach (var order in rejected)
        {
            Console.WriteLine($"  - Order #{order.Id}: {order.FailureReason}");
        }

        Console.WriteLine($"Sync Time:  {syncTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"Async Time: {asyncTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"orders.json now contains {allOrders.Count} records.");
    }
}