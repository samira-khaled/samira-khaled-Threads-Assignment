
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1.Repositories;

public sealed class CouponRepository
{
    private readonly JsonFileStore _store;

    public CouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Coupon?> GetByCodeAsync(string code,CancellationToken ct = default)
    {
        await SimulatedLatency.DelayAsync("Opening connection to coupons.json...",200, 200, ct);

        await SimulatedLatency.DelayAsync($"Executing query for Coupon '{code}'...",300, 600, ct);

        var coupons = await _store.ReadAsync<Coupon>("coupons.json", ct);

        foreach (var coupon in coupons)
        {
            if (string.Equals(coupon.Code,code,StringComparison.OrdinalIgnoreCase))
            {
                Log.Success($"Coupon '{code}' found.");
                return coupon;
            }
        }

        Log.Info($"Coupon '{code}' not found.");
        return null;
    }
}
