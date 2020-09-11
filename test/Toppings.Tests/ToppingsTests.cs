using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Toppings.Tests {
    public class ToppingsTests : IClassFixture<ToppingsApplicationFactory> {
        private readonly ToppingsApplicationFactory _factory;

        public ToppingsTests (ToppingsApplicationFactory factory) {
            _factory = factory;
        }

        [Fact]
        public async Task DecrementStock () {
            dynamic client = _factory.CreateToppingsClient ();
            await client.DecrementStock (new DecrementStockRequest {
                ToppingIds = { "cheese", "tomato" }
            });

            await _factory.MockToppingData
                .Received (1)
                .DecrementStockAsync ("cheese", Arg.Any<CancellationToken> ());

            await _factory.MockToppingData
                .Received (1)
                .DecrementStockAsync ("tomato", Arg.Any<CancellationToken> ());
        }

        [Fact]
        public async Task GetList () {
            dynamic client = _factory.CreateToppingsClient ();
            var response = await client.GetAvailableAsync (new AvailableRequest ());
            Assert.Equal (2, response.Toppings.Count);
        }
    }
}