using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;

namespace OrderProcessing.Phase4
{
    public class Phase4Runner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly IncomingOrderRepository _incomingRepo;
        private readonly OrderRepository _orderRepo;
        private readonly CustomerRepository _customerRepo;
        private readonly ProductRepository _productRepo;
        private readonly OrderProcessor _orderProcessor;
        private readonly IEmailService _emailService;
        private readonly JsonFileStore _store;

        public Phase4Runner(DataSeeder dataSeeder,IncomingOrderRepository incomingRepo,OrderRepository orderRepo,CustomerRepository customerRepo,ProductRepository productRepo,OrderProcessor orderProcessor,
            IEmailService emailService,JsonFileStore store)
        {
            _dataSeeder = dataSeeder;
            _incomingRepo = incomingRepo;
            _orderRepo = orderRepo;
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _orderProcessor = orderProcessor;
            _emailService = emailService;
            _store = store;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // ======================================
            // Task 10.1 - Live Checkout
            // ======================================

            Console.WriteLine("\n========== Task 10.1 ==========");

            var liveRunner = new LiveCheckoutRunner(_dataSeeder,_incomingRepo,_orderProcessor,_orderRepo);

            await liveRunner.RunAsync(ct);


            // ======================================
            // Task 10.2 - Customer Dashboard
            // ======================================

            Console.WriteLine("\n========== Task 10.2 ==========");

            var failPaymentsForCustomerIds = new List<int> { 99 };

            var customerDashboard = new CustomerDashboardService(_customerRepo,_orderRepo,_store,failPaymentsForCustomerIds);

            var dashboard = await customerDashboard.LoadAsync(3, ct);

            Console.WriteLine($"Orders: {dashboard.Orders.Count}");
            Console.WriteLine($"Payments: {dashboard.Payments.Count}");
            Console.WriteLine($"Emails: {dashboard.Emails.Count}");

            try
            {
                await customerDashboard.LoadAsync(99, ct);
            }
            catch (AggregateException)
            {
                Console.WriteLine("Dashboard loading failed as expected.");
            }


            // ======================================
            // Task 10.3 - Fastest Shipping Quote
            // ======================================

            Console.WriteLine("\n========== Task 10.3 ==========");

            var failForOrderIds = new List<int> { 6 };

            var fastShip = new FastShip(_productRepo,failForOrderIds);

            var cairoExpress = new CairoExpress(_productRepo,failForOrderIds);

            var nileCourier = new NileCourier(_productRepo,failForOrderIds);

            var carriers = new List<IShippingCarrier>
            {
                fastShip,
                cairoExpress,
                nileCourier
            };

            var shippingQuoteService = new ShippingQuoteService(carriers);

            var allIncomingOrders = await _incomingRepo.GetAllAsync(ct);

            // Test orders 1 to 5
            for (int orderId = 1; orderId <= 5; orderId++)
            {
                Order? currentOrder = null;

                foreach (var item in allIncomingOrders)
                {
                    if (item.Id == orderId)
                    {
                        currentOrder = item;
                        break;
                    }
                }

                if (currentOrder == null)
                {
                    Console.WriteLine($"Order #{orderId} not found.");
                    continue;
                }

                var quote = await shippingQuoteService.GetFastestQuoteAsync(
                    currentOrder,
                    ct);

                Console.WriteLine(
                    $"Order #{orderId} | Winner: {quote.Carrier} | " +
                    $"Price: {quote.Price} | Time: {quote.ElapsedMs} ms");
            }

            // Test order #6 - All carriers fail
            Order? order6 = null;

            foreach (var item in allIncomingOrders)
            {
                if (item.Id == 6)
                {
                    order6 = item;
                    break;
                }
            }

            if (order6 != null)
            {
                try
                {
                    await shippingQuoteService.GetFastestQuoteAsync(
                        order6,
                        ct);
                }
                catch (ShippingProviderUnavailableException ex)
                {
                    Console.WriteLine($"Order #6: {ex.Message}");
                }
            }


            // ======================================
            // Task 10.4 Part A - Fraud Scoring
            // ======================================

            Console.WriteLine("\n========== Task 10.4 Part A ==========");

            var orders = new List<Order>();

            for (int i = 1; i <= 10_000; i++)
            {
                orders.Add(new Order
                {
                    Id = i
                });
            }

            var fraudRunner = new FraudScoringRunner();

            // Sequential
            var sequentialStopwatch = Stopwatch.StartNew();

            var sequentialResult = fraudRunner.RunSequential(orders);

            sequentialStopwatch.Stop();

            Console.WriteLine("===== Sequential =====");
            Console.WriteLine($"Flagged: {sequentialResult.Count}");
            Console.WriteLine(
                $"Time: {sequentialStopwatch.ElapsedMilliseconds} ms");

            // Parallel
            var parallelStopwatch = Stopwatch.StartNew();

            var parallelResult = fraudRunner.RunParallel(orders, ct);

            parallelStopwatch.Stop();

            Console.WriteLine("\n===== Parallel =====");
            Console.WriteLine($"Flagged: {parallelResult.Count}");
            Console.WriteLine(
                $"Time: {parallelStopwatch.ElapsedMilliseconds} ms");

            // Compare results
            sequentialResult.Sort();
            parallelResult.Sort();

            bool sameResults = true;

            if (sequentialResult.Count != parallelResult.Count)
            {
                sameResults = false;
            }
            else
            {
                for (int i = 0; i < sequentialResult.Count; i++)
                {
                    if (sequentialResult[i] != parallelResult[i])
                    {
                        sameResults = false;
                        break;
                    }
                }
            }

            Console.WriteLine("\n===== Result Comparison =====");
            Console.WriteLine($"Same results: {sameResults}");

            Console.WriteLine("First 5 flagged IDs:");

            for (int i = 0; i < 5 && i < sequentialResult.Count; i++)
            {
                Console.WriteLine(sequentialResult[i]);
            }

            // Cancellation test with a separate token
            Console.WriteLine("\n===== Cancellation Test =====");

            using (var cancellationCts = new CancellationTokenSource())
            {
                cancellationCts.CancelAfter(500);

                try
                {
                    fraudRunner.RunParallel(
                        orders,
                        cancellationCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(
                        "Parallel scoring cancelled successfully.");
                }
            }

            // Performance report
            double speedup =
                (double)sequentialStopwatch.ElapsedMilliseconds /
                parallelStopwatch.ElapsedMilliseconds;

            Console.WriteLine("\n===== Final Report =====");
            Console.WriteLine($"Flagged count: {parallelResult.Count}");
            Console.WriteLine(
                $"Sequential time: {sequentialStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine(
                $"Parallel time: {parallelStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Speedup: {speedup:F2}x");
            Console.WriteLine(
                $"Processor count: {Environment.ProcessorCount}");


            // ======================================
            // Task 10.4 Part B - Email Resend
            // ======================================

            Console.WriteLine("\n========== Task 10.4 Part B ==========");

            var emailRunner = new EmailResendRunner(_emailService);

            await emailRunner.RunAsync(ct);

            Console.WriteLine("\n========== Phase 4 Finished ==========");
        }
    }
}