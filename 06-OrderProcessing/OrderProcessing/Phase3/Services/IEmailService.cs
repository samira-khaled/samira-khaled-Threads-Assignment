using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public interface IEmailService
    {
        public Task SendConfirmationAsync(Order order, Customer customer,CancellationToken cancellationToken);
    }
}
