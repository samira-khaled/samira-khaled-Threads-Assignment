using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
  
        public class SyncOrderProcessor
        {
            private readonly SyncCustomerService _customerService;
            private readonly OrderRepository _orderRepository;
            private readonly SyncInventoryService _inventoryService;
            private readonly SyncPricingService _pricingService;
            private readonly SyncPaymentGateway _paymentGateway;
            private readonly SyncEmailService _emailService;

            public SyncOrderProcessor(SyncCustomerService customerService,OrderRepository orderRepository,SyncInventoryService inventoryService,SyncPricingService pricingService,SyncPaymentGateway paymentGateway,SyncEmailService emailService)
            {
                _customerService = customerService;
                _orderRepository = orderRepository;
                _inventoryService = inventoryService;
                _pricingService = pricingService;
                _paymentGateway = paymentGateway;
                _emailService = emailService;
            }

            private void LogStep(Order order,int stepNumber,string stepName,Stopwatch stopwatch,string result)
            {
                stopwatch.Stop();

                Log.Info(
                    $"Order #{order.Id} -> Step {stepNumber}/6 {stepName} ... {result} in {stopwatch.ElapsedMilliseconds} ms");
            }

            public bool ProcessOrder(Order order)
            {
                Customer customer;
                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    customer = _customerService.GetCustomer(order.CustomerId);

                    LogStep(order, 1, "Get Customer", stopwatch, "OK");
                }
                catch (CustomerNotFoundException ex)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = ex.Message;

                    Log.Error($"Order #{order.Id} -> Step 1/6 Get Customer ... FAILED: {ex.Message}");

                    _orderRepository.Save(order);

                    return false;
                }

                if (!customer.IsActive)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = "Customer is inactive";

                    Log.Error($"Order #{order.Id} -> Step 1/6 Get Customer ... REJECTED: Customer is inactive");

                    _orderRepository.Save(order);

                    return false;
                }

                // Step 2: Reserve Stock
                var inventoryStopwatch = Stopwatch.StartNew();

                bool isReserved = _inventoryService.ReserveStock(order);

                LogStep(
                    order,
                    2,
                    "Reserve Stock",
                    inventoryStopwatch,
                    isReserved ? "OK" : "REJECTED");

                if (!isReserved)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = "Insufficient stock for one or more items.";

                    _orderRepository.Save(order);

                    return false;
                }

                // Step 3: Calculate Pricing
                var pricingStopwatch = Stopwatch.StartNew();

                try
                {
                    order.TotalAmount = _pricingService.CalculateTotal(order);

                    LogStep(order, 3, "Calculate Pricing", pricingStopwatch, "OK");
                }
                catch (InvalidCouponException ex)
                {
                    pricingStopwatch.Stop();

                    Log.Error($"Order #{order.Id} -> Step 3/6 Calculate Pricing ... REJECTED: {ex.Message} in {pricingStopwatch.ElapsedMilliseconds} ms");

                    _inventoryService.ReleaseStock(order);

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = ex.Message;

                    _orderRepository.Save(order);

                    return false;
                }
                catch (Exception ex)
                {
                    pricingStopwatch.Stop();

                    Log.Error($"Order #{order.Id} -> Step 3/6 Calculate Pricing ... FAILED: {ex.Message} in {pricingStopwatch.ElapsedMilliseconds} ms");

                    _inventoryService.ReleaseStock(order);

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    _orderRepository.Save(order);

                    return false;
                }

                // Step 4: Payment
                var paymentStopwatch = Stopwatch.StartNew();

                PaymentResult paymentResult =
                    _paymentGateway.ProcessPayment(order.Id, order.TotalAmount);

                LogStep(
                    order,
                    4,
                    "Payment",
                    paymentStopwatch,
                    paymentResult.Status == PaymentStatus.Captured
                        ? $"OK ({paymentResult.TransactionId})"
                        : "DECLINED");

                if (paymentResult.Status == PaymentStatus.Declined)
                {
                    _inventoryService.ReleaseStock(order);

                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = "Card declined by issuer";

                    _orderRepository.Save(order);

                    return false;
                }

                // Step 5: Save Order
                order.TransactionId = paymentResult.TransactionId;
                order.Status = OrderStatus.Completed;

                var saveStopwatch = Stopwatch.StartNew();

                try
                {
                    _orderRepository.Save(order);

                    LogStep(order, 5, "Save Order", saveStopwatch, "OK");
                }
                catch (Exception ex)
                {
                    saveStopwatch.Stop();

                    Log.Error($"Order #{order.Id} -> Step 5/6 Save Order ... FAILED: {ex.Message}");

                    _paymentGateway.Refund(order.TransactionId);

                    _inventoryService.ReleaseStock(order);

                    order.Status = OrderStatus.Failed;
                    order.FailureReason =
                        $"Database save failed after payment; payment {order.TransactionId} refunded";

                    _orderRepository.Save(order);

                    return false;
                }

                // Step 6: Send Confirmation Email
                if (order.Status == OrderStatus.Completed)
                {
                    var emailStopwatch = Stopwatch.StartNew();

                    _emailService.SendConfirmation(order, customer);

                    LogStep(order, 6, "Send Confirmation Email", emailStopwatch, "OK");
                }

                return order.Status == OrderStatus.Completed;
            }
        }
    }


