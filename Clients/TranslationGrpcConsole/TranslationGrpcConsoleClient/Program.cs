using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Serilog;
using TranslationGrpcConsoleClient;

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
                using var channel = GrpcChannel.ForAddress("http://localhost:5231");
                var client = new TranslationService.TranslationServiceClient(channel);

                //Log
                Log.Information("Connection began. Channel: {Channel}, Client: {Client}", channel, client);
                // Get Service Info
                try
                {
                    //Log
                    Log.Information("Requesting service info.");

                    var serviceInfoResponse = await client.GetServiceInfoAsync(new Google.Protobuf.WellKnownTypes.Empty());
                    Console.WriteLine("Service Info: ");
                    Console.WriteLine(serviceInfoResponse.Info);

                    //Log
                    Log.Information("Service info retrieved: {ServiceInfo}", serviceInfoResponse.Info);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while getting service info.");
                    Console.WriteLine("An error occurred while getting service info.");
                }

                // Translate Text
                try
                {
                   

                    Console.WriteLine("Enter text to translate:");
                    var text = Console.ReadLine();
                    Console.WriteLine("Enter source language code:");
                    var fromLanguage = Console.ReadLine();
                    Console.WriteLine("Enter target language code:");
                    var toLanguage = Console.ReadLine();

                    var request = new TranslateRequest
                    {
                        Text = text,
                        FromLanguage = fromLanguage,
                        ToLanguage = toLanguage
                    };

                    var response = await client.TranslateAsync(request);

                    if (response is not null)
                    {
                        Console.WriteLine($"Translation: {response.TranslatedText}");

                        //Log
                        Log.Information("Translation received: {Translation}", response.TranslatedText);
                    }
                    else
                    {
                        Console.WriteLine("Response is null");

                        //Log
                        Log.Warning("Translation response is null.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while translating text.");
                    Console.WriteLine("An error occurred while translating text.");
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
