using TranslationIntegrationService.Abstraction;

namespace TranslationConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var apiKey = "AIzaSyDnjxmV6631yoUBddKPy70o9kqpMCBmUQw";
            var redisConnectionString = "localhost";
            
            ITranslationService translationService = new GoogleTranslationService(apiKey, redisConnectionString);


            Console.WriteLine("Enter text to translate:");
            var text = Console.ReadLine();
            Console.WriteLine("Enter source language code:");
            var fromLanguage = Console.ReadLine();
            Console.WriteLine("Enter target language code:");
            var toLanguage = Console.ReadLine();

            var translation = await translationService.TranslateTextAsync(text, fromLanguage, toLanguage);
            Console.WriteLine($"Translation: {translation}");

        }
    }
}
