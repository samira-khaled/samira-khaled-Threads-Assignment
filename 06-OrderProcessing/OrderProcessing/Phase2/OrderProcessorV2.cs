using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderProcessorV2
    {
    private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderPricer _orderPricer;
        private readonly InvoiceGenerator _invoiceGenerator;
        public OrderProcessorV2(ProductRepository productRepository, OrderRepository orderRepository, OrderPricer orderPricer, InvoiceGenerator invoiceGenerator)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderPricer = orderPricer;
            _invoiceGenerator = invoiceGenerator;
        }
       
        public async Task<bool> ProcessValidatedOrderAsync(Order order,CancellationToken cancellationToken)
        {
            Dictionary<int, int> reservedItems = new Dictionary<int, int>();
            foreach (var item in order.Items)
            {
                var product = await _productRepository.ReserveStockAsync( item.ProductId,  item.Quantity, cancellationToken);
             
                if(product )
                {
                    reservedItems.Add(item.ProductId, item.Quantity);
                    
                }
              
                else
                {
                    var failedProduct = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                    // Rollback previously reserved items
                    foreach (var reservedItem in reservedItems)
                    {
                        await _productRepository.ReleaseStockAsync(reservedItem.Key, reservedItem.Value, cancellationToken);
                    }
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{failedProduct?.Name ?? "Unknown product"}'";
                    await _orderRepository.SaveAsync(order, cancellationToken);
                    return false;
                }
           
            }
            await _orderPricer.PriceOrderAsync(order, cancellationToken);
            await _invoiceGenerator.GenerateAsync(order);

            order.Status = OrderStatus.Completed;
            await _orderRepository.SaveAsync(order, cancellationToken);
            return true;
        }
    }
}
