
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
// استخدمي هنا الـ namespace الموجود فيه Customer عندك

namespace OrderProcessing.Phase1.Repositories;

public sealed class CustomerRepository
{
    private readonly JsonFileStore _store;

    public CustomerRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Customer?> GetByIdAsync(int id,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to customers.json...",200,200,ct);

        await SimulatedLatency.DelayAsync($"Executing query for Customer #{id}...",300,600,ct);

        var customers = await _store.ReadAsync<Customer>("customers.json",ct);

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
