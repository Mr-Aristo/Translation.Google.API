using Serilog;
using TranslationIntegrationService.Abstraction;

namespace TranslationConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {       
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            ITranslationService translationService = new GoogleTranslationService();

            Log.Information("Translate connection began {translationService}", translationService);
            try
            {
                // Get Service Info
                try
                {
                    //Log
                    Log.Information("Requesting service info.");

                    var serviceInfoResponse = translationService.GetServiceInfo();
                    Console.WriteLine("Service Info: ");
                    Console.WriteLine(serviceInfoResponse);

                    //Log
                    Log.Information("Service info retrieved: {ServiceInfo}", serviceInfoResponse);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while getting service info.");
                    Console.WriteLine("An error occurred while getting service info.");
                }

                try
                {
                    Console.WriteLine("Enter text to translate:");
                    var text = Console.ReadLine();
                    Console.WriteLine("Enter source language code:");
                    var fromLanguage = Console.ReadLine();
                    Console.WriteLine("Enter target language code:");
                    var toLanguage = Console.ReadLine();

                    var translation = await translationService.TranslateTextAsync(text, fromLanguage, toLanguage);

                    if (translation is not null)
                    {

                        Console.WriteLine($"Translation: {translation}");

                    }
                    else
                    {
                        Console.WriteLine("Translation response is null");
                        Log.Error("Translation respinse is null");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while attempting translate.");
                    Console.WriteLine("An error occurred while attempting translate.");
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
