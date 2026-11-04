using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2
{
    public class PricingService
    {
        private readonly ProductRepository _productRepository;
        private readonly CustomerRepository _customerRepository;
        private readonly CouponRepository _couponRepository;
        private readonly IEnumerable<int> _failShippingForOrderIds;

        public PricingService(ProductRepository productRepository, CustomerRepository customerRepository, CouponRepository couponRepository, IEnumerable<int> failShippingForOrderIds)
        {
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _couponRepository = couponRepository;
            _failShippingForOrderIds = failShippingForOrderIds;
        }

        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken cancellationToken)
        {
            decimal total = 0;
            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Product #{item.ProductId} not found.");
                }

                item.UnitPrice = product.Price;
                total = total + (item.UnitPrice * item.Quantity);
            }
            order.SubTotal = Math.Round(total, 2, MidpointRounding.AwayFromZero);
            return order.SubTotal;
        }

        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken cancellationToken)
        {
            await SimulatedLatency.DelayAsync("PricingService -> DNS/handshake...", 150, 150, cancellationToken);
            await SimulatedLatency.DelayAsync("PricingService -> Sending discount request...", 300, 300, cancellationToken);
            await SimulatedLatency.DelayAsync("PricingService -> Waiting for discount response...", 500, 1500, cancellationToken);
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer is null)
            {
                throw new CustomerNotFoundException($"Customer #{order.CustomerId} not found");
            }
            Coupon? coupon = null;
            if (order.CouponCode is not null)
            {
                coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, cancellationToken);
            }

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

            decimal couponDiscount = 0;
            if (coupon != null)
            {
                couponDiscount = coupon.DiscountPercent;
            }
            decimal discountPercentage = Math.Min(loyaltyDiscount + couponDiscount, 30);

            decimal Discount = order.SubTotal * discountPercentage / 100;
            order.Discount = Math.Round(Discount, 2, MidpointRounding.AwayFromZero);
            return order.Discount;
        }

        // 5.00 + 1.50 × totalquantity + 2.00 × total weight (kg); 0 when SubTotal >= 1000
        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken cancellationToken)
        {
            decimal ShippingCost = 0;
            decimal TotalWeight = 0;
            decimal totalquantity = 0;
            await SimulatedLatency.DelayAsync("PricingService -> DNS/handshake...", 150, 150, cancellationToken);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            await SimulatedLatency.DelayAsync("PricingService -> Sending shipping request...", 300, 300, cancellationToken);
            await SimulatedLatency.DelayAsync("PricingService -> Waiting for shipping response...", 500, 1500, cancellationToken);
            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is null)
                {
                    throw new InvalidOperationException($"Product #{item.ProductId} not found.");
                }
                totalquantity = totalquantity + item.Quantity;
                TotalWeight = TotalWeight + (product.WeightKg * item.Quantity);
            }

            if (order.SubTotal >= 1000)
            {
                ShippingCost = 0;
            }
            else
            {
                ShippingCost = 5.00m + (1.50m * totalquantity) + (2.00m * TotalWeight);
            }
            

            order.ShippingCost = Math.Round(ShippingCost, 2, MidpointRounding.AwayFromZero);
            return order.ShippingCost;
        }
    }
}