using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Google;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using TranslationIntegrationService.Abstraction;

namespace TranslationWebApi.Services
{
    public class BaseAPITranslateService : ITranslationService
    {
        private readonly TranslateService _translateService;
        private readonly ConnectionMultiplexer _redis;
        private readonly string _defaultApiKey = "AIzaSyDnjxmV6631yoUBddKPy70o9kqpMCBmUQw";
        private readonly string _defaultRedisConnectionString = "192.168.93.195:6379";

        public BaseAPITranslateService()
        {

            _translateService = new TranslateService(new BaseClientService.Initializer
            {
                ApiKey = _defaultApiKey
            });

            Log.Information("Attempting to connect to Redis with connection string: {RedisConnectionString}", _defaultRedisConnectionString);

            var options = ConfigurationOptions.Parse(_defaultRedisConnectionString);
            options.AbortOnConnectFail = false;
            _redis = ConnectionMultiplexer.Connect(options);

            if (_redis.IsConnected)
            {
                Log.Information("Connected to Redis: {RedisConnection}", _defaultRedisConnectionString);
            }
            else
            {
                Log.Error("Failed to connect to Redis.");
                throw new InvalidOperationException("Failed to connect to Redis.");
            }
        }

        public string GetServiceInfo()
        {
            try
            {
                var redisStatus = _redis.IsConnected ? "Connected" : "Disconnected";

                var info = new
                {
                    Service = "Google Translate API",
                    CacheType = "Redis",
                    CacheStatus = redisStatus,
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

        public async Task<string[]> TranslateTextAsync(string[] texts, string fromLanguage, string toLanguage)
        {
            try
            {
                var results = new List<string>();

                if (texts == null || texts.Length == 0 || string.IsNullOrEmpty(fromLanguage) || string.IsNullOrEmpty(toLanguage))
                {
                    throw new ArgumentException("Texts, fromLanguage, and toLanguage cannot be null or empty.");
                }

                foreach (var text in texts)
                {
                    var cacheKey = $"{fromLanguage}-{toLanguage}-{text}";

                    Log.Information("Attempting to get Redis database");
                    var db = _redis.GetDatabase();
                    if (db == null)
                    {
                        Log.Error("Failed to get Redis database.");
                        throw new InvalidOperationException("Failed to get Redis database.");
                    }

                    Log.Information("Attempting to get cached translation for key: {CacheKey}", cacheKey);
                    var cachedTranslation = await db.StringGetAsync(cacheKey);

                    if (!string.IsNullOrEmpty(cachedTranslation))
                    {
                        Log.Information("Cache hit for key: {CacheKey}", cacheKey);
                        results.Add(cachedTranslation);
                    }
                    else
                    {
                        Log.Information("Cache miss for key: {CacheKey}", cacheKey);

                        var translation = await TranslateText(text, fromLanguage, toLanguage);

                        await db.StringSetAsync(cacheKey, translation);
                        Log.Information("Translation cached for key: {CacheKey} with value: {Translation}", cacheKey, translation);

                        results.Add(translation);
                    }
                }

                return results.ToArray();
            }
            catch (GoogleApiException ex)
            {
                Log.Error(ex, "Google API error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage);
                return HandleExceptionArray(ex, "Google API error occurred.");
            }
            catch (RedisException ex)
            {
                Log.Error(ex, "Redis error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage);
                return HandleExceptionArray(ex, "Redis error occurred.");
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

        private async Task<string> TranslateText(string text, string fromLanguage, string toLanguage)
        {
            try
            {
                var request = _translateService.Translations.List(new[] { text }, toLanguage);
                request.Source = fromLanguage;

                var response = await request.ExecuteAsync();
                return response.Translations.First().TranslatedText;
            }
            catch (GoogleApiException ex)
            {
                Log.Error(ex, "Google API error occurred during translation.");
                throw new InvalidOperationException("Failed to translate text using Google API.", ex);
            }
        }

        private string[] HandleExceptionArray(Exception ex, string errorType)
        {
            Log.Error(ex, errorType);
            return new[] { $"Error: {errorType}" };
        }
    }
}

