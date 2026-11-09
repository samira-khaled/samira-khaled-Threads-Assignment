
//using OrderProcessing.Shared.Storage;
//using OrderProcessing.Phase1;
//using OrderProcessing.Phase1.Repositories;
//using OrderProcessing.Phase2;

//var ct = CancellationToken.None;

//var store = new JsonFileStore();

//var dataSeeder = new DataSeeder(store);

//// Repositories
//var customerRepo = new CustomerRepository(store);
//var productRepo = new ProductRepository(store);
//var couponRepo = new CouponRepository(store);
//var incomingRepo = new IncomingOrderRepository(store);
//var orderRepo = new OrderRepository(store);

//// Sync Repositories
//var syncCustomerRepo = new SyncCustomerRepository(store);
//var syncProductRepo = new SyncProductRepository(store);
//var syncCouponRepo = new SyncCouponRepository(store);

//// Validators
//var syncValidator = new SyncOrderValidator(syncCustomerRepo, syncProductRepo, syncCouponRepo);

//var validator = new OrderValidator(customerRepo,orderRepo, productRepo, couponRepo);

//// Phase 1
//var phase1Runner = new Phase1Runner( dataSeeder, incomingRepo, orderRepo, syncValidator, validator);

//await phase1Runner.RunAsync(ct);


// ======================================
// Phase 2
// ======================================

// Seed once to get an order for RaceConditionDemo
// await dataSeeder.ResetAsync(ct);

// var orders = await incomingRepo.GetAllAsync(ct);
// var order = orders.First(o => o.Id == 1);

// var pricing = new PricingService(
//     productRepo,
//     customerRepo,
//     couponRepo,
//     new[] { 7 });

// var pricer = new OrderPricer(pricing);

// var processor = new OrderProcessorV2(
//     productRepo,
//     orderRepo,
//     pricer,
//     new InvoiceGenerator());

// var raceConditionDemo = new RaceConditionDemo(
//     store,
//     new Race_condition_on_the_last_keyboard(
//         order, productRepo, store));

// var reportGenerator = new ReportGenerator(orderRepo, productRepo);

// var phase2Runner = new Phase2Runner(
//     dataSeeder,
//     raceConditionDemo,
//     pricer,
//     incomingRepo,
//     validator,
//     orderRepo,
//     processor,
//     productRepo,
//     reportGenerator);

// await phase2Runner.RunAsync();



using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var ct = CancellationToken.None;

// ======================================
// Storage & Seeder
// ======================================

var store = new JsonFileStore();
var dataSeeder = new DataSeeder(store);

// ======================================
// Failure Configurations
// ======================================

var timeoutOrderIds = new List<int> { 5 };
var declineOrderIds = new List<int> { 8 };
var failSaveForOrderIds = new List<int> { 13 };

var failAttempts = new Dictionary<int, int>
{
    { 3, 2 },
    { 17, int.MaxValue }
};

bool enableFailures = false;

// ======================================
// Async Repositories
// ======================================

var customerRepo = new CustomerRepository(store);
var productRepo = new ProductRepository(store);
var couponRepo = new CouponRepository(store);
var incomingRepo = new IncomingOrderRepository(store);

var orderRepo = new OrderRepository(
    store,
    failSaveForOrderIds,
    enableFailures
);

// ======================================
// Sync Repositories
// ======================================

var syncCustomerRepo = new SyncCustomerRepository(store);
var syncProductRepo = new SyncProductRepository(store);
var syncCouponRepo = new SyncCouponRepository(store);

// ======================================
// Async Services
// ======================================

var customerService = new CustomerService(customerRepo);

var inventoryService = new InventoryService(productRepo);

var pricingService = new PricingService(
    couponRepo,
    customerRepo,
    productRepo,
    Array.Empty<int>()
);

var paymentGateway = new PaymentGateway(
    store,
    timeoutOrderIds,
    declineOrderIds,
    enableFailures
);

var emailService = new EmailService(
    store,
    failAttempts,
    enableFailures
);

// ======================================
// Async Order Processor
// ======================================

var orderProcessor = new OrderProcessor(
    customerService,
    orderRepo,
    inventoryService,
    productRepo,
    pricingService,
    paymentGateway,
    emailService
);

// ======================================
// Sync Services
// ======================================

var syncCustomerService = new SyncCustomerService(
    syncCustomerRepo
);

var syncInventoryService = new SyncInventoryService(
    syncProductRepo
);

var syncPricingService = new SyncPricingService(
    syncCouponRepo,
    syncCustomerRepo,
    syncProductRepo,
    Array.Empty<int>()
);

var syncPaymentGateway = new SyncPaymentGateway(
    store,
    timeoutOrderIds,
    declineOrderIds,
    enableFailures
);

var syncEmailService = new SyncEmailService(
    store,
    failAttempts,
    enableFailures
);

// ======================================
// Sync Order Processor
// ======================================

var syncOrderProcessor = new SyncOrderProcessor(syncCustomerService,orderRepo, syncInventoryService, syncPricingService, syncPaymentGateway, syncEmailService);

// ======================================
// Get Orders
// ======================================

await dataSeeder.ResetAsync(ct);

var allOrders = await incomingRepo.GetAllAsync(ct);

var orders = new List<Order>();

foreach (var order1 in allOrders)
{
    if (order1.Id >= 1 && order1.Id <= 10)
    {
        orders.Add(order1);
    }
}

Console.WriteLine($"Orders to process: {orders.Count}");

// ======================================
// Sync vs Async Comparison
// ======================================

var comparison = new SyncVsAsyncComparison(
    store,
    syncOrderProcessor,
    orderProcessor,
    dataSeeder
);

//await comparison.CompareAsync(orders);

var order = allOrders[0];
var customer = await customerRepo.GetByIdAsync(order.CustomerId, ct);


var demo = new FireAndForgetDemo(orderRepo, emailService);

await demo.ProcessOrderAsync(order, customer);



