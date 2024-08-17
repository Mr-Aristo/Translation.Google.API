using TranslationIntegrationService.Abstraction;

namespace TranslationGrpcService.Services
{
    public class Test : ITranslationService
    {
        public string GetServiceInfo()
        {
            throw new NotImplementedException();
        }

        public Task<string[]> TranslateTextAsync(string[] text, string fromLanguage, string toLanguage)
        {
            throw new NotImplementedException();
        }
    }
}
