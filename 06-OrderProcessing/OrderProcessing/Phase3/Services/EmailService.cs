using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class EmailService : IEmailService
    {
        //appends to emails.json
        private readonly JsonFileStore _jsonFileStore;
        private readonly Dictionary<int, int> _failAttempts;//لتخزين الايميالات الفاشلة
                                                            //ليه Dictionary مش List؟

      
        public EmailService(JsonFileStore jsonFileStore, Dictionary<int, int> failAttempts, bool enableFailures)
        {
            _jsonFileStore = jsonFileStore;
            _failAttempts = enableFailures ? failAttempts : new Dictionary<int, int>();
            
        }
        public async Task SendConfirmationAsync(Order order, Customer customer,CancellationToken ct)
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


            //await _jsonFileStore.UpdateAsync<EmailMessage>("emails.json", emails => emails.Add(email),ct);
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                email.Attempts = attempt;

                try
                {
                    // Render Template
                    await Task.Delay(200, ct);

                    // Connect SMTP
                    await Task.Delay(300, ct);

                    // Send Email
                    await Task.Delay(500, ct);

                    if (attempt <= maxFailedAttempts)
                    {
                        throw new SmtpUnavailableException("SMTP server is unavailable");
                    }
                    email.SentAt = DateTime.UtcNow;
                    email.LastError = null;

                    await _jsonFileStore.UpdateAsync<EmailMessage>("emails.json",emails => emails.Add(email),ct);
                    Log.Info($"Email for Order #{order.Id} sent successfully");
                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    email.LastError = ex.Message;
                    Log.Error($"Email for Order #{order.Id} -> Attempt {attempt}/4 FAILED: {ex.Message}");

                    if (attempt < 4)
                    {
                        int retryDelay = attempt == 3 ? 2000 : attempt * 500;
                        await Task.Delay(retryDelay, ct);
                    }
                }
            }
            await _jsonFileStore.UpdateAsync<EmailMessage>("email_retry.json",emails => emails.Add(email),ct);

            Log.Error($"Email for Order #{order.Id} queued for retry later");
        }
    }
}
