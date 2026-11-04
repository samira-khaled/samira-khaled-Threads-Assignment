using OrderProcessing.Shared.Models;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderPricer
    {
        private readonly PricingService _pricingService;

        public OrderPricer(PricingService pricingService)
        {
            _pricingService = pricingService;
        }
        public async Task<decimal> PriceOrderAsync(Order order,CancellationToken cancellationToken)
        {
            var subtotal = await _pricingService.CalculateSubTotalAsync(order, cancellationToken);

            var discountTask = _pricingService.GetDiscountAsync(order, cancellationToken);

            var shippingTask = _pricingService.CalculateShippingCostAsync(order, cancellationToken);

           
            var discount = await discountTask;
            var shipping = await shippingTask;

            order.TotalAmount = Math.Round(subtotal - discount + shipping,2,MidpointRounding.AwayFromZero);

            return order.TotalAmount;
        }
        public async Task<decimal> PriceOrderSequentialAsync(Order order,CancellationToken cancellationToken)
        {
            var subtotal = await _pricingService.CalculateSubTotalAsync(order, cancellationToken);

            var discount = await _pricingService.GetDiscountAsync(order, cancellationToken);

            var shipping = await _pricingService.CalculateShippingCostAsync(order, cancellationToken);

            order.TotalAmount = Math.Round(subtotal - discount + shipping,2,MidpointRounding.AwayFromZero);

            return order.TotalAmount;
        }
    }
}
