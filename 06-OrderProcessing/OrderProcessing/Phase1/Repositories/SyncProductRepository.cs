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
    public bool ReserveStock(int productId, int qty)
    {
        bool reserved = false;

        Thread.Sleep(200);
        Thread.Sleep(100);
        Thread.Sleep(400);

        _store.Update<Product>("products.json", products =>
       {
           foreach (var product in products)
           {
               if (product.Id == productId && qty > 0 && product.StockQuantity >= qty)
               {
                   product.StockQuantity -= qty;
                   reserved = true;
                   break;
               }
           }
       });
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
    public bool ReleaseStock(int productId, int qty)
    {
        bool released = false;

        Thread.Sleep(200);
        Thread.Sleep(100);
        Thread.Sleep(400);

         _store.Update<Product>("products.json", products =>
        {
            foreach (var product in products)
            {
                if (product.Id == productId && qty > 0)
                {
                    product.StockQuantity += qty;
                    released = true;
                    break;
                }
            }
        });

        Thread.Sleep(200);

        if (released)
        {
            Log.Success($"Stock released for Product #{productId}.");
        }
        else
        {
            Log.Error($"Stock release failed for Product #{productId}.");
        }

        return released;
    }
}