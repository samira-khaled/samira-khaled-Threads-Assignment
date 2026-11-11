
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OrderProcessing.Phase3.Services
{
    public class SyncPaymentGateway
    {
        private readonly List<int> _timeoutOrderIds;
        private readonly List<int> _declineOrderIds;
        private readonly JsonFileStore _jsonFileStore;
        private readonly bool _enableFailures;

        public SyncPaymentGateway(JsonFileStore jsonFileStore,List<int> timeoutOrderIds,List<int> declineOrderIds,bool enableFailures)
        {
            _jsonFileStore = jsonFileStore;
            _timeoutOrderIds = timeoutOrderIds;
            _declineOrderIds = declineOrderIds;
            _enableFailures = enableFailures;
        }

        public PaymentResult ProcessPayment(int orderId, decimal amount)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                TransactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper(),
                CreatedAt = DateTime.UtcNow
            };

            bool isDeclined = amount <= 0;
            int captureDelay = 700;

            Thread.Sleep(800);
            Thread.Sleep(500);

            if (_enableFailures)
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

                _jsonFileStore.Update<PaymentTransaction>("payments.json",records => records.Add(transaction));

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            if (_enableFailures)
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

            Thread.Sleep(captureDelay);

            transaction.Status = PaymentStatus.Captured;

            _jsonFileStore.Update<PaymentTransaction>("payments.json",records => records.Add(transaction));

            return new PaymentResult
            {
                Success = true,
                TransactionId = transaction.TransactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment successful"
            };
        }

        public void Refund(string transactionId)
        {
            _jsonFileStore.Update<PaymentTransaction>(
                "payments.json",records =>
                {
                    foreach (var transaction in records)
                    {
                        if (transaction.TransactionId == transactionId)
                        {
                            transaction.Status = PaymentStatus.Refunded;
                            break;
                        }
                    }
                });
        }    
    }
}
