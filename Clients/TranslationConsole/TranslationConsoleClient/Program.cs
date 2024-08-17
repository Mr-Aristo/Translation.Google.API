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

            Log.Information("Translate connection began {TranslationService}", translationService);
            try
            {
                // Get Service Info
                try
                {
                    Log.Information("Requesting service info.");

                    var serviceInfoResponse = translationService.GetServiceInfo();
                    Console.WriteLine("Service Info: ");
                    Console.WriteLine(serviceInfoResponse);

                    Log.Information("Service info retrieved: {ServiceInfo}", serviceInfoResponse);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occurred while getting service info.");
                    Console.WriteLine("An error occurred while getting service info.");
                }

                // Multiple translation loop
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
                            var translation = await translationService.TranslateTextAsync(new[] { text }, fromLanguage, toLanguage);

                            if (translation != null && translation.Length > 0)
                            {
                                Console.WriteLine($"Translation: {translation[0]}");
                                Log.Information("Translation received: {Translation}", translation[0]);
                            }
                            else
                            {
                                Console.WriteLine("Translation response is null");
                                Log.Error("Translation response is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An exception occurred while attempting translate.");
                            Console.WriteLine("An error occurred while attempting translate.");
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
