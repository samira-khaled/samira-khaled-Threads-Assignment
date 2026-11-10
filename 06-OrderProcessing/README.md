# Order Processing System — Threads & Async Assignment

A .NET 10 console application demonstrating synchronous and asynchronous programming, concurrent order processing, thread safety, cancellation, and parallel execution.

## 1. Project Overview

This project is divided into four phases:

- **Phase 1:** Async Data Access Layer.
- **Phase 2:** Concurrent Order Processing.
- **Phase 3:** Sync vs Async Order Processing, cancellation, payment handling, and email retries.
- **Phase 4:** Advanced task coordination, exception handling, shipping quotes, fraud scoring, and concurrent email sending.

The application uses JSON files for data storage and does not require a database or external services.

## 2. How to Run

Run the application from the project directory:

```bash
dotnet run -- phase1
dotnet run -- phase2
dotnet run -- phase3
dotnet run -- phase4
```

To run all phases:

```bash
dotnet run -- all
```

The application also supports running with a cancellation delay:

```bash
dotnet run -- phase3 --cancel-after 3000
```

If no mode is provided, the application displays a numbered menu.

### Data Location

The application creates its `Data` directory relative to:

```csharp
AppContext.BaseDirectory
```

For example, when running in Debug mode:

```text
OrderProcessing/bin/Debug/net10.0/Data/
```

The data seeder generates and resets the required data files when the application runs.

## 3. Project Structure

```text
06-OrderProcessing/
│
├── OrderProcessing/
│   ├── Program.cs
│   ├── OrderProcessing.csproj
│   │
│   ├── Phase1/
│   │   ├── OrderValidator.cs
│   │   ├── SyncOrderValidator.cs
│   │   ├── Phase1Runner.cs
│   │   └── Repositories/
│   │       ├── CouponRepository.cs
│   │       ├── CustomerRepository.cs
│   │       ├── IncomingOrderRepository.cs
│   │       ├── OrderRepository.cs
│   │       ├── ProductRepository.cs
│   │       ├── SyncCouponRepository.cs
│   │       ├── SyncCustomerRepository.cs
│   │       └── SyncProductRepository.cs
│   │
│   ├── Phase2/
│   │   ├── InvoiceGenerator.cs
│   │   ├── OrderPricer.cs
│   │   ├── OrderProcessorV2.cs
│   │   ├── Phase2Runner.cs
│   │   ├── PricingService.cs
│   │   ├── RaceConditionDemo.cs
│   │   ├── ReportGenerator.cs
│   │   └── Race condition on the last keyboard.cs
│   │
│   ├── Phase3/
│   │   └── Services/
│   │       ├── CustomerService.cs
│   │       ├── EmailService.cs
│   │       ├── FireAndForgetDemo.cs
│   │       ├── IEmailService.cs
│   │       ├── InventoryService.cs
│   │       ├── IPaymentGateway.cs
│   │       ├── OrderProcessor.cs
│   │       ├── PaymentGateway.cs
│   │       ├── Phase3Runner.cs
│   │       ├── PricingService.cs
│   │       ├── SyncCustomerService.cs
│   │       ├── SyncEmailService.cs
│   │       ├── SyncInventoryService.cs
│   │       ├── SyncOrderProcessor.cs
│   │       ├── SyncPaymentGateway.cs
│   │       ├── SyncPricingService.cs
│   │       └── SyncVsAsyncComparison.cs
│   │
│   ├── Phase4/
│   │   ├── CairoExpress.cs
│   │   ├── CustomerDashboard.cs
│   │   ├── CustomerDashboardService.cs
│   │   ├── EmailResendRunne.cs
│   │   ├── FastShip.cs
│   │   ├── FraudScoringRunner.cs
│   │   ├── IShippingCarrier.cs
│   │   ├── LiveCheckoutRunner.cs
│   │   ├── NileCourier.cs
│   │   ├── Phase4Runner.cs
│   │   ├── ShippingQuote.cs
│   │   └── ShippingQuoteService.cs
│   │
│   └── Shared/
│       ├── Exceptions/
│       │   └── Exceptions.cs
│       ├── Logging/
│       │   ├── Log.cs
│       │   └── SimulatedLatency.cs
│       ├── Models/
│       │   ├── Coupon.cs
│       │   ├── Customer.cs
│       │   ├── EmailMessage.cs
│       │   ├── enums.cs
│       │   ├── Order.cs
│       │   ├── OrderItem.cs
│       │   ├── PaymentResult.cs
│       │   ├── PaymentTransaction.cs
│       │   ├── Product.cs
│       │   └── Report.cs
│       └── Storage/
│           ├── DataFiles.cs
│           ├── DataSeeder.cs
│           ├── JsonFileStore.cs
│           ├── JsonSettings.cs
│           └── SeedData.cs
│
├── sample-output/
│   ├── Phase1/
│   │   └── orders.json
│   ├── phase2/
│   │   ├── orders.json
│   │   └── reports/
│   │       └── report_20261104_041402.json
│   ├── Phase3/
│   │   ├── emails.json
│   │   ├── email_retry.json
│   │   ├── orders.json
│   │   ├── payments.json
│   │   └── products.json
│   └── phase4/
│       ├── emails.json
│       ├── email_retry.json
│       ├── orders.json
│       ├── payments.json
│       └── products.json
│
├── ScreenShots/
│   └── Console screenshots
│
└── README.md
```

