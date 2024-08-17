using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Serilog;
using TranslationIntegrationService.Abstraction;

namespace TranslationGrpcConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Serilog configuration
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();           
            
            try
            {
                using var channel = GrpcChannel.ForAddress("http://192.168.84.79:60839");
                var client = new TranslationService.TranslationServiceClient(channel);

                // Log
                Log.Information("Connection began. Channel: {Channel}, Client: {Client}", channel, client);

                // Get Service Info
                try
                {
                    Log.Information("Requesting service info.");

                    var serviceInfoResponse = await client.GetServiceInfoAsync(new Google.Protobuf.WellKnownTypes.Empty());
                    Console.WriteLine("Service Info: ");
                    Console.WriteLine(serviceInfoResponse.Info);

                    Log.Information("Service info retrieved: {ServiceInfo}", serviceInfoResponse.Info);
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
                            var request = new TranslateRequest
                            {
                                Text = string.Join(" ", texts),
                                FromLanguage = fromLanguage,
                                ToLanguage = toLanguage
                            };

                            var response = await client.TranslateAsync(request);

                            if (response is not null)
                            {
                                Console.WriteLine($"Translation: {response.TranslatedText}");
                                Log.Information("Translation received: {Translation}", response.TranslatedText);
                            }
                            else
                            {
                                Console.WriteLine("Response is null");
                                Log.Warning("Translation response is null.");
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
