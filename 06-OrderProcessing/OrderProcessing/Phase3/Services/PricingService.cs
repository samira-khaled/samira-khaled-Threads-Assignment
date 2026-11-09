using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class PricingService
    {
        private readonly CouponRepository _couponRepository;
        private readonly CustomerRepository _customerRepository;
        private readonly ProductRepository _productRepository;
        private readonly IEnumerable<int> _failShippingForOrderIds;

        public PricingService(CouponRepository couponRepository, CustomerRepository customerRepository, ProductRepository productRepository, IEnumerable<int> failShippingForOrderIds)
        {
            _couponRepository = couponRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _failShippingForOrderIds = failShippingForOrderIds;

        }
        public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = 0;
            decimal totalWeight = 0;
            int totalQuantity = 0;
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId,ct);
            if (customer == null)
            {
                throw new CustomerNotFoundException($"Customer with ID {order.CustomerId} not found.");
            }
            Coupon? coupon = null;
            if (order.CouponCode != null)
            {
                coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);
                if (coupon == null || coupon.ExpiryDate < DateTime.UtcNow)
                    throw new InvalidCouponException(order.CouponCode);
            }
            foreach (var item in order.Items)
            {
                var product=await _productRepository.GetByIdAsync(item.ProductId,ct);
                if (product == null)
                    throw new InvalidOperationException($"Product #{item.ProductId} not found.");
                item.UnitPrice = product.Price;
                subTotal +=item.UnitPrice * item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
                totalQuantity += item.Quantity;
            }
            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
            var discountTask = CalculateDiscountAsync(order, customer, coupon, ct);
            var shippingTask = CalculateShippingCostAsync(order, totalQuantity, totalWeight, ct);
            await Task.WhenAll(discountTask, shippingTask);
            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;
            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);

            return order.TotalAmount;


        }

        private async Task<decimal> CalculateDiscountAsync(Order order,Customer customer,Coupon? coupon,CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync("PricingService -> Discount API request...",300, 300, ct);

            decimal loyaltyDiscount = 0;
            switch (customer.LoyaltyTier)
            {
                case LoyaltyTier.Gold:
                    loyaltyDiscount = 10;
                    break;
                case LoyaltyTier.Silver:
                    loyaltyDiscount = 5;
                    break;
                case LoyaltyTier.Bronze:
                    loyaltyDiscount = 0;
                    break;
            }

            decimal couponDiscount = coupon?.DiscountPercent ?? 0;

            decimal percentage = Math.Min(loyaltyDiscount + couponDiscount, 30);

            decimal discount = order.SubTotal * percentage / 100;

            return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
        }
        private async Task<decimal> CalculateShippingCostAsync(Order order,int totalQuantity,decimal totalWeight,CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync("PricingService -> Shipping API request...",300, 300, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");

            decimal shippingCost = order.SubTotal >= 1000
                ? 0
                : 5.00m + (1.50m * totalQuantity)
                    + (2.00m * totalWeight);

            return Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}
