using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class RaceConditionDemo
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly Race_condition_on_the_last_keyboard _reservation;
        public RaceConditionDemo(JsonFileStore jsonFileStore, Race_condition_on_the_last_keyboard reservation)
        {
            _jsonFileStore = jsonFileStore;
            _reservation = reservation;
        }
        private async Task ResetStockAsync()
        {
            
            await _jsonFileStore.UpdateAsync<Product>("products.json", products =>

            {
                foreach (var product in products)
                {
                    if (product.Id == 4) // Assuming the keyboard has ID 4
                    {
                        product.StockQuantity = 1; // Reset stock to 1
                        break;
                    }
                }
            });
        }
        public async Task RunAsync()
        {
            // Step 1: Reset keyboard stock to 1
            await ResetStockAsync();
            var order1 = new Order { Id = 1, Items = new List<OrderItem> { new OrderItem { ProductId = 4, Quantity = 1 } } };
            var order2 = new Order { Id = 2, Items = new List<OrderItem> { new OrderItem { ProductId = 4, Quantity = 1 } } };
            var reservation1 = new Race_condition_on_the_last_keyboard(order1,new ProductRepository(_jsonFileStore),_jsonFileStore);

            var reservation2 = new Race_condition_on_the_last_keyboard(order2,new ProductRepository(_jsonFileStore),_jsonFileStore);
            var task1 = reservation1.ReserveStockUnsafeAsync(4, 1, CancellationToken.None);

            var task2 = reservation2.ReserveStockUnsafeAsync(4, 1, CancellationToken.None);

            bool result1 = await task1;
            bool result2 = await task2;
            Console.WriteLine($"Order 10 reservation: {result1}");
            Console.WriteLine($"Order 11 reservation: {result2}");
            var products = await _jsonFileStore.ReadAsync<Product>("products.json");

            foreach (var product in products)
            {
                if (product.Id == 4)
                {
                    Console.WriteLine($"Final keyboard stock: {product.StockQuantity}");
                    break;
                }
            }
            await ResetStockAsync();
            var productRepo = new ProductRepository(_jsonFileStore);

            var task3 =productRepo.ReserveStockAsync(4, 1, CancellationToken.None);
            var task4= productRepo.ReserveStockAsync(4, 1, CancellationToken.None);

            bool result3=await task3;
            bool result4= await task4;
            Console.WriteLine("----- Safe Reservation -----");
            Console.WriteLine($"Order 10 reservation: {result3}");
            Console.WriteLine($"Order 11 reservation: {result4}");
            var safeProducts = await _jsonFileStore.ReadAsync<Product>("products.json");

            foreach (var product in safeProducts)
            {
                if (product.Id == 4)
                {
                    Console.WriteLine($"Final keyboard stock: {product.StockQuantity}");
                    break;
                }
            }
        }
    }
}
