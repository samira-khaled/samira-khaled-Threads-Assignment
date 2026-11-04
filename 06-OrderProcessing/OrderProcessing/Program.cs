
using OrderProcessing.Shared.Storage;
using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;

var ct = CancellationToken.None;

var store = new JsonFileStore();

var dataSeeder = new DataSeeder(store);

var customerRepo = new CustomerRepository(store);
var productRepo = new ProductRepository(store);
var couponRepo = new CouponRepository(store);
var incomingRepo = new IncomingOrderRepository(store);
var orderRepo = new OrderRepository(store);
var reportGenerator = new ReportGenerator(orderRepo, productRepo);

// Seed once to get an order for RaceConditionDemo
await dataSeeder.ResetAsync(ct);

var orders = await incomingRepo.GetAllAsync(ct);
var order = orders.First(o => o.Id == 1);

var pricing = new PricingService(
    productRepo,
    customerRepo,
    couponRepo,
    new[] { 7 });

var pricer = new OrderPricer(pricing);

var validator = new OrderValidator(
    customerRepo,
    orderRepo,
    productRepo,
    couponRepo);

var processor = new OrderProcessorV2(
    productRepo,
    orderRepo,
    pricer,
    new InvoiceGenerator());

var raceConditionDemo = new RaceConditionDemo(
    store,
    new Race_condition_on_the_last_keyboard(
        order, productRepo, store));

var phase2Runner = new Phase2Runner(
    dataSeeder,
    raceConditionDemo,
    pricer,
    incomingRepo,
    validator,
    orderRepo,
    processor,
    productRepo, reportGenerator);

await phase2Runner.RunAsync();
