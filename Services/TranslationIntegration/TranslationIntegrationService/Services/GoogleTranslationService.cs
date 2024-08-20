using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;

namespace TranslationIntegrationService.Services
{
    internal class GoogleTranslationService : ITranslationService
    {
        private readonly TranslateService _translateService;
        private string apiKey = "AIzaSyDnjxmV6631yoUBddKPy70o9kqpMCBmUQw";
        public GoogleTranslationService()
        {
            _translateService = new TranslateService(new BaseClientService.Initializer
            {
                ApiKey = apiKey
            });
        }

        public string GetServiceInfo()
        {
            var info = new
            {
                Service = "Google Translate API",
                ApiKey = "Hidden" 
            };
            return JsonConvert.SerializeObject(info, Formatting.Indented);
        }

        public async Task<string[]> TranslateTextAsync(string[] texts, string fromLanguage, string toLanguage)
        {
            var request = _translateService.Translations.List(texts, toLanguage);
            request.Source = fromLanguage;

            var response = await request.ExecuteAsync();
            return response.Translations.Select(t => t.TranslatedText).ToArray();
        }
    }
}
