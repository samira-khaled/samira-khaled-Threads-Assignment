
using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class Phase3Runner
{
    private readonly JsonFileStore _store;
    private readonly DataSeeder _dataSeeder;

    private readonly CustomerRepository _customerRepo;
    private readonly ProductRepository _productRepo;
    private readonly CouponRepository _couponRepo;
    private readonly IncomingOrderRepository _incomingRepo;
    private readonly OrderRepository _orderRepo;

    private readonly OrderProcessor _orderProcessor;
    private readonly EmailService _emailService;

    public Phase3Runner(JsonFileStore store,DataSeeder dataSeeder,CustomerRepository customerRepo,ProductRepository productRepo,
        CouponRepository couponRepo,IncomingOrderRepository incomingRepo,OrderRepository orderRepo,OrderProcessor orderProcessor,EmailService emailService)
    {
        _store = store;
        _dataSeeder = dataSeeder;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _couponRepo = couponRepo;
        _incomingRepo = incomingRepo;
        _orderRepo = orderRepo;
        _orderProcessor = orderProcessor;
        _emailService = emailService;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("              PHASE 3");
        Console.WriteLine("======================================");

        // 1. Create Sync Repositories
        var syncCustomerRepo = new SyncCustomerRepository(_store);
        var syncProductRepo = new SyncProductRepository(_store);
        var syncCouponRepo = new SyncCouponRepository(_store);

        // 2. Create Sync Services
        var syncCustomerService = new SyncCustomerService(syncCustomerRepo);

        var syncInventoryService = new SyncInventoryService(syncProductRepo);

        var syncPricingService = new SyncPricingService(syncCouponRepo,syncCustomerRepo,syncProductRepo,Array.Empty<int>());

        var syncPaymentGateway = new SyncPaymentGateway(_store,new List<int> { 5 },new List<int> { 8 },true);

        var syncEmailService = new SyncEmailService(_store,
            new Dictionary<int, int>
            {
                { 3, 2 },
                { 17, int.MaxValue }
            },
            true);

        // 3. Create Sync Order Processor
        var syncOrderProcessor = new SyncOrderProcessor(syncCustomerService,_orderRepo,syncInventoryService,syncPricingService,syncPaymentGateway,syncEmailService);

        // 4. Get Orders
        await _dataSeeder.ResetAsync(ct);

        var allOrders = await _incomingRepo.GetAllAsync(ct);

        var orders = new List<Order>();

        foreach (var order in allOrders)
        {
            if (order.Id >= 1 && order.Id <= 10)
            {
                orders.Add(order);
            }
        }

        Console.WriteLine($"Orders to process: {orders.Count}");

        // 5. Sync vs Async Comparison
        var comparison = new SyncVsAsyncComparison(_store,syncOrderProcessor,_orderProcessor,_dataSeeder);

        await comparison.CompareAsync(orders);

        // 6. Fire and Forget Demo
        var firstOrder = orders[0];

        var customer = await _customerRepo.GetByIdAsync(firstOrder.CustomerId,ct);

        if (customer != null)
        {
            var demo = new FireAndForgetDemo(_orderRepo,_emailService);

            await demo.ProcessOrderAsync(firstOrder, customer);
        }

        Console.WriteLine();
        Console.WriteLine("========== PHASE 3 FINISHED ==========");
    }
}
