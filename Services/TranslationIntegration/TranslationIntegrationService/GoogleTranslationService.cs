using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TranslationIntegrationService.Abstraction;

public class GoogleTranslationService : ITranslationService
{
    private readonly TranslateService _translateService;
    private readonly ConnectionMultiplexer _redis;
    private readonly string _apiKey;

    public GoogleTranslationService(string apiKey, string redisConnectionString)
    {
        _apiKey = apiKey;
        _translateService = new TranslateService(new BaseClientService.Initializer
        {
            ApiKey = apiKey
        });
        _redis = ConnectionMultiplexer.Connect(redisConnectionString);
    }

    public string GetServiceInfo()
    {
        var redisStatus = _redis.IsConnected ? "Connected" : "Disconnected";
        var info = new
        {
            Service = "Google Translate API",
            ApiKeyUsed = _apiKey.Substring(0, 4) + "****",
            CacheType = "Redis",
            CacheStatus = redisStatus
        };

        return JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented);
    }

    public async Task<string> TranslateTextAsync(string text, string fromLanguage, string toLanguage)
    {
        var cacheKey = $"{fromLanguage}-{toLanguage}-{text}";
        var db = _redis.GetDatabase();
        var cachedTranslation = await db.StringGetAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedTranslation))
        {
            return cachedTranslation;
        }

        var request = _translateService.Translations.List(new[] { text }, toLanguage);
        request.Source = fromLanguage;
        var response = await request.ExecuteAsync();
        var translation = response.Translations.First().TranslatedText;

        await db.StringSetAsync(cacheKey, translation);
        return translation;
    }
}
