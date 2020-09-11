using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Toppings.Data;

namespace Toppings {
    public class ToppingsService : Toppings.ToppingsBase {

        private readonly IToppingData _data;

        public ToppingsService (IToppingData data) {
            _data = data;
        }

        public override async Task<DecrementStockResponse> DecrementStock(DecrementStockRequest request, ServerCallContext context)
        {
            foreach(var id in request.ToppingIds){
                await _data.DecrementStockAsync(id);
            }
            
            return new DecrementStockResponse();
        }

        public override async Task<AvailableResponse> GetAvailable (AvailableRequest request, Grpc.Core.ServerCallContext context) {
            List<ToppingEntity> toppings;

            try {
                toppings = await _data.GetAsync ();
            } catch (Exception ex) {
                throw new RpcException (new Status(StatusCode.Internal, ex.Message));
            }

            var response = new AvailableResponse();
            foreach(var entity in toppings){
                var topping = new Topping{
                    Id = entity.Id,
                    Name = entity.Name,
                    Price = entity.Price
                };
                var availableTopping = new AvailableTopping {
                    Topping = topping,
                    Quantity = entity.StockCount
                };
                response.Toppings.Add(availableTopping);
            }

            return response;
        }
    }
}