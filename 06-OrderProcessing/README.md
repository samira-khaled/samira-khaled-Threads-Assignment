## Phase 1 — Async Data Access Layer

### Task 7.3 — Sync vs Async Results

| Metric | Sync | Async |
|---|---:|---:|
| Total Orders | 20 | 20 |
| Validated | 15 | 15 |
| Rejected | 5 | 5 |
| Execution Time | 35.8 seconds | 54.5 seconds |

The validation results were correct in both runs.

Rejected orders:

- Order #4: Customer is inactive
- Order #9: Insufficient stock for 'Wireless Mouse'
- Order #12: Coupon 'OLD20' has expired
- Order #15: Customer #99 not found
- Order #18: Coupon 'FAKE50' does not exist

The async run took longer because it also saves all 20 orders to `orders.json`, while the sync run only validates orders and does not save them. Each save operation has simulated delays, which add extra time to the async run.

### Phase 1 Questions

**1. Why can async take longer than sync when orders are awaited one after another?**

Because both versions process orders sequentially. Each order must finish before the next one starts. Async does not make a single operation faster; it allows the thread to be released while waiting for an asynchronous operation to complete.

In my run, the async time was higher because the async workflow also saved every order, while the sync workflow did not save results. Therefore, the two measurements do not include exactly the same work.

**2. Is the thread ID before an await always the same as after it? Why?**

No. The thread ID can change after an await because the continuation may run on a different available thread.

In my logs, the sync operations used Thread 7, while the async operations used different thread IDs, such as Thread 11, Thread 12, Thread 13, and Thread 14.

**3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?**

With `Task.Delay()`, the thread is released and can do other work while waiting.

With `Thread.Sleep()`, the current thread is blocked and cannot do other work until the sleep finishes.

**4. Why should a repository method return Task<T> instead of T?**

Because repository operations often involve I/O, such as reading from or writing to files or databases. Returning `Task<T>` allows the caller to use `await` without blocking the current thread while waiting for the operation to finish.

**5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?**

`File.ReadAllText` reads the file synchronously and blocks the current thread until the operation finishes.

`File.ReadAllTextAsync` reads the file asynchronously and returns a Task. The caller can await it without blocking its thread while the I/O operation is pending. The async I/O operation generally does not need a thread to remain occupied during the disk wait.

---

## Phase 2 — Concurrent Order Processing

### Final Task Status

| Task Status | Count |
|---|---:|
| RanToCompletion | 14 |
| Faulted | 1 |
| Total Tasks | 15 |

`RanToCompletion` means that the task finished without throwing an exception. It does not necessarily mean that the order was completed successfully. Some tasks returned `false` because their orders were rejected, while still finishing normally.

### Final Order Results

| Status | Count |
|---|---:|
| Completed | 13 |
| Rejected | 6 |
| Failed | 1 |
| Total Orders | 20 |

**Total Processing Time:** 8.093 seconds

### Comparison with Phase 1

Phase 1 Async took 54.5 seconds to validate and save all 20 orders sequentially.

Phase 2 processed the 15 validated orders concurrently and took 8.093 seconds.

The Phase 2 processing time was shorter because multiple orders could be processed concurrently instead of waiting for each order to finish before starting the next one.

However, the two measurements are not directly comparable because they cover different operations. Phase 1 includes validation and saving all 20 orders, while Phase 2 measures the processing of the 15 validated orders.

**Important:** One task became Faulted because Order #7 encountered the exception `Shipping provider unavailable`. The exception was handled in Phase2Runner, and the order was marked as Failed. The remaining tasks continued processing.

### Task 8.5 — Task.Run for a CPU-bound Step

The `InvoiceGenerator` uses a loop of 20,000,000 iterations to simulate creating an invoice.

I used `Task.Run()` for the loop because it performs many calculations and uses the CPU.

After the loop finishes, the invoice is saved to `Data/invoices/invoice_{orderId}.txt` using `File.WriteAllTextAsync()`.

**Why did I use Task.Run() here but not for database calls?**

I used `Task.Run()` because the loop needs the CPU to do calculations.

Database calls already have async methods, so I can use `await` directly. I don't need to use `Task.Run()` for them.

## 1. What is the difference between starting a task and awaiting it?

Starting a task means beginning its execution, and the program can continue doing other work. Awaiting a task means waiting for it to finish before executing the next line of code, without blocking the thread.

## 2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?

No, not every Task creates a new thread. Some tasks use a thread from the ThreadPool, but Task.Delay() does not occupy a thread while waiting. It uses a timer, and the thread can do other work during this time.

## 3. What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous?

.Result and .Wait() block the current thread until the task finishes. This is dangerous because the thread cannot do other work, and it may cause a deadlock. We should use await instead.

## 4. What happens to an exception thrown inside a task that is never awaited?

The exception is stored inside the task. If we never await or check the task, we may not notice that an exception happened, and the code after starting the task can continue executing. Using await allows us to catch the exception with try-catch.

## 5. When should you use Task.Run(), and when should you avoid it?

We use Task.Run() for CPU-bound operations, such as heavy calculations. We should avoid using it for I/O-bound operations that already have async methods, such as reading files or calling APIs.

## 6. What is the difference between Task and Task<T>?

Task represents an asynchronous operation that does not return a result. Task<T> represents an asynchronous operation that returns a value of type T when it finishes.

## 7. Why can a lock block not contain an await?

Because lock is designed for synchronous code, and await can suspend the method while the lock is still held. For asynchronous synchronization, we can use SemaphoreSlim with WaitAsync() instead.

## 8. Why did the unprotected stock reservation oversell, and how did you prevent it?

Two threads could read the same stock quantity at the same time before it was updated. Both threads thought there was enough stock, so they reserved more than the available quantity.

We prevented this by using lock to allow only one thread at a time to execute the complete stock reservation operation (checking and updating the stock).