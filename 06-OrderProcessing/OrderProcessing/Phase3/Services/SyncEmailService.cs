
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OrderProcessing.Phase3.Services
{
    public class SyncEmailService
    {
        private readonly JsonFileStore _jsonFileStore;
        private readonly Dictionary<int, int> _failAttempts;

        public SyncEmailService(JsonFileStore jsonFileStore,Dictionary<int, int> failAttempts,bool enableFailures)
        {
            _jsonFileStore = jsonFileStore;
            _failAttempts = enableFailures? failAttempts: new Dictionary<int, int>();
        }

        public void SendConfirmation(Order order, Customer customer)
        {
            int maxFailedAttempts = 0;

            foreach (var item in _failAttempts)
            {
                if (item.Key == order.Id)
                {
                    maxFailedAttempts = item.Value;
                    break;
                }
            }

            var email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Hello {customer.Name}, your order #{order.Id} has been confirmed.",
                SentAt = DateTime.UtcNow,
                Attempts = 1,
                LastError = null
            };

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                email.Attempts = attempt;

                try
                {
                    // Render Template
                    Thread.Sleep(200);

                    // Connect SMTP
                    Thread.Sleep(300);

                    // Send Email
                    Thread.Sleep(500);

                    if (attempt <= maxFailedAttempts)
                    {
                        throw new SmtpUnavailableException("SMTP server is unavailable");
                    }

                    email.SentAt = DateTime.UtcNow;
                    email.LastError = null;

                    _jsonFileStore.Update<EmailMessage>("emails.json",emails => emails.Add(email));

                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    email.LastError = ex.Message;

                    Log.Error(
                        $"Email for Order #{order.Id} -> Attempt {attempt}/4 FAILED: {ex.Message}");

                    if (attempt < 4)
                    {
                        int retryDelay = attempt == 3? 2000: attempt * 500;

                        Thread.Sleep(retryDelay);
                    }
                }
            }

            _jsonFileStore.Update<EmailMessage>("email_retry.json",emails => emails.Add(email));

            Log.Error($"Email for Order #{order.Id} queued for retry later");
        }
    }
}