### Main Class Responsibilities

| Class / Group | Responsibility |
|---|---|
| `Program.cs` | Application entry point and phase selection menu. |
| `Phase1Runner` | Runs Phase 1 validation and sync/async comparison. |
| `OrderValidator` | Validates incoming orders using asynchronous repository operations. |
| `SyncOrderValidator` | Performs synchronous order validation. |
| `Repositories` | Reads and writes customers, products, coupons, incoming orders, and processed orders. |
| `Phase2Runner` | Runs concurrent order processing and collects task results. |
| `OrderProcessorV2` | Processes orders concurrently. |
| `OrderPricer` / `PricingService` | Calculates order prices and discounts. |
| `InvoiceGenerator` | Generates invoices using a CPU-bound operation and asynchronous file writing. |
| `RaceConditionDemo` | Demonstrates the stock reservation race condition. |
| `ReportGenerator` | Generates the order processing report. |
| `Phase3Runner` | Runs Phase 3 scenarios and comparisons. |
| `OrderProcessor` | Handles the asynchronous order workflow, including payment and cancellation. |
| `SyncOrderProcessor` | Handles the synchronous order workflow. |
| `PaymentGateway` | Simulates asynchronous payment operations. |
| `SyncPaymentGateway` | Simulates synchronous payment operations. |
| `InventoryService` | Handles asynchronous stock reservation and release. |
| `SyncInventoryService` | Handles synchronous inventory operations. |
| `EmailService` | Sends emails asynchronously and handles retry behavior. |
| `SyncEmailService` | Sends emails synchronously. |
| `FireAndForgetDemo` | Demonstrates the difference between fire-and-forget and awaiting an operation. |
| `SyncVsAsyncComparison` | Compares synchronous and asynchronous processing. |
| `Phase4Runner` | Runs the Phase 4 tasks. |
| `LiveCheckoutRunner` | Displays order results as tasks complete using `Task.WhenEach`. |
| `CustomerDashboardService` | Loads dashboard data concurrently and handles multiple exceptions. |
| `CustomerDashboard` | Holds the dashboard data. |
| `ShippingQuoteService` | Coordinates shipping carrier tasks and selects a successful quote. |
| `IShippingCarrier` | Defines the shipping carrier contract. |
| `CairoExpress`, `NileCourier`, `FastShip` | Simulate different shipping providers. |
| `ShippingQuote` | Represents a shipping quote. |
| `FraudScoringRunner` | Compares sequential and parallel fraud scoring and supports cancellation. |
| `EmailResendRunne` | Runs concurrent email re-sending operations. |
| `JsonFileStore` | Provides shared JSON file reading and writing with thread safety. |
| `DataSeeder` / `SeedData` | Creates and initializes application data. |
| `DataFiles` | Defines data file paths. |
| `JsonSettings` | Configures JSON serialization and deserialization. |
| `Log` | Provides application logging. |
| `SimulatedLatency` | Adds simulated delays to operations. |
| `Models` | Contains the application's data models and enums. |
| `Exceptions` | Contains custom application exceptions. |

