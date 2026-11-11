using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public class SyncCustomerService
    {
        private readonly SyncCustomerRepository _syncCustomerRepository;
        public SyncCustomerService(SyncCustomerRepository customerRepository)
        {
            _syncCustomerRepository = customerRepository;
        }
        public Customer GetCustomer(int id)
        {
            var customer =  _syncCustomerRepository.GetById(id);
            if (customer == null)
            {
                throw new CustomerNotFoundException($"Customer #{id} not found");
            }
            return customer;

        }
    }
}
