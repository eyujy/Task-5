using System;
using System.Collections.Generic;

namespace OrderApi.Models
{
    public class Order
    {
        
        public int OrderId { get; set; }

        public List<Product> Products { get; set; }

        public double Total { get; set; }

        public string OrderStatus { get; set; }

        public int CartId { get; set; }
    }
    public class Product
    {
        public string ProductId { get; set; }
        public double ProductPrice { get; set; }

    }

}
