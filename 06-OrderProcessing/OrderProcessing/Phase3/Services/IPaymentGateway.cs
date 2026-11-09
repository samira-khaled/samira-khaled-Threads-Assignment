using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public interface IPaymentGateway
    {
      public  Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct);
        public Task RefundAsync(string transactionId, CancellationToken cancellationToken);
    }
}
