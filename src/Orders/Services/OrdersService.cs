using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Orders.PubSub;

namespace Orders {
    public class OrdersService : Orders.OrdersBase {
        private readonly Toppings.ToppingsClient _toppingsClient;
        private IOrderPublisher _orderPublisher;

        private readonly ILogger<OrdersService> _logger;

        private IOrderMessages _orderMessages;

        public OrdersService (ILogger<OrdersService> logger,
            IOrderMessages orderMessages,
            IOrderPublisher orderPublisher,
            Toppings.ToppingsClient toppingsClient
        ) {
            _logger = logger;
            _orderMessages = orderMessages;
            _orderPublisher = orderPublisher;
            _toppingsClient = toppingsClient;
        }

        public override async Task Subscribe (SubscribeRequest request, Grpc.Core.IServerStreamWriter<SubscribeResponse> responseStream, Grpc.Core.ServerCallContext context) {
            var cancellationToken = context.CancellationToken;
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    var orderMessage = await _orderMessages.ReadAsync (cancellationToken);
                    var response = new SubscribeResponse {
                        Time = orderMessage.Time.ToTimestamp ()
                    };
                    response.ToppingIds.AddRange (orderMessage.ToppingIds);
                    await responseStream.WriteAsync (response);
                } catch (OperationCanceledException) {
                    _logger.LogInformation ("Subscriber is disconnected.");
                }
            }
        }

        [Authorize]
        public override async Task<PlaceOrderResponse> PlaceOrder (PlaceOrderRequest request, Grpc.Core.ServerCallContext context) {
            var user = context.GetHttpContext ().User;
            _logger.LogInformation($"{nameof(PlaceOrder)} request from {user.FindFirst(ClaimTypes.Name).Value}");

            var time = DateTimeOffset.UtcNow;

            await _orderPublisher.PublishOrder (request.ToppingIds, time);

            var decrementRequest = new DecrementStockRequest ();
            decrementRequest.ToppingIds.AddRange (request.ToppingIds);
            await _toppingsClient.DecrementStockAsync (decrementRequest);
            var response = new PlaceOrderResponse {
                Time = DateTimeOffset.UtcNow.ToTimestamp ()
            };

            return response;
        }
    }
}