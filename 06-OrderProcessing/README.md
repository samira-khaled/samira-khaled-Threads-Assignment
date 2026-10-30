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

**1. Why are the sync and async times almost the same when orders are awaited one after another?**

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