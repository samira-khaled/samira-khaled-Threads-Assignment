
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class InvoiceGenerator
    {
        public async Task GenerateAsync(Order order)
        {
            
            double result = await Task.Run(() =>
            {
                double total = 0;

                for (int i = 0; i < 20_000_000; i++)
                {
                    total += Math.Sqrt(i);
                }

                return total;
            });

            StringBuilder content = new StringBuilder();

            content.AppendLine($"Invoice ID: {order.Id}");
            content.AppendLine($"Customer ID: {order.CustomerId}");
            content.AppendLine("--------------------------------");

            foreach (var item in order.Items)
            {
                content.AppendLine($"Product ID: {item.ProductId}, Quantity: {item.Quantity}");
            }

            content.AppendLine("--------------------------------");
            content.AppendLine($"Subtotal: {order.SubTotal}");
            content.AppendLine($"Discount: {order.Discount}");
            content.AppendLine($"Total: {order.SubTotal - order.Discount}");
            content.AppendLine($"Rendering Result: {result}");

         
            
            Directory.CreateDirectory(DataFiles.InvoicesFolder);

          
            string filePath = Path.Combine(DataFiles.InvoicesFolder, $"invoice_{order.Id}.txt");

            await File.WriteAllTextAsync(filePath, content.ToString());
        }
    }
}
