using Grpc.Net.Client;

namespace TranslationGrpcConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5231");
            var client = new TranslationService.TranslationServiceClient(channel);
            

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
            Console.WriteLine($"Translation: {response.TranslatedText}");
        }
    }
}
