using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1.Repositories;

public sealed class SyncCouponRepository
{
    private readonly JsonFileStore _store;

    public SyncCouponRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Coupon? GetByCode(string code)
    {
        Log.Info("Opening connection to coupons.json...");
        Thread.Sleep(200);

        Log.Info($"Executing query for Coupon '{code}'...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var coupons = _store.Read<Coupon>("coupons.json");

        foreach (var coupon in coupons)
        {
            if (string.Equals(coupon.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                Log.Success($"Coupon '{code}' found.");
                return coupon;
            }
        }

        Log.Info($"Coupon '{code}' not found.");
        return null;
    }
}