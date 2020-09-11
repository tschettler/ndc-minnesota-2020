using System.Linq;
using System.Threading.Tasks;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Frontend.Controllers
{
    [Route("orders")]
    public class OrdersController : Controller
    {
        private readonly ILogger<OrdersController> _log;
        private Orders.OrdersClient _client;

        public OrdersController(ILogger<OrdersController> log, Orders.OrdersClient client)
        {
            _client = client;
            _log = log;
        }

        [HttpPost]
        public async Task<IActionResult> Order([FromForm]HomeViewModel viewModel)
        {
            var ids = viewModel.Toppings.Where(t => t.Selected)
            .Select(t => t.Id)
            .ToArray();

            _log.LogInformation("Order received: {Toppings}", string.Join(',', ids));

            var request = new PlaceOrderRequest();
            request.ToppingIds.AddRange(ids);
            await _client.PlaceOrderAsync(request);
            
            return View();
        }
    }
}