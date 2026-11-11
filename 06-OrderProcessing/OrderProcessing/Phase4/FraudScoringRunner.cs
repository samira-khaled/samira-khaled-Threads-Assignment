using OrderProcessing.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public class FraudScoringRunner
    {
        private static int Score(int orderId)
        {
            long h = orderId;

            for (int i = 0; i < 100_000; i++)
            {
                h = (h * 31 + i) % 1_000_000_007;
            }

            return (int)(h % 100);
        }
        public List<int> RunSequential(List<Order> orders)
        {
            var flaggedOrders = new List<int>();

            foreach(var order in orders)
            {
                int score = Score(order.Id);
                if(score>=90)
                {
                    flaggedOrders.Add(order.Id);
                }
            }

            return flaggedOrders;
        }
        public List<int> RunParallel(List<Order> orders, CancellationToken ct)
        {
            var flaggedOrders = new ConcurrentBag<int>();
            var threadIds = new ConcurrentBag<int>();

            var options = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(orders, options, order =>
            {
                threadIds.Add(Environment.CurrentManagedThreadId);
                int score = Score(order.Id);

                if (score >= 90)
                {
                    flaggedOrders.Add(order.Id);
                }
                
            });
            var distinctThreads = new HashSet<int>(threadIds);

            Console.WriteLine($"Distinct threads used: {distinctThreads.Count}");
            Console.WriteLine($"Processor count: {Environment.ProcessorCount}");
            return new List<int>(flaggedOrders);
        }
    }
}
