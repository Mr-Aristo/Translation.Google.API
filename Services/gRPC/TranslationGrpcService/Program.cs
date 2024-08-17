using Microsoft.Extensions.Configuration;
using Serilog;
using TranslationGrpcService.Services;
using TranslationIntegrationService.Abstraction;

namespace TranslationGrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Configure Serilog using the settings from the appsettings.json
            builder.Host.UseSerilog((context, services, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            builder.Services.AddGrpc();

            //T.Integration Dependency Injection
            builder.Services.AddSingleton<ITranslationService, BaseGrpcTranslateService>();
            //builder.Services.AddSingleton<ITranslationService>(provider =>
            //{
            //    var configuration = provider.GetRequiredService<IConfiguration>();
            //    var googleApiKey = configuration["GoogleApiKey"];
            //    var redisConnectionString = configuration["RedisConnectionString"];
            //    return new GoogleTranslationService(googleApiKey, redisConnectionString);
            //});

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<GrpcService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}