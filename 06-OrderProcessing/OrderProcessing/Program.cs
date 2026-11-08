
using OrderProcessing.Shared.Storage;
using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;

var ct = CancellationToken.None;

var store = new JsonFileStore();

var dataSeeder = new DataSeeder(store);

// Repositories
var customerRepo = new CustomerRepository(store);
var productRepo = new ProductRepository(store);
var couponRepo = new CouponRepository(store);
var incomingRepo = new IncomingOrderRepository(store);
var orderRepo = new OrderRepository(store);

// Sync Repositories
var syncCustomerRepo = new SyncCustomerRepository(store);
var syncProductRepo = new SyncProductRepository(store);
var syncCouponRepo = new SyncCouponRepository(store);

// Validators
var syncValidator = new SyncOrderValidator(syncCustomerRepo, syncProductRepo, syncCouponRepo);

var validator = new OrderValidator(customerRepo,orderRepo, productRepo, couponRepo);

// Phase 1
var phase1Runner = new Phase1Runner( dataSeeder, incomingRepo, orderRepo, syncValidator, validator);

await phase1Runner.RunAsync(ct);


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