## 4. Phase 1 — Async Data Access Layer

### Task 7.3 — Sync vs Async Results

| Metric | Sync | Async |
|---|---:|---:|
| Total Orders | 20 | 20 |
| Validated | 15 | 15 |
| Rejected | 5 | 5 |
| Execution Time | 34.9 seconds | 54.7 seconds |

The validation results were correct in both runs.

**Rejected Orders**

| Order | Reason |
|---|---|
| #4 | Customer is inactive |
| #9 | Insufficient stock for 'Wireless Mouse' |
| #12 | Coupon 'OLD20' has expired |
| #15 | Customer #99 not found |
| #18 | Coupon 'FAKE50' does not exist |

The async run took longer because it also saved all 20 orders to `orders.json`, while the sync run only validated the orders.

Each save operation includes simulated delays, which increased the total async execution time.

### Phase 1 Questions

**1. Why can async take longer than sync when orders are awaited one after another?**

Both versions process orders sequentially. Each order must finish before the next one starts.

Async does not make a single operation faster. It allows the thread to be released while waiting for an asynchronous operation to complete.

In my run, the async time was higher because the async workflow also saved every order, while the sync workflow did not save the results. Therefore, the two measurements do not include exactly the same work.

**2. Is the thread ID before an await always the same as after it? Why?**

No. The thread ID can change after an await because the continuation may run on a different available thread.

In my logs, the sync operations used Thread 7, while the async operations used different thread IDs, such as Thread 11, Thread 12, Thread 13, and Thread 14.

**3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?**

With `Task.Delay()`, the thread is released and can do other work while waiting.

With `Thread.Sleep()`, the current thread is blocked and cannot do other work until the sleep finishes.

**4. Why should a repository method return Task<T> instead of T?**

Repository operations often involve I/O, such as reading from or writing to files or databases.

Returning `Task<T>` allows the caller to use `await` without blocking the current thread while waiting for the operation to finish.

**5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?**

`File.ReadAllText` reads the file synchronously and blocks the current thread until the operation finishes.

`File.ReadAllTextAsync` reads the file asynchronously and returns a Task. The caller can await it without blocking its thread while the I/O operation is pending.

The async I/O operation generally does not need a thread to remain occupied during the disk wait.

### Phase 1 Output

- Validation screenshot: `ScreenShots/phase1-validation-summary.png`
- Generated orders file: `sample-output/Phase1/orders.json`

The `orders.json` file contains the 20 processed order records.

## 5. Phase 2 — Concurrent Order Processing

### Final Task Status

| Task Status | Count |
|---|---:|
| RanToCompletion | 14 |
| Faulted | 1 |
| Total Tasks | 15 |

`RanToCompletion` means that a task finished without throwing an exception. It does not necessarily mean that the order was completed successfully.

Some tasks returned `false` because their orders were rejected, while still finishing normally.

### Final Order Results

| Status | Count |
|---|---:|
| Completed | 13 |
| Rejected | 6 |
| Failed | 1 |
| Total Orders | 20 |

**Total Processing Time: 8.093 seconds**

Phase 2 processed orders concurrently, allowing multiple orders to be processed without waiting for each previous order to finish.

The Phase 2 processing time is not directly comparable to Phase 1 because the phases do not perform exactly the same operations.

**Important:**

One task became Faulted because Order #7 encountered the exception `Shipping provider unavailable`.

The exception was handled in `Phase2Runner`, and the order was marked as Failed. The remaining tasks continued processing.

### Task 8.5 — Task.Run for a CPU-bound Step

The `InvoiceGenerator` uses a loop of 20,000,000 iterations to simulate creating an invoice.

I used `Task.Run()` for the loop because it performs many calculations and uses the CPU.

After the loop finishes, the invoice is saved to:

```text
Data/invoices/invoice_{orderId}.txt
```

using `File.WriteAllTextAsync()`.

**Why did I use Task.Run() here but not for database calls?**

I used `Task.Run()` because the loop needs the CPU to perform calculations.

