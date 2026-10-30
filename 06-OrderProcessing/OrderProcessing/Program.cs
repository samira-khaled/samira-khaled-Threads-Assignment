using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

var store = new JsonFileStore();
var seeder = new DataSeeder(store);

await seeder.ResetAsync();

var customerRepo = new CustomerRepository(store);
var productRepo = new ProductRepository(store);
var couponRepo = new CouponRepository(store);
var orderRepo = new OrderRepository(store);
var incomingRepo = new IncomingOrderRepository(store);

Console.WriteLine("\n===== Repository Read Tests =====");

// Customer
var customer = await customerRepo.GetByIdAsync(1);
Console.WriteLine($"Customer: {customer?.Name ?? "Not found"}");

// Customer not found
var missingCustomer = await customerRepo.GetByIdAsync(999);
Console.WriteLine($"Missing customer: {missingCustomer?.Name ?? "Not found"}");

// Product
var product = await productRepo.GetByIdAsync(1);
Console.WriteLine($"Product: {product?.Name ?? "Not found"}");

// Coupon
var coupon = await couponRepo.GetByCodeAsync("OLD20");
Console.WriteLine($"Coupon: {coupon?.Code ?? "Not found"}");

// Case-insensitive coupon search
var couponLower = await couponRepo.GetByCodeAsync("old20");
Console.WriteLine($"Lowercase search: {couponLower?.Code ?? "Not found"}");

// Incoming orders
var incomingOrders = await incomingRepo.GetAllAsync();
Console.WriteLine($"Incoming orders count: {incomingOrders.Count}");

// All saved orders
var orders = await orderRepo.GetAllAsync();
Console.WriteLine($"Saved orders count: {orders.Count}");

Console.WriteLine("\n===== Repository Update Tests =====");

// Check product stock before reservation
var productBefore = await productRepo.GetByIdAsync(1);
Console.WriteLine($"Stock before: {productBefore?.StockQuantity}");

// Reserve stock
var reserved = await productRepo.ReserveStockAsync(1, 2);
Console.WriteLine($"Stock reserved: {reserved}");

// Check product stock after reservation
var productAfter = await productRepo.GetByIdAsync(1);
Console.WriteLine($"Stock after: {productAfter?.StockQuantity}");

// Try invalid reservation
var invalidReservation = await productRepo.ReserveStockAsync(999, 2);
Console.WriteLine($"Invalid reservation: {invalidReservation}");

// Save an order
var testOrder = new Order
{
    Id = 100,
    CustomerId = 1,
    Status = OrderStatus.Pending
};

await orderRepo.SaveAsync(testOrder);

// Read orders
var savedOrders = await orderRepo.GetAllAsync();
Console.WriteLine($"Orders after save: {savedOrders.Count}");

// Update the same order
testOrder.Status = OrderStatus.Validated;
await orderRepo.SaveAsync(testOrder);

savedOrders = await orderRepo.GetAllAsync();

Console.WriteLine($"Orders after update: {savedOrders.Count}");
Console.WriteLine($"Updated status: {savedOrders[0].Status}");
