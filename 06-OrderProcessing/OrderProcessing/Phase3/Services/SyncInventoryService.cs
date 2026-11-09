using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class SyncInventoryService
    {

            private readonly SyncProductRepository _syncProductRepository;

            public SyncInventoryService(SyncProductRepository syncProductRepository)
            {
                _syncProductRepository = syncProductRepository;
            }
            public   bool ReserveStock(Order order)
            {
                Dictionary<int, int> productQuantities = new Dictionary<int, int>();
                Dictionary<int, int> reservedProducts = new Dictionary<int, int>();

                foreach (var item in order.Items)
                {
                    var product =  _syncProductRepository.GetById(item.ProductId);

                    if (product == null)
                    {
                        return false;
                    }
                    if (!productQuantities.ContainsKey(item.ProductId))
                    {
                        productQuantities[item.ProductId] = 0;
                    }
                    productQuantities[item.ProductId] += item.Quantity;
                    if (productQuantities[item.ProductId] > product.StockQuantity)
                    {
                        return false;
                    }
                }
                foreach (var item in productQuantities)
                {

                    bool reserved =  _syncProductRepository.ReserveStock(item.Key, item.Value);
                    if (!reserved)
                    {
                        foreach (var reverse in reservedProducts)
                        {
                            bool released = _syncProductRepository.ReleaseStock(reverse.Key, reverse.Value);

                            if (!released)
                            {
                                throw new InvalidOperationException(
                                    $"Rollback failed for Product ID {reverse.Key}.");
                            }
                        }

                        return false;
                    }
                    reservedProducts[item.Key] = item.Value;

                }

                return true;
            }
            public void ReleaseStock(Order order)
            {
                foreach (var item in order.Items)
                {
                    bool released = _syncProductRepository.ReleaseStock(item.ProductId, item.Quantity);
                    if (!released)
                    {
                        throw new InvalidOperationException("Stock release failed.");
                    }
                }

            }
        }
    }