Database calls already have async methods, so I can use `await` directly. I don't need to use `Task.Run()` for them.

### Phase 2 Report

| Metric | Result |
|---|---:|
| Total Orders | 20 |
| Completed | 13 |
| Rejected | 6 |
| Failed | 1 |
| Payment Failed | 0 |
| Total Revenue | 5,966.45 |
| Average Order Value | 458.96 |
| Top Selling Product | USB-C Hub |
| Top Selling Quantity | 5 |

### Phase 2 Questions

**1. What is the difference between starting a task and awaiting it?**

Starting a task means beginning its execution. The program can continue doing other work while the task is running.

Awaiting a task means asynchronously waiting for it to finish before executing the next line of code, without blocking the current thread.

**2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?**

No, not every Task creates a new thread.

Some tasks use a thread from the ThreadPool, but `Task.Delay()` does not occupy a thread while waiting. It uses a timer, and the thread can do other work during this time.

**3. What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous?**

`.Result` and `.Wait()` block the current thread until the task finishes.

This is dangerous because the thread cannot do other work, and it may cause a deadlock or thread-pool starvation. We should use `await` instead.

**4. What happens to an exception thrown inside a task that is never awaited?**

The exception is stored inside the task.

If we never await or check the task, we may not notice that an exception happened, and the code after starting the task can continue executing.

Using `await` allows us to catch the exception with `try-catch`.

**5. When should you use Task.Run(), and when should you avoid it?**

We use `Task.Run()` for CPU-bound operations, such as heavy calculations.

We should avoid using it for I/O-bound operations that already have async methods, such as reading files or calling APIs.

**6. What is the difference between Task and Task<T>?**

`Task` represents an asynchronous operation that does not return a result.

`Task<T>` represents an asynchronous operation that returns a value of type `T` when it finishes.

**7. Why can a lock block not contain an await?**

Because `lock` is designed for synchronous code, and `await` can suspend the method while the lock is still held.

For asynchronous synchronization, we can use `SemaphoreSlim` with `WaitAsync()` instead.

**8. Why did the unprotected stock reservation oversell, and how did you prevent it?**

Two threads could read the same stock quantity at the same time before it was updated.

Both threads thought there was enough stock, so they reserved more than the available quantity.

We prevented this by using `lock` to allow only one thread at a time to execute the complete stock reservation operation (checking and updating the stock).

## 6. Phase 3 — Sync vs Async

### Questions & Answers

**1. Does async make one order's workflow faster?**

No, async does not necessarily make one order faster. It allows the thread to do other work while waiting for I/O operations instead of staying blocked.

**2. Why does async scale better for many orders?**

Async scales better because threads are not blocked while waiting for I/O operations. This allows the application to handle more orders using fewer threads.

**3. What happens to the sync version with 1,000 orders?**

The sync version may create too many threads, which increases memory usage, context switching, and CPU overhead. This can reduce performance and exhaust system resources.

**4. Why is async essential for apps that call databases, files, and APIs?**

These operations take time to complete.

Async allows the application to do other work while waiting for their results instead of blocking threads, improving responsiveness and scalability.

### Task 9.7 — Fire-and-Forget & Await

**1. When is await necessary?**

We should use `await` when the next operation depends on the result of the current asynchronous operation, or when we need to make sure it completes before continuing.

**2. When is fire-and-forget acceptable, if ever?**

Fire-and-forget can be acceptable for non-critical operations, such as logging or telemetry, if failures are properly handled and monitored.

It should not be used for critical operations like sending order confirmation emails because the operation may not finish if the application exits.

**3. Why is async void dangerous?**

`async void` is dangerous because it cannot be awaited, and its exceptions cannot be caught by the calling method using a normal try-catch block.

This makes error handling difficult.

**4. Why is async void acceptable for event handlers?**

`async void` is acceptable for event handlers because event handler signatures usually require a `void` return type.

However, it should be avoided in other methods, where `async Task` is preferred.

### Console Output — Before Fix (Fire-and-Forget)

The email operation was not awaited, so the method finished before the email was sent.

```text
[01:46:19.276] [Thread 7] Order #1 saved.
Order processing finished
```

