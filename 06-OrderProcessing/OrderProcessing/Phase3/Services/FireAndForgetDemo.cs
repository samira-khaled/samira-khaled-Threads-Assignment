using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    
    public class FireAndForgetDemo
    {
        private readonly OrderRepository _orderRepository;
        private readonly EmailService _emailService;
        public FireAndForgetDemo(OrderRepository orderRepository,EmailService emailService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
        }
        public async Task ProcessOrderAsync(Order order, Customer customer)
        {
            await _orderRepository.SaveAsync(order);
        //#pragma warning disable CS4014
        //    _emailService.SendConfirmationAsync(order, customer, CancellationToken.None);
        //#pragma warning restore CS4014
        await _emailService.SendConfirmationAsync(order, customer, CancellationToken.None);

            Console.WriteLine("Order processing finished");
        }
    }
}
