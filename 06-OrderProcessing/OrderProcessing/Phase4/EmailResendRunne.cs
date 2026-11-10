using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Models;
using System.Diagnostics;

namespace OrderProcessing.Phase4
{
    public class EmailResendRunner
    {
        private readonly IEmailService _emailService;

        public EmailResendRunner(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // 30 إيميل تجريبي
            var jobs = new List<(Order order, Customer customer)>();
            for (int id = 1001; id <= 1030; id++)
            {
                var order = new Order { Id = id, CustomerId = 1 };
                var customer = new Customer { Id = 1, Name = "Test Customer", Email = "test.customer@example.com" };
                jobs.Add((order, customer));
            }

            int current = 0;   //عدد عمليات الإرسال الشغالة حاليًا
            int max = 0;       //	أعلى عدد عمليات إرسال اشتغلت مع بعض
            var sw = Stopwatch.StartNew();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,   //معناها إن الـ Parallel Library مسموح لها تشغّل بحد أقصى 4 عمليات في نفس الوقت.
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(jobs, options, async (job, token) =>
            {
                int now = Interlocked.Increment(ref current);

                // نحدّث max بشكل آمن بين الـ threads
                int seen;
                while (now > (seen = Volatile.Read(ref max)))
                {
                    if (Interlocked.CompareExchange(ref max, now, seen) == seen) break;
                }

                try
                {
                    await _emailService.SendConfirmationAsync(job.order, job.customer, token);
                }
                finally
                {
                    Interlocked.Decrement(ref current);
                }
            });

            sw.Stop();
            Console.WriteLine($"Emails sent: {jobs.Count}");
            Console.WriteLine($"Max concurrent sends: {max}");
            Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds:F1} s");
        }
    }
}