The email confirmation message did not appear before the application exited.

### Console Output — After Fix (Using await)

After adding `await` to `SendConfirmationAsync`, the method waited until the email operation completed.

```text
[01:49:51.446] [Thread 5] Order #1 saved.
[01:49:52.482] [Thread 8] Email for Order #1 sent successfully
Order processing finished
```

**Result:**

The email was sent successfully before the processing method finished. This confirms that using `await` ensures the email operation completes before the next line executes.

## 7. Phase 4 — Understanding and Implementation Answers

### Task 10.1 — Live Checkout Progress

I used `Task.WhenEach` to process the tasks in the order they complete. This allows me to print each order's result immediately when it finishes, without waiting for the previous order.

For example, if Order #3 finishes before Order #1, I can print Order #3 first.

Using `foreach` with `await` on the original task list would make the program wait for each task in the original order, even if another task has already finished.

**Results**

| Order Status | Count |
|---|---:|
| Completed | 11 |
| Rejected | 6 |
| PaymentFailed | 2 |
| Failed | 1 |
| Total | 20 |

### Task 10.2 — Handling Multiple Exceptions

I used `Task.WhenAll` to run the four data-loading operations concurrently. I wrapped the await in a `try-catch` block.

If an exception occurs, I loop through the tasks and check each task's `Exception` property to collect all the errors.

A normal `await` on `Task.WhenAll` throws an exception, but checking each task allows me to see all the failures.

**Results**

- Dashboard loaded in 785 ms.
- Orders: 2
- Payments: 2
- Emails: 1
- The Customer #99 test showed two failures:
  - Payments store timeout.
  - Customer #99 not found.

### Task 10.3 — Cancelling Losing Carriers

I used `Task.WhenAny` to identify the next completed carrier task. After getting a successful quote, I cancel the remaining carrier operations using cancellation tokens.

If we don't cancel the other carriers after getting the first successful quote, they will continue running even though we no longer need their results.

This wastes time and resources. Cancelling them helps stop unnecessary work.

**Results**

| Order | Winner | Price | Time |
|---|---|---:|---:|
| #1 | CairoExpress | 11.97 | 1093 ms |
| #2 | NileCourier | 8.03 | 1182 ms |
| #3 | Flat rate | 25.00 | 1210 ms |
| #4 | FastShip | 7.10 | 970 ms |
| #5 | FastShip | 16.50 | 1120 ms |
| #6 | All carriers failed | — | — |

### Task 10.4 — CPU-bound vs I/O-bound

#### Part A — Fraud Scoring

Fraud scoring is CPU-bound because it performs many calculations. Using multiple CPU cores allows more calculations to run in parallel, which reduces execution time.

**Measured Results**

| Metric | Sequential | Parallel |
|---|---:|---:|
| Execution Time | 16,955 ms | 2,378 ms |
| Flagged Orders | 1,005 | 1,005 |

Additional results:

- Processor count: 8
- Speedup: 7.13x
- Same results: True
- First 5 flagged IDs: 4, 8, 12, 29, 33
- Cancellation test: Parallel scoring cancelled successfully.

#### Part B — Email Sending

Email sending is I/O-bound because it spends most of its time waiting for an external email service. It does not benefit from more CPU cores in the same way because the main delay is network waiting, not CPU processing.

**Measured Results**

| Metric | Result |
|---|---:|
| Emails Sent | 30 |
| Maximum Concurrent Sends | 4 |
| Total Time | 8.2 seconds |

### Phase 4 Questions

**1. For each of Tasks 10.1–10.4, which tool did you use, and why can't you start then await one by one?**

- Task 10.1: `Task.WhenEach` lets me process tasks as they complete.
- Task 10.2: `Task.WhenAll` starts the four data-loading operations concurrently and waits for all of them.
- Task 10.3: `Task.WhenAny` lets me identify completed carrier tasks so I can use the first successful quote and cancel the remaining operations.
- Task 10.4 Part A: `Parallel.ForEach` is used for parallel CPU-bound fraud scoring.
- Task 10.4 Part B: `Parallel.ForEachAsync` is used to process email operations concurrently with a maximum degree of parallelism of 4.

