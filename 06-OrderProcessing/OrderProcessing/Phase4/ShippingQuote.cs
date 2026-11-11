using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public class ShippingQuote
    {
         public string Carrier { get; set; }
         public decimal Price { get; set; }
          public long ElapsedMs { get; set; }
        
    }
}
