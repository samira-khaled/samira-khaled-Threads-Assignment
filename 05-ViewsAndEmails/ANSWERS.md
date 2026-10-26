# Assignment 05 — Answers

Student name: Samira Khaled Anwar Shaltout
Group / ID: Group 3

## Failures you observed

I ran the project 3 times and it crashed every time, with different errors:

1. `ArgumentException`: An item with the same key has already been added (`koshary-bowl`).
2. `InvalidOperationException`: a concurrent update corrupted the collection's state.
3. The same `InvalidOperationException`, but this time when reading `dict[key]`.

The reason is that many threads were using a normal `Dictionary` at the same time. Two threads saw that the key was not there, and both tried to add it. Also, when threads changed the `Dictionary` at the same moment, it got damaged, and even reading from it failed.

The program crashed before reaching the emails, but the queue had the same problem. If there was one job in the queue, two workers could both see `Count > 0` and both try to take it, and one of them would fail. The old code hid this with a `try/catch`.

## What you changed

The original code used a normal `Dictionary` and a normal `Queue`, so I changed them to `ConcurrentDictionary` and `ConcurrentQueue`. But that was not enough, because the methods still used them in separate steps. So I also changed the code inside:

- **Views:** the check, the `Add`, and the read-then-write were three separate steps. Now it is one line: `AddOrUpdate(product, 1, (key, oldValue) => oldValue + 1)`. It adds the product with 1 if it is new, or adds 1 to the old value.
- **Emails:** `Count > 0` then `Dequeue()` were two steps. Now I use `TryDequeue`, which checks and takes the job together. I removed the `try/catch` because it only hid the error and try dequeue return true or false.
- I read `ProducersDone` before taking a job, so a worker does not stop while the last job is still coming.
- `SentEmails` is a normal `List`, so I kept the `lock` on it.

## Output after the fix :
Views expected=50000, actual=50000
Email jobs placed=20, unique sent=20, total sends=20

## Collection safety vs business-rule safety

A thread-safe collection only makes each single operation safe, like one `AddOrUpdate` or one `TryDequeue`. If the rule needs two steps, like "read, then write", another thread can come in between them.

For example, with `ConcurrentDictionary`, `Views[key] = Views[key] + 1` can still lose a view. Two threads read the same old value, and both write the same new value, so the count goes up by one instead of two.

So I need one operation that does the whole rule (`AddOrUpdate`, `TryDequeue`), or a `lock` around the whole rule.