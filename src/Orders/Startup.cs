using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthHelp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.PubSub;

namespace Orders {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices (IServiceCollection services) {
            // optimization
            // services.AddSingleton<OrdersService>();
            services.AddHttpClient ("toppings")
                .ConfigurePrimaryHttpMessageHandler (DevelopmentModeCertificateHelper.CreateClientHandler);

            services.AddGrpcClient<Toppings.ToppingsClient> ((provider, options) => {
                var config = provider.GetRequiredService<IConfiguration> ();
                options.Address = config.GetServiceUri ("Toppings", "https");
            }).ConfigureChannel ((provider, channel) => {
                // this doesn't work here: channel.HttpHandler = DevelopmentModeCertificateHelper.CreateClientHandler;
                channel.HttpClient = provider.GetRequiredService<IHttpClientFactory> ().CreateClient ("toppings");
                channel.DisposeHttpClient = true;
            });

            services.AddOrderPubSub ();
            services.AddGrpc ();

            services.AddAuthentication (JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer (options => {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateActor = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = JwtHelper.SecurityKey
                    };
                });

            services.AddAuthorization (options => {
                options.AddPolicy (JwtBearerDefaults.AuthenticationScheme, policy => {
                    policy.AddAuthenticationSchemes (JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim (ClaimTypes.Name);
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }

            app.UseRouting ();

            app.UseAuthentication ();
            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapGrpcService<OrdersService> ();

                endpoints.Map ("/generateJwtToken", context => context.Response.WriteAsync (JwtHelper.GenerateJwtToken (context.Request.Query["name"])));

                endpoints.MapGet ("/", async context => {
                    await context.Response.WriteAsync ("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}