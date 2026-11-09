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
    public class OrderProcessor
    {
        private readonly CustomerService _customerService;
        private readonly OrderRepository _orderRepository;
        private readonly InventoryService _inventoryService;
        private readonly ProductRepository _productRepository;
        private readonly PricingService _pricingservice;
        private readonly IPaymentGateway _paymentgateway;
        private readonly IEmailService _emailService;
        private void LogStep(Order order, int stepNumber, string stepName, Stopwatch stopwatch, string result)
        {
            stopwatch.Stop();

            Log.Info($"Order #{order.Id} -> Step {stepNumber}/6 {stepName} ... {result} in {stopwatch.ElapsedMilliseconds} ms");
        }
        public OrderProcessor(CustomerService customerService, OrderRepository orderRepository, InventoryService inventoryService,
            ProductRepository productRepository, PricingService pricingservice, IPaymentGateway paymentgateway, IEmailService emailService)
        {
            _customerService = customerService;
            _orderRepository = orderRepository;
            _inventoryService = inventoryService;
            _productRepository = productRepository;
            _pricingservice = pricingservice;
            _paymentgateway = paymentgateway;
            _emailService = emailService;
        }
        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken cancellationToken)
        {
            Customer customer;
            PaymentResult paymentResult;
            try
            {
                var customerStopwatch = Stopwatch.StartNew();   
                customer = await _customerService.GetCustomerAsync(order.CustomerId, cancellationToken);
                LogStep(order, 1, "Get Customer", customerStopwatch, "OK");
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;
                Log.Error($"Order #{order.Id} -> Step 1/6 Get Customer ... FAILED: {ex.Message}");
                await _orderRepository.SaveAsync(order, cancellationToken);
                return false;

            }
            if (customer.IsActive == false)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";
                Log.Error($"Order #{order.Id} -> Step 1/6 Get Customer ... REJECTED: Customer is inactive");
                await _orderRepository.SaveAsync(order, cancellationToken);
                return false;
            }
            var stopwatch = Stopwatch.StartNew();
            bool isreserved = await _inventoryService.ReserveStockAsync(order, cancellationToken);
            LogStep(order, 2, "Reserve Stock", stopwatch, isreserved ? "OK" : "REJECTED");
            if (!isreserved)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock for one or more items.";
                await _orderRepository.SaveAsync(order, cancellationToken);
                return false;
            }
            var pricingStopwatch = Stopwatch.StartNew();
            try
            {
                order.TotalAmount = await _pricingservice.CalculateTotalAsync(order, cancellationToken);
                LogStep(order, 3, "Calculate Pricing", pricingStopwatch, "OK");
            }
            catch (InvalidCouponException ex)
            {
                pricingStopwatch.Stop();

                Log.Error($"Order #{order.Id} -> Step 3/6 Calculate Pricing ... REJECTED: {ex.Message} in {pricingStopwatch.ElapsedMilliseconds} ms");
                await _inventoryService.ReleaseStockAsync(order, cancellationToken);
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;
                await _orderRepository.SaveAsync(order, cancellationToken);
                return false;
            }
            // تعديل 2: أي خطأ تاني في التسعير (زي الشحن) لازم الطلب يتحفظ والمخزون يتحرر
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                pricingStopwatch.Stop();

                Log.Error($"Order #{order.Id} -> Step 3/6 Calculate Pricing ... FAILED: {ex.Message} in {pricingStopwatch.ElapsedMilliseconds} ms");
                await _inventoryService.ReleaseStockAsync(order, cancellationToken);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                await _orderRepository.SaveAsync(order, cancellationToken);
                return false;
            }
            //paymentResult = await _paymentgateway.ProcessPaymentAsync(order.Id, order.TotalAmount, cancellationToken);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            var paymentStopwatch = Stopwatch.StartNew();

            try
            {
                paymentResult = await _paymentgateway.ProcessPaymentAsync(
                    order.Id, order.TotalAmount, linkedCts.Token);

               
                LogStep(order, 4, "Payment", paymentStopwatch,
                    paymentResult.Status == PaymentStatus.Captured ? $"OK ({paymentResult.TransactionId})" : "DECLINED");
            }
            catch (OperationCanceledException)
            {
                paymentStopwatch.Stop();

                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Error($"Order #{order.Id} -> Step 4/6 Payment ... CANCELLED in {paymentStopwatch.ElapsedMilliseconds} ms");

                    await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);

                    order.Status = OrderStatus.Pending;
                    order.FailureReason = "Cancelled";

                    await _orderRepository.SaveAsync(order, CancellationToken.None);
                   
                }
                else if (timeoutCts.IsCancellationRequested)
                {
                    Log.Error($"Order #{order.Id} -> Step 4/6 Payment ... TIMEOUT after {paymentStopwatch.ElapsedMilliseconds} ms, released");

                    await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);

                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = "Payment gateway timeout (3s)";

                    await _orderRepository.SaveAsync(order, CancellationToken.None);

                    return false;
                }

                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                if (paymentResult.Status == PaymentStatus.Captured)
                {
                    await _paymentgateway.RefundAsync(
                        paymentResult.TransactionId!, CancellationToken.None);
                }

                await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);

                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";

                await _orderRepository.SaveAsync(order, CancellationToken.None);

                throw new OperationCanceledException(cancellationToken);
            }

            if (paymentResult.Status == PaymentStatus.Declined)
            {
                await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = "Card declined by issuer";

                await _orderRepository.SaveAsync(order, CancellationToken.None);
                return false;
            }


            order.TransactionId = paymentResult.TransactionId;
            order.Status = OrderStatus.Completed;
            var saveStopwatch = Stopwatch.StartNew();
            try
            {
                await _orderRepository.SaveAsync(order, CancellationToken.None);
                LogStep(order, 5, "Save Order", saveStopwatch, "OK");
            }
            catch
            {
                saveStopwatch.Stop();

                Log.Error($"Order #{order.Id} -> Step 5/6 Save Order ... FAILED in {saveStopwatch.ElapsedMilliseconds} ms");
                await _paymentgateway.RefundAsync(order.TransactionId, CancellationToken.None);
                await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
                order.Status = OrderStatus.Failed;
                order.FailureReason = $"Database save failed after payment; payment {order.TransactionId} refunded";

                await _orderRepository.SaveAsync(order, CancellationToken.None);
                return false;
            }
            if (order.Status == OrderStatus.Completed)
            {
                var emailStopwatch = Stopwatch.StartNew();
                await _emailService.SendConfirmationAsync(order, customer, CancellationToken.None);
                LogStep(order, 6, "Send Confirmation Email", emailStopwatch, "OK");
            }

            return order.Status == OrderStatus.Completed;
        }
    }
}