# Assignment 01 — Answers

Student name: Samira Khaled Anwar Shaltout
Group / ID: < Group #3 .NET |Sun & Wed @ 7 PM >

## Problems with the current design

Before my change, the export handled the orders one by one. The program
finished one order completely (load order, read customer, write invoice)
before it even started the next one.

Here is the console output of the original program:

```
Starting invoice export...
Customer files: ...\bin\Debug\net10.0\Data
Invoice output: ...\bin\Debug\net10.0\invoices

Wrote ...\invoices\invoice-1001.txt
Wrote ...\invoices\invoice-1002.txt
Wrote ...\invoices\invoice-1003.txt
Wrote ...\invoices\invoice-1004.txt
Wrote ...\invoices\invoice-1005.txt
Export finished in 6.0 s
```

The problems are:

- **It is slow.** 5 invoices took 6.0 seconds, so about 1.2 seconds for each
  invoice. The total time is the sum of the time of all orders.
- **Most of the time is spent waiting.** The program waits for the database
  and for the files. It does not do any useful work during that time.
- **It does not scale.** If we have 500 orders, it will take about 10 minutes
  (500 x 1.2 s). If we have 5,000 orders, it will take more than an hour.
  For a nightly export with many orders, this is too slow.
- **Each order waits for the previous one.** Even if order 1002 is ready to
  start, it must wait until order 1001 is completely finished.

## What you changed

The old code had `await` inside the `foreach` loop. This means the program
waited for each order to finish before starting the next one. So the code
looked async, but it still worked on one order at a time.

I changed it in two steps:

1. **Start all the orders together.** I moved the steps of one order
   (load order, then read customer, then write invoice) into a new method
   called `ExportOneAsync`. Then I called this method for every order and
   saved the returned tasks in a list. I did not use `await` inside this
   loop, so all the orders start working at the same time.

2. **Await the tasks one by one.** In a second loop, I used `await` on each
   task and printed the path of the invoice that was written.

```csharp
var tasks = new List<Task<string>>();
foreach (Order order in orders)
    tasks.Add(ExportOneAsync(order.Id));      // start all orders

foreach (Task<string> task in tasks)
{
    string path = await task;                 // await them one by one
    Console.WriteLine($"Wrote {path}");
}

async Task<string> ExportOneAsync(int orderId)
{
    Order fresh = await _db.GetOrderById(orderId)
        ?? throw new InvalidOperationException($"Order {orderId} missing");

    CustomerProfile customer = await _customers.ReadProfile(fresh.CustomerId);
    return await _invoices.WriteInvoice(fresh, customer);
}
```
Console output after my change:

```
Starting invoice export...
Customer files: ...\bin\Debug\net10.0\Data
Invoice output: ...\bin\Debug\net10.0\invoices

Wrote ...\invoices\invoice-1001.txt
Wrote ...\invoices\invoice-1002.txt
Wrote ...\invoices\invoice-1003.txt
Wrote ...\invoices\invoice-1004.txt
Wrote ...\invoices\invoice-1005.txt
Export finished in 1.9 s
```

Result: the total time went from **6.0 s to 1.9 s**. The waiting of all the
orders now happens at the same time instead of one after another, so the
total time is close to the time of the slowest single order and not the
sum of all orders.