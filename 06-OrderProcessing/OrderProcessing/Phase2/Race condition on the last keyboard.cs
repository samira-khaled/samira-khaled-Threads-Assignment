using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class Race_condition_on_the_last_keyboard
    {
        private readonly Order _order;
        private readonly ProductRepository _productRepository;
        private readonly JsonFileStore _jsonFileStore;

        public Race_condition_on_the_last_keyboard(Order order, ProductRepository productRepository, JsonFileStore jsonFileStore)
        {
            _order = order;
            _productRepository = productRepository;
            _jsonFileStore = jsonFileStore;
        }
        public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken cancellationToken)
        {

            // 1. Read product
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

            // 2. Check product
            if (product is null)
                return false;

            // 3. Check available stock
            if (qty <= 0 || product.StockQuantity < qty)
                return false;

            // 4. Opening connection - 200ms
            await SimulatedLatency.DelayAsync("Opening connection...", 200, 200, cancellationToken);

            // 5. Beginning transaction - 100ms
            await SimulatedLatency.DelayAsync("Beginning transaction...", 100, 100, cancellationToken);

            // 6. Updating stock - 400ms
            await SimulatedLatency.DelayAsync("Updating stock...", 400, 400, cancellationToken);
            product.StockQuantity -= qty;
            await _jsonFileStore.UpdateAsync<Product>("products.json",products =>
            {
                foreach (var existingProduct in products)
                {
                    if (existingProduct.Id == productId)
                {
                existingProduct.StockQuantity = product.StockQuantity;
                break;
            }
        }
        },cancellationToken);
            return true;
        }
    }
}
