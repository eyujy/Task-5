using System;
using Microsoft.EntityFrameworkCore;

namespace CartApi.Models
{
    public class CartContext : DbContext
    {
        public CartContext(DbContextOptions<CartContext> options)
            : base(options)
        {
        }

        public DbSet<Cart> Carts { get; set; }
    }
}
