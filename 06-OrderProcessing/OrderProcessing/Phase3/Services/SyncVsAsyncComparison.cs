
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Phase3.Services
{
    public class SyncVsAsyncComparison
    {
        private readonly JsonFileStore _store;
        private readonly SyncOrderProcessor _syncOrderProcessor;
        private readonly OrderProcessor _asyncOrderProcessor;
        private readonly DataSeeder _dataSeeder;

        public SyncVsAsyncComparison(JsonFileStore store,SyncOrderProcessor syncOrderProcessor,OrderProcessor asyncOrderProcessor,DataSeeder dataSeeder)
        {
            _store = store;
            _syncOrderProcessor = syncOrderProcessor;
            _asyncOrderProcessor = asyncOrderProcessor;
            _dataSeeder = dataSeeder;
        }

        public async Task CompareAsync(List<Order> orders)
        {
            // Sync Comparison
            await _dataSeeder.ResetAsync();

            var syncThreads = new List<Thread>();
            var syncResults = new List<bool>();
            object syncLock = new object();

            int syncThreadCount = 0;

            var syncStopwatch = Stopwatch.StartNew();

            foreach (var order in orders)
            {
                Thread thread = new Thread(() =>
                {
                    Interlocked.Increment(ref syncThreadCount);

                    bool result = _syncOrderProcessor.ProcessOrder(order);

                    lock (syncLock)
                    {
                        syncResults.Add(result);
                    }
                });

                syncThreads.Add(thread);
                thread.Start();
            }

            foreach (var thread in syncThreads)
            {
                thread.Join();
            }

            syncStopwatch.Stop();

            // Async Comparison
            await _dataSeeder.ResetAsync();

            var asyncTasks = new List<Task<bool>>();

            int threadPoolCountBefore = ThreadPool.ThreadCount;

            var asyncStopwatch = Stopwatch.StartNew();

            foreach (var order in orders)
            {
                asyncTasks.Add(_asyncOrderProcessor.ProcessOrderAsync(order, CancellationToken.None));
            }

            var asyncResults = new List<bool>();

            foreach (var task in asyncTasks)
            {
                asyncResults.Add(await task);
            }

            asyncStopwatch.Stop();

            int threadPoolCountAfter = ThreadPool.ThreadCount;

            // Results
            int syncCompleted = 0;
            int asyncCompleted = 0;

            foreach (var result in syncResults)
            {
                if (result)
                    syncCompleted++;
            }

            foreach (var result in asyncResults)
            {
                if (result)
                    asyncCompleted++;
            }

            Log.Info("========== SYNC VS ASYNC COMPARISON ==========");

            Log.Info($"Sync Time: {syncStopwatch.ElapsedMilliseconds} ms");
            Log.Info($"Sync Threads Created: {syncThreadCount}");
            Log.Info($"Sync Completed Orders: {syncCompleted}");
            Log.Info($"Sync Rejected Orders: {syncResults.Count - syncCompleted}");

            Log.Info("----------------------------------------------");

            Log.Info($"Async Time: {asyncStopwatch.ElapsedMilliseconds} ms");
            Log.Info($"ThreadPool.ThreadCount Before: {threadPoolCountBefore}");
            Log.Info($"ThreadPool.ThreadCount After: {threadPoolCountAfter}");
            Log.Info($"Async Completed Orders: {asyncCompleted}");
            Log.Info($"Async Rejected Orders: {asyncResults.Count - asyncCompleted}");

            Log.Info("==============================================");
        }
    }
}
