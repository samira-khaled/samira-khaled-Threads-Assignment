using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class SyncPricingService
    {

            private readonly SyncCouponRepository _syncCouponRepository;
            private readonly SyncCustomerRepository _syncCustomerRepository;
            private readonly SyncProductRepository _syncProductRepository;
            private readonly IEnumerable<int> _failShippingForOrderIds;

            public SyncPricingService(SyncCouponRepository syncCouponRepository, SyncCustomerRepository syncCustomerRepository, SyncProductRepository syncProductRepository, IEnumerable<int> failShippingForOrderIds)
            {
                _syncCouponRepository = syncCouponRepository;
                _syncCustomerRepository = syncCustomerRepository;
                _syncProductRepository = syncProductRepository;
                _failShippingForOrderIds = failShippingForOrderIds;

            }
            public decimal CalculateTotal(Order order)
            {
                decimal subTotal = 0;
                decimal totalWeight = 0;
                int totalQuantity = 0;
                var customer =  _syncCustomerRepository.GetById(order.CustomerId);
                if (customer == null)
                {
                    throw new CustomerNotFoundException($"Customer with ID {order.CustomerId} not found.");
                }
                Coupon? coupon = null;
                if (order.CouponCode != null)
                {
                    coupon =  _syncCouponRepository.GetByCode(order.CouponCode);
                    if (coupon == null || coupon.ExpiryDate < DateTime.UtcNow)
                        throw new InvalidCouponException(order.CouponCode);
                }
                foreach (var item in order.Items)
                {
                    var product =  _syncProductRepository.GetById(item.ProductId);
                    if (product == null)
                        throw new InvalidOperationException($"Product #{item.ProductId} not found.");
                    item.UnitPrice = product.Price;
                    subTotal += item.UnitPrice * item.Quantity;
                    totalWeight += product.WeightKg * item.Quantity;
                    totalQuantity += item.Quantity;
                }
                order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
                order.Discount = CalculateDiscount(order, customer, coupon);
                order.ShippingCost = CalculateShippingCost(order, totalQuantity, totalWeight);


            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);

                return order.TotalAmount;


            }

            private decimal CalculateDiscount(Order order, Customer customer, Coupon? coupon)
            {
            Thread.Sleep(300);

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
            private decimal CalculateShippingCost(Order order, int totalQuantity, decimal totalWeight)
            {
               Thread.Sleep(300); // Simulate latency for shipping cost calculation)

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



