using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1.Repositories;

public sealed class SyncProductRepository
{
    private readonly JsonFileStore _store;

    public SyncProductRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Product? GetById(int id)
    {
        Log.Info("Opening connection to products.json...");
        Thread.Sleep(200);

        Log.Info($"Executing query for Product #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var products = _store.Read<Product>("products.json");

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
}