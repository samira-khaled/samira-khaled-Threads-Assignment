using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class ReportGenerator
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        public ReportGenerator(OrderRepository orderRepository, ProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }
        public async Task<Report> GenerateAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            Dictionary<int, int> soldQuantities = new Dictionary<int, int>(); // int 1 =>productId, int 2 => sold quantity
            int totalOrders = orders.Count;
            int completedCount = 0;
            int rejectedCount = 0;
            int failedCount = 0;
            int paymentFailedCount = 0;
            decimal totalRevenue = 0;
            int topSellingProductId = 0;
            int maxSoldQuantity = 0;
            decimal averageOrderValue;
            DateTime generatedAt = DateTime.Now;


            foreach (var order in orders)
            {
                if(order.Status == OrderStatus.Completed)
                {
                    completedCount++;
                    totalRevenue += order.TotalAmount;
                    foreach(var item in order.Items)
                    {
                        
                        if (soldQuantities.ContainsKey(item.ProductId))
                        {
                            soldQuantities[item.ProductId] += item.Quantity;
                        }
                        else
                        {
                            soldQuantities[item.ProductId] = item.Quantity;
                        }       

                    }

                }
                if(order.Status == OrderStatus.Rejected)
                {
                    rejectedCount++;
                }
                if(order.Status == OrderStatus.Failed)
                {
                    failedCount++;
                }
                if(order.Status == OrderStatus.PaymentFailed)
                {
                    paymentFailedCount++;
                }

            }
            averageOrderValue = completedCount > 0 ? totalRevenue / completedCount : 0;
            foreach (var kvp in soldQuantities)
            {
                if (kvp.Value > maxSoldQuantity ||(kvp.Value == maxSoldQuantity &&(topSellingProductId == 0 || kvp.Key < topSellingProductId)))
                {
                    maxSoldQuantity = kvp.Value;
                    topSellingProductId = kvp.Key;
                }
            }
            string topSellingProduct = "";

            if (topSellingProductId != 0)
            {
                var product = await _productRepository.GetByIdAsync(topSellingProductId);
                topSellingProduct = product?.Name ?? "";
            }
            Report report = new Report
            {
                TotalOrders = totalOrders,
                CompletedCount = completedCount,
                RejectedCount = rejectedCount,
                FailedCount = failedCount,
                PaymentFailedCount = paymentFailedCount,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                TopSellingProduct = topSellingProduct,
                TopSellingQuantity = maxSoldQuantity,
                GeneratedAt = generatedAt
            };
            Directory.CreateDirectory(DataFiles.ReportsFolder);
            string reportFilePath = Path.Combine(DataFiles.ReportsFolder, $"report_{generatedAt:yyyyMMdd_HHmmss}.json");
            string jsonReport=System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(reportFilePath, jsonReport);
            return report;





        }
    }
}
