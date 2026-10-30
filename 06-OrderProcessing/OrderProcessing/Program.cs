using OrderProcessing.Shared.Storage;
using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;

var store = new JsonFileStore();

var seeder = new DataSeeder(store);

var incomingOrderRepository = new IncomingOrderRepository(store);
var orderRepository = new OrderRepository(store);

var customerRepository = new SyncCustomerRepository(store);
var productRepository = new SyncProductRepository(store);
var couponRepository = new SyncCouponRepository(store);

var syncValidator = new SyncOrderValidator(
    customerRepository,
    productRepository,
    couponRepository);

var asyncCustomerRepository = new CustomerRepository(store);
var asyncProductRepository = new ProductRepository(store);
var asyncCouponRepository = new CouponRepository(store);

var asyncValidator = new OrderValidator(
    asyncCustomerRepository,
    orderRepository,
    asyncProductRepository,
    asyncCouponRepository);

var runner = new Phase1Runner(
    seeder,
    incomingOrderRepository,
    orderRepository,
    syncValidator,
    asyncValidator);

await runner.RunAsync();