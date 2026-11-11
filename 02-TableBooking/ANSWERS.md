# Assignment 02 — Answers

Student name: Samira Khaled Anwar Shaltout
Group / ID: Group 3

## What is wrong

I ran the project 3 times before the fix. The results were different each time:

| Run | Booked | TablesLeft |
|-----|--------|------------|
| 1   | 21     | 4          |
| 2   | 29     | -23        |
| 3   | 24     | -19        |

There are only 5 tables and 40 customers, so the correct result should be
`Booked=5` and `TablesLeft=0`. My results were wrong, and they changed in
every run.

The reason is that many customers (threads) work at the same time and they
overlap with each other. For example, if only one table is left, two
customers can both see "one table left" at the same moment. Both of them
think the table is free, so both of them book it. This is why more than 5
customers booked a table and `TablesLeft` became a negative number.

Also, `TablesLeft--` and `Booked++` are not one step. Each one reads the
number, changes it, and writes it back. When two threads do this together,
one update is lost, so the numbers are wrong.

This problem is called a **race condition**.

## How you fixed it

I used a `lock` inside the `Book` method (in `FridayNightBooking.cs`). The
lock lets only one customer enter at a time, and the other customers wait
for their turn.

I put the check (`TablesLeft <= 0`), the decrease, the increase, and the
`Console.WriteLine` all inside the same lock. So when a customer checks that
a table is free, no one else can take it before he books it. Also, the
number that is printed is the same number that this customer just created.

All threads use the same lock object, so they all wait for each other.

Output after the fix:

```
Customer-01 booked a table. Left=4
Customer-03 booked a table. Left=3
Customer-02 booked a table. Left=2
Customer-04 booked a table. Left=1
Customer-05 booked a table. Left=0

Booked=5, TablesLeft=0
```

Now exactly 5 customers book a table, and the totals are correct in every run.