using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class FastShip : IShippingCarrier
{
    private readonly ProductRepository _productRepository;
    private readonly IEnumerable<int> _failForOrderIds;

    public FastShip(ProductRepository productRepository,IEnumerable<int> failForOrderIds)
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
                throw new ShippingProviderUnavailableException("FastShip is unavailable.");
            }
        }
        // Handshake: 150 ms
        await SimulatedLatency.DelayAsync("FastShip handshake...", 150, 150, ct);

        // Response time: 500–1500 ms
        await SimulatedLatency.DelayAsync("FastShip getting quote...", 500, 1500, ct);

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

        decimal priceFactor = 1.00m;

        return Math.Round(basePrice * priceFactor,2,MidpointRounding.AwayFromZero);
    }
}