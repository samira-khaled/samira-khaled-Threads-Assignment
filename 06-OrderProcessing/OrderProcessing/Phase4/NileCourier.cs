using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class NileCourier : IShippingCarrier
{
    private readonly ProductRepository _productRepository;
    private readonly IEnumerable<int> _failForOrderIds;

    public NileCourier(ProductRepository productRepository,IEnumerable<int> failForOrderIds)
    {
        _productRepository = productRepository;
        _failForOrderIds = failForOrderIds;
    }

    public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
    {
        foreach (int orderId in _failForOrderIds)
        {
            if (orderId == order.Id)
            {
                throw new ShippingProviderUnavailableException("NileCourier is unavailable.");
            }
        }
        await SimulatedLatency.DelayAsync("NileCourier handshake...", 150, 150, ct);

        await SimulatedLatency.DelayAsync("NileCourier getting quote...", 800, 1200, ct);

        if (_failForOrderIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException("NileCourier is unavailable.");
        }

        decimal totalWeight = 0;
        int totalQuantity = 0;

        var products = await _productRepository.GetAllAsync(ct);

        foreach (var item in order.Items)
        {
            Product? product = null;

            foreach (var p in products)
            {
                if (p.Id == item.ProductId)
                {
                    product = p;
                    break;
                }
            }

            if (product == null)
                throw new InvalidOperationException($"Product #{item.ProductId} not found.");

            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
        }

        decimal basePrice = order.SubTotal >= 1000? 0 : 5.00m + (1.50m * totalQuantity) + (2.00m * totalWeight);

        decimal price = basePrice * 1.10m;

        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}