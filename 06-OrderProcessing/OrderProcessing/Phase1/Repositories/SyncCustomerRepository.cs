using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1.Repositories;

public sealed class SyncCustomerRepository
{
    private readonly JsonFileStore _store;

    public SyncCustomerRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Customer? GetById(int id)
    {
        Log.Info("Opening connection to customers.json...");
        Thread.Sleep(200);

        Log.Info($"Executing query for Customer #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var customers = _store.Read<Customer>("customers.json");

        foreach (var customer in customers)
        {
            if (customer.Id == id)
            {
                Log.Success($"Customer #{id} found.");
                return customer;
            }
        }

        Log.Info($"Customer #{id} not found.");
        return null;
    }
}