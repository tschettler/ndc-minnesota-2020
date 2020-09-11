using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AuthHelp;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frontend {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            services.AddControllersWithViews ();

            services.AddHttpClient<IAuthHelper, AuthHelper> ()
                .ConfigureHttpClient ((provider, client) => {
                    var config = provider.GetRequiredService<IConfiguration> ();
                    client.BaseAddress = config.GetServiceUri ("Orders");
                    client.DefaultRequestVersion = new Version (2, 0);
                });

            services.AddHttpClient ("toppings")
                .ConfigurePrimaryHttpMessageHandler (DevelopmentModeCertificateHelper.CreateClientHandler);

            services.AddGrpcClient<Orders.OrdersClient> ((provider, options) => {
                var config = provider.GetRequiredService<IConfiguration> ();
                options.Address = config.GetServiceUri ("Orders");
            }).ConfigureChannel ((provider, channel) => {
                // can also do channel.HttpClient..., but not the gRPC way of doing things
                var authHelper = provider.GetRequiredService<IAuthHelper> ();

                // gRPC specific authentication
                var credentials = CallCredentials.FromInterceptor (async (context, metadata) => {
                    // this token will expire and so will eventually not be valid anymore
                    var token = await authHelper.GetTokenAsync ();
                    metadata.Add ("Authorization", $"Bearer {token}");
                });

                channel.Credentials = ChannelCredentials.Create (new SslCredentials (), credentials);
            });

            services.AddGrpcClient<Toppings.ToppingsClient> ((provider, options) => {
                var config = provider.GetRequiredService<IConfiguration> ();
                options.Address = config.GetServiceUri ("Toppings", "https");
            }).ConfigureChannel ((provider, channel) => {
                // this doesn't work here: channel.HttpHandler = DevelopmentModeCertificateHelper.CreateClientHandler;
                channel.HttpClient = provider.GetRequiredService<IHttpClientFactory> ().CreateClient ("toppings");
                channel.DisposeHttpClient = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            } else {
                app.UseExceptionHandler ("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts ();
            }
            app.UseHttpsRedirection ();
            app.UseStaticFiles ();

            app.UseRouting ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }
}