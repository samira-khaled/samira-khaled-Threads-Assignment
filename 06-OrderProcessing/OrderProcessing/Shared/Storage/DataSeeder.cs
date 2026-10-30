using OrderProcessing.Shared.Models;

namespace OrderProcessing.Shared.Storage;

public sealed class DataSeeder
{
    private readonly JsonFileStore _jsonFileStore;

    public DataSeeder(JsonFileStore jsonFileStore)
    {
        _jsonFileStore = jsonFileStore;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        

        Directory.CreateDirectory(DataFiles.DataFolder);
        Directory.CreateDirectory(DataFiles.InvoicesFolder);
        Directory.CreateDirectory(DataFiles.ReportsFolder);

        await SeedFileAsync(DataFiles.Customers, SeedData.Customers, ct);
        await SeedFileAsync(DataFiles.Products, SeedData.Products, ct);
        await SeedFileAsync(DataFiles.Coupons, SeedData.Coupons, ct);
        await SeedFileAsync(DataFiles.IncomingOrders, SeedData.IncomingOrders, ct);

        await SeedFileAsync<Order>(DataFiles.Orders, [], ct);
        await SeedFileAsync<PaymentTransaction>(DataFiles.Payments, [], ct);
        await SeedFileAsync<EmailMessage>(DataFiles.Emails, [], ct);
        await SeedFileAsync<EmailMessage>(DataFiles.EmailRetry, [], ct);
    }

    private async Task SeedFileAsync<T>(string fileName,List<T> records,CancellationToken ct)
    {
        await _jsonFileStore.WriteAsync(fileName, records, ct);

        Console.WriteLine($"[Seeder] {fileName} -> {records.Count} records.");
    }
}