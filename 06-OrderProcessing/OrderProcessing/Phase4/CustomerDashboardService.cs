using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    
    public class CustomerDashboardService
    {
        private readonly CustomerRepository _customerRepository;
        private readonly OrderRepository _orderRepository;
        private readonly JsonFileStore _jsonFileStore;
        private readonly List<int> _failPaymentsForCustomerIds;
        public CustomerDashboardService(CustomerRepository customerRepository, OrderRepository orderRepository,JsonFileStore jsonFileStore, List<int>? failPaymentsForCustomerIds=null)
        {
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _jsonFileStore = jsonFileStore;
            _failPaymentsForCustomerIds = failPaymentsForCustomerIds ?? new List<int>();
        }
        public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var customerTask = _customerRepository.GetByIdAsync(customerId, cancellationToken);

            var ordersTask = _orderRepository.GetAllAsync(cancellationToken);

            var paymentsTask = LoadPaymentsAsync(customerId, cancellationToken);

            var emailsTask = LoadEmailsAsync(cancellationToken);

            var allTasks = Task.WhenAll(customerTask, ordersTask, paymentsTask, emailsTask);
            try
            {
                
                    await allTasks;
                
            }

            catch
            {
                var errors = new List<Exception>();

                foreach (var error in allTasks.Exception.Flatten().InnerExceptions)
                {
                    errors.Add(error);
                }

                if (customerTask.IsCompletedSuccessfully)
                {
                    var foundCustomer = await customerTask;

                    if (foundCustomer == null)
                    {
                        errors.Add(new Exception($"Customer #{customerId} not found"));
                    }
                }

                foreach (var error in errors)
                {
                    Console.WriteLine(error.Message);
                }

                throw new AggregateException("Dashboard loading failed", errors);
            }
            stopwatch.Stop();
            Console.WriteLine($"Dashboard loaded in {stopwatch.ElapsedMilliseconds} ms");

            var customerOrders = new List<Order>();
            var customerPayments = new List<PaymentTransaction>();
            var CustomerEmail = new List<EmailMessage>();
            var customer = await customerTask;
            var orders = await ordersTask;
            var payments = await paymentsTask;
            var emails = await emailsTask;
            if (customer == null)
            {
                throw new Exception($"Customer #{customerId} not found");
            }

            foreach (var order in orders)
            {
                if (order.CustomerId == customerId)
                {
                    customerOrders.Add(order);

                }
            }
            foreach (var pay in payments)
            {
                foreach (var order in customerOrders)
                {
                    if (pay.OrderId == order.Id)
                    {
                        customerPayments.Add(pay);
                        break;
                    }
                }
            }
            foreach(var email in emails)
            {
                foreach(var order in customerOrders)
                {
                    if(email.OrderId==order.Id)
                    {
                        CustomerEmail.Add(email);
                        break;
                    }
                }
            }
                return new CustomerDashboard
                {
                    Customer = customer,
                    Orders = customerOrders,
                    Payments = customerPayments,
                    Emails = CustomerEmail
                };
        }
        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId,CancellationToken cancellationToken)
        {
            if (_failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException("Payments store timeout");
            }
            await SimulatedLatency.DelayAsync("Opening connection to payments.json...",200, 200, cancellationToken);

            await SimulatedLatency.DelayAsync("Executing payments query...",300, 600, cancellationToken);

            return await _jsonFileStore.ReadAsync<PaymentTransaction>("payments.json",cancellationToken);
        }
        private async Task<List<EmailMessage>> LoadEmailsAsync(CancellationToken cancellationToken)
        {
            await SimulatedLatency.DelayAsync("Opening connection to emails.json...",200, 200, cancellationToken);

            await SimulatedLatency.DelayAsync("Executing emails query...",300, 600, cancellationToken);

            return await _jsonFileStore.ReadAsync<EmailMessage>("emails.json", cancellationToken);
        }
    }
}
