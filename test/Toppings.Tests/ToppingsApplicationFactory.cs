using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Toppings.Data;
using TestHelpers;
using System.Collections.Generic;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;

namespace Toppings.Tests
{
    public class ToppingsApplicationFactory : WebApplicationFactory<Startup>
    {
        public IToppingData MockToppingData {get; }

        public ToppingsApplicationFactory(){
            var list = new List<ToppingEntity>
                {
                    new ToppingEntity("cheese", "Cheese", 0.5m, 1),
                    new ToppingEntity("tomato", "Tomato", 0.5m, 1),
                };
                MockToppingData = Substitute.For<IToppingData>();
                MockToppingData.GetAsync().Returns(list);
                //MockToppingData.DecrementStockAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        }

        public Toppings.ToppingsClient CreateToppingsClient()
        {
            var client = CreateDefaultClient(new ResponseVersionHandler());

            var channel = GrpcChannel.ForAddress(client.BaseAddress, new GrpcChannelOptions
            {
                HttpClient = client
            });

            return new Toppings.ToppingsClient(channel);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                services.Remove<IToppingData>();
                services.AddSingleton(MockToppingData);
            });
        }

        private class ResponseVersionHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                response.Version = request.Version;
                return response;
            }
        }
    }
}