# Assignment 03 — Answers

Student name: Samira Khaled Anwar Shaltout
Group / ID: Group 3

## Q1: What is System.Threading.Monitor used for?

`Monitor` is used to let only one thread run a part of code at a time. It
protects shared data from race conditions.

## Q2: What does the C# lock statement compile to?

The C# `lock` statement is compiled to `Monitor.Enter` and `Monitor.Exit`
inside a `try/finally` block. The `finally` makes sure the lock is always
released, even if an exception happens. Without it, the other threads would
wait forever.

## Q3: Differences between lock and calling Monitor directly

1. With `lock`, the compiler always writes the `try/finally` for me, so the
   lock is released even if there is an exception. With `Monitor.Enter` and
   `Monitor.Exit`, I must write the `try/finally` myself, and if I forget it,
   the lock may never be released.
2. `lock` waits until it gets the lock, as long as it takes. `Monitor` gives
   more control, for example `Monitor.TryEnter` can give up after a timeout.

In short, `lock` is simpler and safer for normal cases, and `Monitor` is
better when I need more control.

## Q4: When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain lock?

A plain `lock` only does one thing: it makes a thread wait until it gets the
lock. I use `Monitor` methods when I need more than that:

- **Monitor.TryEnter:** when a thread should not wait forever for a lock. If it
  cannot get the lock in a given time, it stops waiting and does something else
  (for example, it prints "busy" and tries again later).
- **Monitor.Wait:** when a thread has the lock but cannot continue yet. For
  example, a worker thread wants a job, but the queue is empty. `Wait` releases
  the lock and puts the thread to sleep until something changes. This is better
  than checking the queue again and again in a loop.
- **Monitor.Pulse:** to wake up a thread that is sleeping in `Wait`. For
  example, when a thread adds a new job to the queue, it calls `Pulse` to tell
  the waiting worker that it can continue.

## Q5: The same critical section with lock and with Monitor

**Version 1: using `lock`**

```csharp
lock (_lock)
{
    if (TablesLeft <= 0)
        return;

    TablesLeft--;
    Booked++;
}
```

**Version 2: using `Monitor.Enter` and `Monitor.Exit`**

```csharp
Monitor.Enter(_lock);
try
{
    if (TablesLeft <= 0)
        return;

    TablesLeft--;
    Booked++;
}
finally
{
    Monitor.Exit(_lock);
}
```

Both versions do the same thing: only one thread can enter the critical
section at a time. The `lock` version is shorter because the compiler writes
the `try/finally` for me. In the `Monitor` version I write it myself, and
`Monitor.Exit` is inside `finally`, so the lock is released even if there is a
`return` or an exception.

## Links

- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock
- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor