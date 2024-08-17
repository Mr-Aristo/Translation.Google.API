using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TranslationGrpcService.Services;
using TranslationIntegrationService.Abstraction;

namespace TranslationGrpcConsoleClient
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
      
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

           
            var serviceProvider = new ServiceCollection()
                .AddSingleton<GrpcChannel>(sp => GrpcChannel.ForAddress("http://192.168.84.79:60839"))
                .AddSingleton<ITranslationService, BaseGrpcTranslateService>()
                .BuildServiceProvider();

     
            var translationService = serviceProvider.GetRequiredService<ITranslationService>();

            try
            {
                // Log
                Log.Information("Connection began using gRPC channel.");

               
                try
                {
                    Log.Information("Requesting service info.");

                    var serviceInfo = translationService.GetServiceInfo();
                    Console.WriteLine("Service Info: ");
                    Console.WriteLine(serviceInfo);

                    Log.Information("Service info retrieved: {ServiceInfo}", serviceInfo);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while getting service info.");
                    Console.WriteLine("An error occurred while getting service info.");
                }

                // Translate Text
                var texts = new List<string>();

                Console.WriteLine("Enter source language code:");
                var fromLanguage = Console.ReadLine();
                Console.WriteLine("Enter target language code:");
                var toLanguage = Console.ReadLine();

                while (true)
                {
                    Console.WriteLine("Enter text to translate (or type 'exit' to quit):");
                    var text = Console.ReadLine();

                    if (text?.ToLower() == "exit")
                        break;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        texts.Add(text);
                    }

                    if (texts.Count > 0)
                    {
                        try
                        {
                            var translations = await translationService.TranslateTextAsync(texts.ToArray(), fromLanguage, toLanguage);

                            foreach (var translation in translations)
                            {
                                Console.WriteLine($"Translation: {translation}");
                                Log.Information("Translation received: {Translation}", translation);
                            }

                            texts.Clear();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An exception occurred while translating text.");
                            Console.WriteLine("An error occurred while translating text.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "A fatal exception occurred in the application.");
                Console.WriteLine("A fatal error occurred in the application.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
