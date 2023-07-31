using System;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Extensions.DependencyInjection;

namespace HttpClientUsageScenarios.NetFramework.WebApi
{
    public class AutofacWebapiConfig
    {
        public static IContainer Container;

        public static void Initialize(HttpConfiguration config)
        {
            Initialize(config, RegisterServices(new ContainerBuilder()));
        }

        public static void Initialize(HttpConfiguration config, IContainer container)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static IContainer RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            
            builder.Register(ctx =>
            {
                var services = new ServiceCollection();
                services.AddHttpClient("typicode", c =>
                {
                    c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
                    c.Timeout = TimeSpan.FromSeconds(15);
                    c.DefaultRequestHeaders.Add(
                        "accept", "application/json");
                })
                    .SetHandlerLifetime(TimeSpan.FromSeconds(15));

                var provider = services.BuildServiceProvider();
                return provider.GetRequiredService<IHttpClientFactory>();

            }).SingleInstance();

            Container = builder.Build();

            return Container;
        }

    }
}