Starting and awaiting each operation one by one would make the operations run sequentially. This would prevent us from getting the benefits of concurrency or parallel processing.

**2. Why should you collect all exceptions when multiple operations fail?**

Because several independent operations may fail during the same execution.

If I only handle the first exception, I may miss other failures. Checking the exception of each task helps me collect and report all the errors.

**3. What happens if you don't cancel the losing carriers?**

The other carrier operations continue running even after we already have a successful quote.

This wastes time and resources. Cancellation helps stop operations that are no longer needed.

**4. What is the difference between CPU-bound and I/O-bound work?**

CPU-bound work spends most of its time performing calculations and using the processor.

I/O-bound work spends most of its time waiting for external operations, such as network requests, files, or databases.

**5. What is the difference between parallel work and concurrent asynchronous work?**

Parallel work means executing CPU-bound operations at the same time using multiple CPU cores.

Concurrent async work means starting multiple operations without waiting for each one to finish before starting the next. It is useful for I/O-bound operations because the program can continue working while other operations are waiting.

**6. What happens when cancellation occurs during checkout?**

When cancellation occurs during checkout, the affected orders are cancelled instead of being completed.

Any reserved stock is released. If a payment was already captured, it is refunded to avoid charging the customer for a cancelled order.

## 8. Storage Design and Thread Safety

The application uses JSON files for storing its data.

### Data Files

The `Data` directory contains the following files and output directories:

| File / Directory | Purpose |
|---|---|
| `customers.json` | Customer records |
| `products.json` | Product records and stock quantities |
| `coupons.json` | Coupon information |
| `incoming_orders.json` | Incoming orders to be processed |
| `orders.json` | Processed order records |
| `payments.json` | Payment records |
| `emails.json` | Successfully sent email records |
| `email_retry.json` | Emails that need to be retried |
| `invoices/` | Generated invoice files |
| `reports/` | Generated reports |

### JSON Format

The application uses `System.Text.Json` to serialize and deserialize data.

The shared JSON configuration uses:

- Indented JSON output.
- Camel-case property names.
- `JsonStringEnumConverter` for enum values.
- UTF-8 encoding.

Empty collections are represented as empty JSON arrays (`[]`).

### Concurrent File Access

The `JsonFileStore` is responsible for reading and writing JSON data.

To prevent race conditions when multiple operations access the same file:

- A `ConcurrentDictionary` stores a `SemaphoreSlim` for each file path.
- Operations accessing the same file use the same synchronization gate.
- Both synchronous and asynchronous operations use the same per-file lock.
- Read-modify-write operations keep the lock for the complete operation.
- Writes use a temporary file followed by an atomic file replacement.

This prevents concurrent updates from overwriting each other and helps maintain consistent JSON data.

The asynchronous methods support `CancellationToken` so that cancellation can be passed through file operations.

If a file does not exist, the store returns an empty collection. Invalid JSON results in a `DataFileCorruptedException` that identifies the affected file.

## 9. Performance Measurements

The following table summarizes the measurements currently available from the runs.

| Phase / Task | Measurement | Result |
|---|---|---:|
| Phase 1 | Sync validation time | 34.9 seconds |
| Phase 1 | Async validation and save time | 54.7 seconds |
| Phase 2 | Concurrent processing time | 8.093 seconds |
| Phase 3 | Sync processing time | 14,553 ms |
| Phase 3 | Async processing time | 14,585 ms |
| Phase 3 | Sync threads created | 10 |
| Phase 3 | Async ThreadPool count before | 4 |
| Phase 3 | Async ThreadPool count after | 6 |
| Phase 4 — Task 10.2 | Dashboard loading time | 785 ms |
| Phase 4 — Task 10.4 A | Fraud scoring sequential time | 16,955 ms |
| Phase 4 — Task 10.4 A | Fraud scoring parallel time | 2,378 ms |
| Phase 4 — Task 10.4 A | Speedup | 7.13x |
| Phase 4 — Task 10.4 B | Email sending time | 8.2 seconds |
| Phase 4 — Task 10.4 B | Maximum concurrent email sends | 4 |

**Additional measurements required by the assignment:**

