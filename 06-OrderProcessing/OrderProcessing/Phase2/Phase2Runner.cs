using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class Phase2Runner
    {
   
        private readonly DataSeeder _dataSeeder;
        private readonly RaceConditionDemo _raceConditionDemo;
        private readonly OrderPricer _orderPricer;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderValidator _orderValidator; 
        private readonly OrderRepository _orderRepository;
        private readonly OrderProcessorV2 _orderProcessorV2;
        private readonly ProductRepository _productRepository;
        private readonly ReportGenerator _reportGenerator;


        public Phase2Runner( DataSeeder dataSeeder, RaceConditionDemo raceConditionDemo, OrderPricer orderPricer, 
            IncomingOrderRepository incomingOrderRepository, OrderValidator orderValidator, 
            OrderRepository orderRepository, OrderProcessorV2 orderProcessorV2, ProductRepository productRepository, ReportGenerator reportGenerator)
        {
            _dataSeeder = dataSeeder;
    
            _raceConditionDemo = raceConditionDemo;
            _orderPricer = orderPricer;
            _incomingOrderRepository = incomingOrderRepository;
            _orderValidator = orderValidator;
            _orderRepository = orderRepository;
             _orderProcessorV2 = orderProcessorV2;
            _productRepository = productRepository;
            _reportGenerator = reportGenerator;
        }
        public async Task RunAsync()
        {
            await _dataSeeder.ResetAsync();
          await _raceConditionDemo.RunAsync();
            await _dataSeeder.ResetAsync();
            var orders = await _incomingOrderRepository.GetAllAsync();
            List<Task> validationTasks = new List<Task>();
            List<Task<bool>> processingTasks = new List<Task<bool>>();
            foreach (var incomingOrder in orders)
            {
                if(incomingOrder.Id== 1)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    decimal parallelTotal = await _orderPricer.PriceOrderAsync(incomingOrder, CancellationToken.None);

                    stopwatch.Stop();
                    long parallelTime = stopwatch.ElapsedMilliseconds;

                    Console.WriteLine($"Parallel Pricing: {parallelTime} ms");
                    Stopwatch sequentialStopwatch = Stopwatch.StartNew();

                    decimal sequentialTotal = await _orderPricer.PriceOrderSequentialAsync(incomingOrder, CancellationToken.None);

                    sequentialStopwatch.Stop();

                    long sequentialTime = sequentialStopwatch.ElapsedMilliseconds;

                    Console.WriteLine($"Sequential Pricing: {sequentialTime} ms");

                    Console.WriteLine($"Parallel Total: {parallelTotal}");
                    Console.WriteLine($"Sequential Total: {sequentialTotal}");

                    Console.WriteLine($"Same Result: {parallelTotal == sequentialTotal}");

                    Console.WriteLine($"Time Difference: {Math.Abs(parallelTime - sequentialTime)} ms");

                }
                validationTasks.Add(_orderValidator.ValidateOrderAsync(incomingOrder, CancellationToken.None));
            }
            foreach (var task in validationTasks)
            {
                await task;
            }
         var validatedOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Validated,CancellationToken.None);
            Stopwatch processingStopwatch = Stopwatch.StartNew();
            foreach (var order in validatedOrders)
            {
                processingTasks.Add(_orderProcessorV2.ProcessValidatedOrderAsync(order, CancellationToken.None));
                if (processingTasks.Count == 1)
                {
                    Console.WriteLine($"First Task Status: {processingTasks[0].Status}");
                }

            }
            await Task.Delay(1000);
           
            foreach (var task in processingTasks)
            {
                Console.WriteLine($"Task Status: {task.Status}");
            }
            int i = 0;
            foreach (var task in processingTasks)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Task Status: {task.Status}");
                    var order1 = validatedOrders[i];
                    order1.Status = OrderStatus.Failed;
                    order1.FailureReason = $"Error processing order: {ex.Message}";
                    foreach (var item in order1.Items)
                    {
                        await _productRepository.ReleaseStockAsync(item.ProductId,item.Quantity,CancellationToken.None);
                    }
                  
                    await _orderRepository.SaveAsync(order1, CancellationToken.None);

                }
                finally
                {
                    i++;
                }
            }
            foreach (var task in processingTasks)
            {
                Console.WriteLine($"Final Task Status: {task.Status}");
            }
            processingStopwatch.Stop();

            Console.WriteLine($"Total Processing Time: {processingStopwatch.ElapsedMilliseconds} ms");

            var allOrders = await _orderRepository.GetAllAsync();

            Console.WriteLine($"Total Orders in orders.json: {allOrders.Count}");
            var completed = await _orderRepository.GetByStatusAsync(OrderStatus.Completed, CancellationToken.None);

            var rejected = await _orderRepository.GetByStatusAsync(OrderStatus.Rejected, CancellationToken.None);

            var failed = await _orderRepository.GetByStatusAsync(OrderStatus.Failed, CancellationToken.None);

            Console.WriteLine($"Completed Orders: {completed.Count}");
            Console.WriteLine($"Rejected Orders: {rejected.Count}");
            Console.WriteLine($"Failed Orders: {failed.Count}");

            Task<Report> reportTask = _reportGenerator.GenerateAsync();

            Console.WriteLine("Generating report...");

            Report report = await reportTask;
            Console.WriteLine("\n===== REPORT =====");
            Console.WriteLine($"Total Orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}");
            Console.WriteLine($"Rejected: {report.RejectedCount}");
            Console.WriteLine($"Failed: {report.FailedCount}");
            Console.WriteLine($"Payment Failed: {report.PaymentFailedCount}");
            Console.WriteLine($"Total Revenue: {report.TotalRevenue}");
            Console.WriteLine($"Average Order Value: {report.AverageOrderValue}");
            Console.WriteLine($"Top Selling Product: {report.TopSellingProduct}");
            Console.WriteLine($"Top Selling Quantity: {report.TopSellingQuantity}");
            Console.WriteLine($"Generated At: {report.GeneratedAt}");
        }
    }
}
