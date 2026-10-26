using System.Collections.Concurrent;

namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly ConcurrentDictionary<string, int> Views = new();
    public readonly ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
  
        Views.AddOrUpdate(product,1,(key,oldvalue)=>1+oldvalue);
    }

    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");
    }

    public void EmailWorker()
    {
        while (true)
        {
                string? job = null;
                if (EmailJobs.TryDequeue(out job))
                {// طالما دخل يبقى فى ايميل هيحطو فى الليست 
                    lock (_sentSync)
                    SentEmails.Add(job);
                    continue;
                }
                if (ProducersDone)// مفيش ايميل
                    return;

                Thread.Sleep(1);
           

        }
    }
}
