using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;
using Serilog;

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

        Log.Information("GoogleTranslationService initialized with API Key: {ApiKey} and Redis Connection: {RedisConnection}",
            _apiKey.Substring(0, 4) + "****", redisConnectionString);
    }

    public string GetServiceInfo()
    {
        try
        {         
            var redisStatus = _redis.IsConnected ? "Connected" : "Disconnected";
            var info = new
            {
                Service = "Google Translate API",
                ApiKeyUsed = _apiKey.Substring(0, 4) + "****",
                CacheType = "Redis",
                CacheStatus = redisStatus
            };

            var serviceInfo = JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented);
            Log.Information("Service Info: {ServiceInfo}", serviceInfo);

            return serviceInfo;
        }
        catch (Exception ex)
        {

            Log.Error(ex, "An error occurred while getting service info.");
            throw new InvalidOperationException("Failed to get service info.", ex);
        }
    }

    public async Task<string> TranslateTextAsync(string text, string fromLanguage, string toLanguage)
    {
        try
        {
            var cacheKey = $"{fromLanguage}-{toLanguage}-{text}";
            var db = _redis.GetDatabase();
            var cachedTranslation = await db.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedTranslation))
            {
                Log.Information("Cache hit for key: {CacheKey}", cacheKey);
                return cachedTranslation;
            }

            Log.Information("Cache miss for key: {CacheKey}", cacheKey);

            var request = _translateService.Translations.List(new[] { text }, toLanguage);
            request.Source = fromLanguage;
            var response = await request.ExecuteAsync();
            var translation = response.Translations.First().TranslatedText;

            await db.StringSetAsync(cacheKey, translation);
            Log.Information("Translation cached for key: {CacheKey} with value: {Translation}", cacheKey, translation);

            return translation;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage);
            throw new InvalidOperationException("Failed to translate text.", ex);
        }
    }
}
