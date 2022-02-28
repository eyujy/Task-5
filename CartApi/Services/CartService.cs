using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


using Microsoft.Extensions.Configuration;
using CartApi.Models;
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class CartService : BackgroundService
{
    private readonly ILogger _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly IConfiguration _env;
    // private readonly CartContext  _context;
    private readonly IServiceScopeFactory _scopeFactory;

    public CartService(ILoggerFactory loggerFactory, IConfiguration env, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        this._logger = loggerFactory.CreateLogger<CartService>();
        // _context = context;
        _env = env;

        InitRabbitMQ();
    }

    private void InitRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            // HostName = "localhost",
            // Port = 31500,
            // UserName = "guest",
            // Password = "guest"
            HostName = _env.GetSection("RABBITMQ_HOST").Value,
            Port = Convert.ToInt32(_env.GetSection("RABBITMQ_PORT").Value),
            // Port = Convert.ToInt32(_env.GetSection("RABBITMQ_PORT").Value),
            UserName = _env.GetSection("RABBITMQ_USER").Value,
            Password = _env.GetSection("RABBITMQ_PASSWORD").Value
        };

        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        //_channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
        _channel.QueueDeclare("order_processed", true, false, false, null);

        // _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
        // _channel.BasicQos(0, 1, false);

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            // received message  
            var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

            // handle the received message  
            HandleMessage(content);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        consumer.Shutdown += OnConsumerShutdown;
        consumer.Registered += OnConsumerRegistered;
        consumer.Unregistered += OnConsumerUnregistered;
        consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

        _channel.BasicConsume("order_processed", true, consumer);
        return Task.CompletedTask;
    }

    private void HandleMessage(string content)
    {
        // we just print this message   
        _logger.LogInformation($"consumer received {content}");

        Cart cart = JsonSerializer.Deserialize<Cart>(content);

        // string message = string.Empty;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CartContext>();
            
            if(db.Carts.Any(e => e.CartId == cart.CartId)){
                Cart cartDB = db.Carts.Find(cart.CartId);
                cartDB.OrderStatus = cart.OrderStatus;
                db.Entry(cartDB).State = EntityState.Modified;
            };

            db.SaveChanges();
        }

    }

    private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
    private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}