using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CartApi.Models;

using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace CartApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        // private readonly CartContext _context;
        private readonly IConfiguration _env;

        public CartController( IConfiguration env)
        {
            // _context = context;
            _env = env;
        }


        // POST: api/Cart
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public void PostCart(Cart cart)
        {
            cart.OrderStatus = "INITIATED";
            // _context.Carts.Add(cart);
            // await _context.SaveChangesAsync();

            var factory = new ConnectionFactory()
            {
                HostName = _env.GetSection("RABBITMQ_HOST").Value,
                Port = Convert.ToInt32(_env.GetSection("RABBITMQ_PORT").Value),
                UserName = _env.GetSection("RABBITMQ_USER").Value,
                Password = _env.GetSection("RABBITMQ_PASSWORD").Value
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "orders", durable: false, exclusive: false, autoDelete: false, arguments: null);

                string message = string.Empty;
                message = JsonSerializer.Serialize(cart);
                //string message = "Type: ORDER_CREATED | Order ID:" + order.Id + " | Order Price:" + order.Price + " | Order Details:" + order.Details + " | Customer Id:" + order.CustomerId;
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "orders", basicProperties: null, body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }

            // return CreatedAtAction("GetCart", new { id = cart.CartId }, cart);
        }
    }

    //     // DELETE: api/Cart/5
    //     [HttpDelete("{id}")]
    //     public async Task<IActionResult> DeleteCart(int id)
    //     {
    //         var cart = await _context.Carts.FindAsync(id);
    //         if (cart == null)
    //         {
    //             return NotFound();
    //         }

    //         _context.Carts.Remove(cart);
    //         await _context.SaveChangesAsync();

    //         return NoContent();
    //     }

    //     private bool CartExists(int id)
    //     {
    //         return _context.Carts.Any(e => e.CartId == id);
    //     }
    // }
}
