using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http.Json;
using TranslationIntegrationService.Abstraction;
using TranslationWebApi.Services;
using WebApiClient.Model;

namespace WebApiClient
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
                .AddLogging(configure => configure.AddSerilog())
                .AddTransient<ITranslationService, BaseAPITranslateService>()
                .BuildServiceProvider();

            var translationService = serviceProvider.GetService<ITranslationService>();
            
            try
            {
                var httpClient = new HttpClient { BaseAddress = new Uri("http://192.168.81.241:60839") };

                try
                {
                    var infoResponse = await httpClient.GetAsync("Translation/info");

                    if (infoResponse.IsSuccessStatusCode)
                    {
                        var serviceInfo = await infoResponse.Content.ReadAsStringAsync();
                        Console.WriteLine("Service Info: ");
                        Console.WriteLine(serviceInfo);
                    }
                    else
                    {
                        var errorContent = await infoResponse.Content.ReadAsStringAsync();
                        Log.Error($"Error retrieving service info: {infoResponse.StatusCode} - {errorContent}");
                        Console.WriteLine("Error retrieving service info.");
                    }
                } 
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while retrieving service info.");
                    Console.WriteLine("An error occurred while retrieving service info.");
                }

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
                        try
                        {
                            var request = new
                            {
                                Text = text,
                                FromLanguage = fromLanguage,
                                ToLanguage = toLanguage
                            };

                            var translateResponse = await httpClient.PostAsJsonAsync("Translation/translate", request);
                            if (translateResponse.IsSuccessStatusCode)
                            {
                                var result = await translateResponse.Content.ReadFromJsonAsync<TranslateResponse>();
                                Console.WriteLine($"Translation: {result.TranslatedText}");
                                Log.Information("Translation received: {Translation}", result.TranslatedText);
                            }
                            else
                            {
                                var errorContent = await translateResponse.Content.ReadAsStringAsync();
                                Log.Error($"Error translating text: {translateResponse.StatusCode} - {errorContent}");
                                Console.WriteLine("Error translating text.");
                            }
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
                Log.Fatal(ex, "A fatal error occurred in the application.");
                Console.WriteLine("A fatal error occurred in the application.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
