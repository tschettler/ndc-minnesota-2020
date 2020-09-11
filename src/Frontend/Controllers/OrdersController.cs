using System.Linq;
using System.Threading.Tasks;
using Frontend.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Frontend.Controllers {
    [Route ("orders")]
    public class OrdersController : Controller {
        private readonly ILogger<OrdersController> _log;

        private readonly Orders.OrdersClient _client;

        //private readonly IAuthHelper _authHelper;

        public OrdersController (ILogger<OrdersController> log, Orders.OrdersClient client/*, IAuthHelper authHelper*/) {
            _client = client;
            _log = log;
           // _authHelper = authHelper;
        }

        [HttpPost]
        public async Task<IActionResult> Order ([FromForm] HomeViewModel viewModel) {
            var ids = viewModel.Toppings.Where (t => t.Selected)
                .Select (t => t.Id)
                .ToArray ();

            _log.LogInformation ("Order received: {Toppings}", string.Join (',', ids));

            //var token = await _authHelper.GetTokenAsync ();
            //var metadata = new Metadata ();
            //metadata.Add ("Authorization", $"Bearer {token}");

            var request = new PlaceOrderRequest ();
            request.ToppingIds.AddRange (ids);
            await _client.PlaceOrderAsync (request/*, metadata*/);

            return View ();
        }
    }
}