The following values still need to be collected from the relevant execution logs before the performance table is complete:

- Phase 2 pricing: sequential time vs concurrent time.
- Phase 2 concurrent processing: distinct thread count and `ThreadPool.ThreadCount`.
- Phase 3: async distinct thread count and sync `ThreadPool.ThreadCount`, if required by the measurement method.
- Phase 4 dashboard, shipping quotes, fraud scoring, and email sending: distinct thread counts and `ThreadPool.ThreadCount`.

These values are not included because they have not been confirmed from the available results.

## 10. Problems Found and Solutions

| Problem | Solution |
|---|---|
| Sequential processing made orders wait for one another. | Used asynchronous and concurrent task execution where appropriate. |
| `Thread.Sleep()` blocked the current thread. | Used `Task.Delay()` for asynchronous waiting. |
| `.Result` and `.Wait()` block threads. | Used `await` instead. |
| Multiple operations could update the same stock quantity. | Protected the complete stock reservation operation using synchronization. |
| Concurrent file updates could overwrite each other. | Used a per-file `SemaphoreSlim` and atomic file writes. |
| A task could fail while other orders were still processing. | Handled task exceptions so the remaining processing could continue. |
| Payment could succeed before a later operation failed. | Refunded captured payments when compensation was required. |
| Reserved stock could remain unavailable after a failed order. | Released reserved stock when processing failed or was cancelled. |
| Email failures could affect the order workflow if not handled separately. | Added retry handling and a retry file for unsuccessful emails. |
| Fire-and-forget email operations could finish after the main method. | Used `await` when email completion was required. |
| Losing carrier operations could continue unnecessarily. | Used cancellation tokens to cancel operations after selecting a successful quote. |

## 11. Console Screenshots

The `ScreenShots/` folder contains the following screenshots:

| Screenshot | Evidence |
|---|---|
| `phase1-validation-summary.png` | Phase 1 validation summary |
| `Concurrent Execution.png` | Concurrent execution demonstration |
| `Fault Handling.png` | Faulted task and exception handling |
| `Phase2-Report-Expected-Results.png` | Phase 2 report results |
| `Payment Timeout.png` | Payment timeout handling |
| `Payment Declined.png` | Payment declined handling |
| `Email Retry.png` | Email retry attempts |
| `Sync vs Async Comparison.png` | Phase 3 synchronous vs asynchronous comparison |
| `Cancellation.png` | Cancellation demonstration |
| `Live Checkout Progress.png` | Task 10.1 — Live checkout progress |
| `Dashboard Success.png` | Task 10.2 — Successful dashboard loading |
| `Dashboard Failure.png` | Task 10.2 — Multiple exception handling |
| `Shipping Providers.png` | Task 10.3 — Shipping quotes and carrier selection |
| `Sequential vs Parallel.png` | Task 10.4 — Sequential vs parallel fraud scoring |
| `Parallel Email Sending.png` | Task 10.4 — Concurrent email sending |

## 12. Sample Output

The `sample-output/` directory contains the saved JSON outputs for the phases:

```text
sample-output/
├── Phase1/
│   └── orders.json
├── phase2/
│   ├── orders.json
│   └── reports/
│       └── report_20261104_041402.json
├── Phase3/
│   ├── emails.json
│   ├── email_retry.json
│   ├── orders.json
│   ├── payments.json
│   └── products.json
└── phase4/
    ├── emails.json
    ├── email_retry.json
    ├── orders.json
    ├── payments.json
    └── products.json
```

These files are examples of the generated outputs from the different phases.

## 13. Final Checklist

- [x] Project targets .NET 10.
- [x] Four phases are implemented.
- [x] Phase 1 validation output is included.
- [x] Phase 2 order and report output is included.
- [x] Phase 3 JSON output files are included.
- [x] Phase 4 JSON output files are included.
- [x] Console screenshots are included for the listed scenarios.
- [x] README includes project structure and class responsibilities.
- [x] README explains JSON storage and concurrent file access.
- [x] README includes answers to the Phase 1–4 questions.
- [ ] Complete the remaining performance measurements.
- [ ] Verify that the Phase 2 sample report is the final report from the latest run.
