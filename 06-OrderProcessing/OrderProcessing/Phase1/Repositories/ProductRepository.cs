
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
// Add the namespace containing Product

namespace OrderProcessing.Phase1.Repositories;

public sealed class ProductRepository
{
    private readonly JsonFileStore _store;

    public ProductRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Product?> GetByIdAsync(int id,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to products.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync($"Executing query for Product #{id}...",300, 600, ct);

        var products = await _store.ReadAsync<Product>("products.json", ct);

        foreach (var product in products)
        {
            if (product.Id == id)
            {
                Log.Success($"Product #{id} found.");
                return product;
            }
        }

        Log.Info($"Product #{id} not found.");
        return null;
    }

    public async Task<bool> ReserveStockAsync(int productId,int qty,CancellationToken ct = default)
    {
        bool reserved = false;

        await SimulatedLatency.DelayAsync("Opening connection to products.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync("Beginning stock reservation transaction...",100, 100, ct);

        await SimulatedLatency.DelayAsync($"Updating stock for Product #{productId}...",400, 400, ct);

        await _store.UpdateAsync<Product>("products.json",products =>
            {
                foreach (var product in products)
                {
                    if (product.Id == productId &&qty > 0 &&product.StockQuantity >= qty)
                    {
                        product.StockQuantity -= qty;
                        reserved = true;
                        break;
                    }
                }
            },ct);

        await SimulatedLatency.DelayAsync("Committing stock reservation...",200, 200, ct);

        if (reserved)
        {
            Log.Success($"Stock reserved for Product #{productId}.");
        }
        else
        {
            Log.Error($"Stock reservation failed for Product #{productId}.");
        }

        return reserved;
    }
}
