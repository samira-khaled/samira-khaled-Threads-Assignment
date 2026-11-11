using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1;

public sealed class SyncOrderValidator
{
    private readonly SyncCustomerRepository _customerRepository;
    private readonly SyncProductRepository _productRepository;
    private readonly SyncCouponRepository _couponRepository;

    public SyncOrderValidator(SyncCustomerRepository customerRepository,SyncProductRepository productRepository,SyncCouponRepository couponRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    public void ValidateOrder(Order order)
    {
        var customer = _customerRepository.GetById(order.CustomerId);

        if (customer == null)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = $"Customer #{order.CustomerId} not found";
            return;
        }

        if (customer.IsActive == false)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = "Customer is inactive";
            return;
        }

        foreach (var item in order.Items)
        {
            var product = _productRepository.GetById(item.ProductId);

            if (product == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Product #{item.ProductId} not found";
                return;
            }

            if (product.StockQuantity < item.Quantity)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Insufficient stock for '{product.Name}'";
                return;
            }
        }

        if (order.CouponCode != null)
        {
            var coupon = _couponRepository.GetByCode(order.CouponCode);

            if (coupon == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Coupon '{order.CouponCode}' does not exist";
                return;
            }

            if (coupon.ExpiryDate < DateTime.Today)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Coupon '{order.CouponCode}' has expired";
                return;
            }
        }

        order.Status = OrderStatus.Validated;
        order.FailureReason = null;
    }
}