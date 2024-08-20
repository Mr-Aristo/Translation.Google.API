using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;

namespace TranslationIntegrationService.Services
{
    public class BaseTranslateService : ITranslationService
    {
        private readonly ITranslationService _translationService;

        public BaseTranslateService()
        {
            var googleTranslateService = new GoogleTranslationService();
            _translationService = (ITranslationService)new CachingTranslationService(googleTranslateService, "192.168.93.195:6379");
        }

        public string GetServiceInfo()
        {
            try
            {
                var info = _translationService.GetServiceInfo();
                Log.Information("Service Info: {ServiceInfo}", info);
                return info;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while getting service info.");
                return HandleExceptionArray(ex, "Failed to get service info.").First();
            }
        }

        public async Task<string[]> TranslateTextAsync(string[] texts, string fromLanguage, string toLanguage)
        {
            try
            {
                return await _translationService.TranslateTextAsync(texts, fromLanguage, toLanguage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage);
                return HandleExceptionArray(ex, "Unexpected error.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private string[] HandleExceptionArray(Exception ex, string errorType)
        {
            Log.Error(ex, errorType);
            return new[] { $"Error: {errorType}" };
        }

    }
}
