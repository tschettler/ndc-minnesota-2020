using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Grpc.Core;

namespace Orders.ShopConsole {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;

        private readonly Orders.OrdersClient _ordersClient;

        public Worker (ILogger<Worker> logger, Orders.OrdersClient ordersClient) {
            _logger = logger;
            _ordersClient = ordersClient;
        }

        protected override async Task ExecuteAsync (CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var subscribeRequest = new SubscribeRequest ();
                    var streamingCall = _ordersClient.Subscribe (subscribeRequest, cancellationToken : stoppingToken);

                    await foreach (var response in streamingCall.ResponseStream.ReadAllAsync (stoppingToken)) {
                        var toppings = string.Join (", ", response.ToppingIds);
                        Console.WriteLine ($"{response.Time.ToDateTimeOffset():T}: {toppings}");
                        Console.WriteLine();
                    }
                } catch (System.Exception) {
                    _logger.LogInformation ("Worker is disconnected.");
                }
            }
        }
    }
}