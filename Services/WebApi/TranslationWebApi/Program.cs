using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TranslationIntegrationService.Abstraction;
using TranslationWebApi.Services;

namespace TranslationWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Serilog configuration
            //Log.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(builder.Configuration)
            //    .WriteTo.File("Logs/app.log",
            //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            //    rollingInterval: RollingInterval.Day)
            //    .CreateLogger();

            // Configure Serilog using the settings from the appsettings.json
            builder.Host.UseSerilog((context, services, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            // Add services to the container.

            //T.Integration Dependency Injection
            builder.Services.AddSingleton<ITranslationService, BaseAPITranslateService>();

            //builder.Services.AddSingleton<ITranslationService>(provider =>
            //{
            //    var configuration = provider.GetRequiredService<IConfiguration>();
            //    var googleApiKey = configuration["GoogleApiKey"];
            //    var redisConnectionString = configuration["RedisConnectionString"];
            //    return new GoogleTranslationService(googleApiKey, redisConnectionString);
            //});

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                //app.MapGrpcService<GrpcService>();

            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
