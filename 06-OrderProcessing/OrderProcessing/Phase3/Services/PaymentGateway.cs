using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Transactions;

namespace OrderProcessing.Phase3.Services
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly List<int> _timeoutOrderIds;//الأوردرات اللي الدفع بتاعها هيتأخر
        private readonly List<int> _declineOrderIds;
        private readonly JsonFileStore _jsonFileStore;
        private readonly bool _enableFailures;
        public PaymentGateway(JsonFileStore jsonFileStore,List<int> timeoutOrderIds,List<int> declineOrderIds,bool enableFailures)
        {
            _jsonFileStore = jsonFileStore;
            _timeoutOrderIds = timeoutOrderIds;
            _declineOrderIds = declineOrderIds;
            _enableFailures = enableFailures;
        }
        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            var transaction = new PaymentTransaction();
            transaction.OrderId = orderId;
            transaction.Amount = amount;
            transaction.TransactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();
            transaction.CreatedAt = DateTime.UtcNow;
            bool isDeclined = amount <= 0;
            int captureDelay = 700;
            await Task.Delay(800, ct);
            await Task.Delay(500, ct);
            if(_enableFailures)
            {
                foreach (var failId in _declineOrderIds)
                {
                    if (orderId == failId)
                    {
                        isDeclined = true;
                        break;
                    }
                }
            }
            
            if (isDeclined)
            {
                transaction.Status = PaymentStatus.Declined;

                await _jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json", records=>records.Add(transaction), ct);

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };   
            }
            if(_enableFailures)
            {
                foreach (var failId in _timeoutOrderIds)
                {
                    if (orderId == failId)
                    {
                        captureDelay = 5000;
                        break;
                    }
                }
            }
            await Task.Delay(captureDelay, ct);
            transaction.Status = PaymentStatus.Captured;
            await _jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json", records=>records.Add(transaction), ct);
            return new PaymentResult
            {
                Success = true,
                TransactionId = transaction.TransactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment successful"
            };



        }

        public async Task RefundAsync(string transactionId, CancellationToken ct)
        {
            await _jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json",records =>
        {
            foreach (var transaction in records)
            {
                if (transaction.TransactionId == transactionId)
                {
                transaction.Status = PaymentStatus.Refunded;
                break;
                }
            }
        },ct);
        }

       
    }
}
