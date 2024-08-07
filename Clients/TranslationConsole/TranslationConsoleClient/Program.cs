using Serilog;
using TranslationIntegrationService.Abstraction;

namespace TranslationConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var apiKey = "AIzaSyDnjxmV6631yoUBddKPy70o9kqpMCBmUQw";
            var redisConnectionString = "localhost";
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log\\log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            ITranslationService translationService = new GoogleTranslationService(apiKey, redisConnectionString);

            Log.Information("Translate connection began {translationService}", translationService);
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
