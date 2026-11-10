using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public class LiveCheckoutRunner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderProcessor _processor;
        private readonly OrderRepository _orderRepository;
        public LiveCheckoutRunner(DataSeeder dataSeeder,IncomingOrderRepository incomingOrderRepository,OrderProcessor processor,OrderRepository orderRepository)
        {

            _dataSeeder = dataSeeder;
            _incomingOrderRepository = incomingOrderRepository;
            _processor = processor;
            _orderRepository = orderRepository;
        }
        public async Task RunAsync(CancellationToken ct)
        {
            await _dataSeeder.ResetAsync(ct);
            var allOrder = await _incomingOrderRepository.GetAllAsync(ct);
            var tasks = new List<Task<(Order order, double seconds)>>();
            foreach (var order in allOrder)
            {
                tasks.Add(RunOneAsync(order, ct));
            }
            int done = 0;
            await foreach (var task in Task.WhenEach(tasks))
            {
                done++;
                var (finishedOrder, seconds) = await task;
                var reason = string.IsNullOrEmpty(finishedOrder.FailureReason) ? "" : $" ({finishedOrder.FailureReason})";
                Log.Info($"[{done,2}/{allOrder.Count}] Order #{finishedOrder.Id} -> {finishedOrder.Status}{reason} after {seconds:F1} s");
            }
            var orderRepo = await _orderRepository.GetAllAsync(ct);
            Dictionary <OrderStatus,int> statusCounts = new Dictionary<OrderStatus, int>();
            foreach (var order in orderRepo)
            {
                if (statusCounts.ContainsKey(order.Status))
                {
                    statusCounts[order.Status]++;
                }
                else
                {
                    statusCounts[order.Status] = 1;
                }

            }
            foreach (var pair in statusCounts)
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }
        private async Task<(Order order, double seconds)> RunOneAsync(Order order, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            await _processor.ProcessOrderAsync(order, ct);
            return (order, sw.Elapsed.TotalSeconds);
        }
    }
}
