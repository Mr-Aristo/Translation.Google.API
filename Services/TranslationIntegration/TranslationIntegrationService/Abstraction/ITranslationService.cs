using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationIntegrationService.Abstraction
{
    public interface ITranslationService
    {
        string GetServiceInfo();
        Task<string[]> TranslateTextAsync(string[] text, string fromLanguage, string toLanguage);
    }
}
