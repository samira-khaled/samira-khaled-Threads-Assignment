using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace OrderProcessing.Phase1
{
    public class OrderValidator
    {
        private readonly CustomerRepository _customerRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;

        public OrderValidator(CustomerRepository customerRepository,OrderRepository orderRepository,ProductRepository productRepository,CouponRepository couponRepository)
        {
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _couponRepository = couponRepository;
        }
        public async Task ValidateOrderAsync(Order order,CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId,cancellationToken);
            if (customer == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer #{order.CustomerId} not found";
               await _orderRepository.SaveAsync(order, cancellationToken);
                return;
            }
            if(customer.IsActive==false)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";
                await _orderRepository.SaveAsync(order, cancellationToken);
                return;
            }
            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

                if (product == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product #{item.ProductId} not found";
                    await _orderRepository.SaveAsync(order, cancellationToken);
                    return;
                }
                if(product.StockQuantity<item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}'";
                    await _orderRepository.SaveAsync(order, cancellationToken);
                    return;
                }

            }
            if (order.CouponCode != null)
            {
                var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, cancellationToken);

                if (coupon == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon '{order.CouponCode}' does not exist";
                    await _orderRepository.SaveAsync(order, cancellationToken);
                    return;
                }
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon '{order.CouponCode}' has expired";
                    await _orderRepository.SaveAsync(order, cancellationToken);
                    return;
                }
            }
            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
            await _orderRepository.SaveAsync(order, cancellationToken);
        }
    }
}
