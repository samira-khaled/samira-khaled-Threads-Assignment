using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class InventoryService
    {
        private readonly ProductRepository _productRepository;
       
        public InventoryService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<bool> ReserveStockAsync(Order order,CancellationToken cancellationToken=default)
        {
            Dictionary<int, int> productQuantities = new Dictionary<int, int>();
            Dictionary<int, int> reservedProducts = new Dictionary<int, int>();

            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
              
                if (product == null)
                {
                    return false;
                }
                if (!productQuantities.ContainsKey(item.ProductId))
                {
                    productQuantities[item.ProductId] = 0;
                }
                productQuantities[item.ProductId] += item.Quantity;
                if (productQuantities[item.ProductId] > product.StockQuantity) 
                {
                    return false; 
                }
            }
            foreach (var item in productQuantities)
            {
              
               bool reserved = await _productRepository.ReserveStockAsync(item.Key, item.Value, cancellationToken);
                if (!reserved)
                {
                    foreach (var reverse in reservedProducts)
                    {
                        bool released = await _productRepository.ReleaseStockAsync(reverse.Key,reverse.Value,cancellationToken);

                        if (!released)
                        {
                            throw new InvalidOperationException(
                                $"Rollback failed for Product ID {reverse.Key}.");
                        }
                    }

                    return false;
                }
                reservedProducts[item.Key] = item.Value;

            }
           
            return true; 
        }
        public async Task ReleaseStockAsync(Order order, CancellationToken cancellationToken=default)
        {
            foreach(var item in order.Items)
            {
                bool released = await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
                if (!released)
                {
                    throw new InvalidOperationException("Stock release failed.");
                }
            }

        }
    }
}
