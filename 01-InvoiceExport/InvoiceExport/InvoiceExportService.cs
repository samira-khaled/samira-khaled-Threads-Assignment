
using System.Diagnostics;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }

    public async Task ExportAllPending()
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders = await _db.GetPendingOrders();


        var tasks = new List<Task<string>>();
        foreach (Order order in orders)
            tasks.Add(ExportOneAsync(order.Id));


        foreach (Task<string> task in tasks)
        {
            string path = await task;
            Console.WriteLine($"Wrote {path}");
        }

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }
    async Task<string> ExportOneAsync(int orderId)
    {
        Order fresh = await _db.GetOrderById(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} missing");

        CustomerProfile customer = await _customers.ReadProfile(fresh.CustomerId);
        return await _invoices.WriteInvoice(fresh, customer);
    }
}
