
using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Phase4;
using OrderProcessing.Shared.Storage;

// ======================================
// 1. Cancellation Token
// ======================================

var cts = new CancellationTokenSource();
var ct = cts.Token;

// ======================================
// 2. Storage & Seeder
// ======================================

var store = new JsonFileStore();
var dataSeeder = new DataSeeder(store);

// ======================================
// 3. Failure Configurations
// ======================================

var timeoutOrderIds = new List<int> { 5 };
var declineOrderIds = new List<int> { 8 };
var failSaveForOrderIds = new List<int> { 13 };

var failAttempts = new Dictionary<int, int>
{
    { 3, 2 },
    { 17, int.MaxValue }
};

bool enableFailures = true;

// ======================================
// 4. Async Repositories
// ======================================

var customerRepo = new CustomerRepository(store);
var productRepo = new ProductRepository(store);
var couponRepo = new CouponRepository(store);
var incomingRepo = new IncomingOrderRepository(store);

// Phase 2: بدون Database Failure
var phase2OrderRepo = new OrderRepository(store,failSaveForOrderIds,false);

// Phase 3: مع Database Failure
var phase3OrderRepo = new OrderRepository(
    store,
    failSaveForOrderIds,
    true
);

// ======================================
// 5. Sync Repositories
// ======================================

var syncCustomerRepo = new SyncCustomerRepository(store);
var syncProductRepo = new SyncProductRepository(store);
var syncCouponRepo = new SyncCouponRepository(store);

// ======================================
// 6. Validators
// ======================================

var syncValidator = new SyncOrderValidator(syncCustomerRepo,syncProductRepo,syncCouponRepo);

var validator = new OrderValidator(customerRepo, phase2OrderRepo, productRepo,couponRepo);

// ======================================
// 7. Phase 1
// ======================================

Console.WriteLine("\n\n######################################");
Console.WriteLine("             PHASE 1");
Console.WriteLine("######################################");

var phase1Runner = new Phase1Runner(dataSeeder,incomingRepo, phase2OrderRepo, syncValidator,validator);

//await phase1Runner.RunAsync(ct);

// ======================================
// 8. Phase 2 Dependencies
// ======================================

Console.WriteLine("\n\n######################################");
Console.WriteLine("             PHASE 2");
Console.WriteLine("######################################");

await dataSeeder.ResetAsync(ct);

var phase2Orders = await incomingRepo.GetAllAsync(ct);

var raceOrder = phase2Orders.First(o => o.Id == 1);

var phase2Pricing = new OrderProcessing.Phase2.PricingService(productRepo,customerRepo,couponRepo,new[] { 7 });

var pricer = new OrderPricer(phase2Pricing);

var processorV2 = new OrderProcessorV2(productRepo, phase2OrderRepo, pricer,new InvoiceGenerator());

var raceConditionDemo = new RaceConditionDemo(
    store,
    new Race_condition_on_the_last_keyboard(
        raceOrder,
        productRepo,
        store
    )
);

var reportGenerator = new ReportGenerator(
    phase2OrderRepo,
    productRepo
);

// ======================================
// 9. Phase 2 Runner
// ======================================

var phase2Runner = new Phase2Runner(dataSeeder,raceConditionDemo,pricer,
    incomingRepo,validator, phase2OrderRepo, processorV2,productRepo,reportGenerator
);

//await phase2Runner.RunAsync();

// ======================================
// 10. Phase 3 Dependencies
// ======================================

Console.WriteLine("\n\n######################################");
Console.WriteLine("             PHASE 3");
Console.WriteLine("######################################");

// Async Services

var customerService = new CustomerService(customerRepo);

var inventoryService = new InventoryService(productRepo);

var phase3PricingService = new OrderProcessing.Phase3.Services.PricingService(
    couponRepo,customerRepo,productRepo,Array.Empty<int>());

var paymentGateway = new PaymentGateway(store,timeoutOrderIds,declineOrderIds,enableFailures);

var emailService = new EmailService(store,failAttempts,enableFailures);

// Async Order Processor

var orderProcessor = new OrderProcessor(customerService, phase3OrderRepo, inventoryService,productRepo,
    phase3PricingService,paymentGateway,emailService);

// ======================================
// 11. Phase 3 Runner
// ======================================

var phase3Runner = new Phase3Runner(store,dataSeeder,customerRepo,productRepo,couponRepo,
    incomingRepo, phase3OrderRepo, orderProcessor,emailService
);

//await phase3Runner.RunAsync(ct);

// ======================================
// 12. Phase 4 Runner
// ======================================

Console.WriteLine("\n\n######################################");
Console.WriteLine("             PHASE 4");
Console.WriteLine("######################################");

var phase4Runner = new Phase4Runner(dataSeeder,incomingRepo, phase3OrderRepo, customerRepo,productRepo,orderProcessor,emailService,store);

await phase4Runner.RunAsync(ct);

// ======================================
// 13. Project Finished
// ======================================

Console.WriteLine("\n\n######################################");
Console.WriteLine("          PROJECT FINISHED");
Console.WriteLine("######################################");

cts.Dispose();
