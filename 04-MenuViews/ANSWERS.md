# Assignment 04 — Answers

Student name: Samira Khaled Anwar Shaltout  
Group / ID: Group 3

## R1 - Expected vs Actual

I ran the project three times before fixing the code.

Expected: 400,000

Actual results:
- Run 1: 371,055
- Run 2: 177,635
- Run 3: 224,214

The actual count was different in each run and was always less than the expected count.

## Why Expected and Actual disagree

`Views++` is not an atomic operation. It reads the current value, increases it by one, and writes the new value. When multiple threads update the counter at the same time, some updates can be lost. This is called a race condition.

## What you changed

I replaced `Views++` with `Interlocked.Increment(ref Views)`. This makes each increment atomic and prevents lost updates when multiple threads update the counter concurrently.

## When would you protect the update differently

I would use `lock` if the update involved a condition or multiple operations that must be executed together. For a single counter increment, `Interlocked.Increment` is more suitable.