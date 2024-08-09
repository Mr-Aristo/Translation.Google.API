using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;
using Serilog;
using Google;

public class GoogleTranslationService : ITranslationService
{
    private readonly TranslateService _translateService;
    private readonly ConnectionMultiplexer _redis;
    private readonly string _apiKey;
    private readonly string _redisConnectionString;
    private readonly string _defaultApiKey = "AIzaSyDnjxmV6631yoUBddKPy70o9kqpMCBmUQw";
    //private readonly string _defaultRedisConnectionString = "172.17.0.2:6379,abortConnect=false";
    private readonly string _defaultRedisConnectionString = "192.168.93.195:6379";//Docker/redis-tag:nanoserver
    //private readonly string _defaultApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    //private readonly string _defaultRedisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

    public GoogleTranslationService(string apiKey = null, string redisConnectionString = null)
    {
        _apiKey = apiKey ?? _defaultApiKey;
        _redisConnectionString = redisConnectionString ?? _defaultRedisConnectionString;

        _translateService = new TranslateService(new BaseClientService.Initializer
        {
            ApiKey = _apiKey
        });

        Log.Information("Attempting to connect to Redis with connection string: {RedisConnectionString}", _redisConnectionString);

        var options = ConfigurationOptions.Parse(_redisConnectionString);
        options.AbortOnConnectFail = false; // Bu satır eklendi
        _redis = ConnectionMultiplexer.Connect(options);
        //_redis = ConnectionMultiplexer.Connect(_redisConnectionString);

        if (_redis.IsConnected)
        {
            Log.Information("Connected to Redis: {RedisConnection}", _redisConnectionString);
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
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fromLanguage) || string.IsNullOrEmpty(toLanguage))
            {
                throw new ArgumentException("Text, fromLanguage, and toLanguage cannot be null or empty.");
            }

            //***** Redis caching 
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

                return cachedTranslation;
            }

            Log.Information("Cache miss for key: {CacheKey}", cacheKey);
            //******

            var request = _translateService.Translations.List(new[] { text }, toLanguage);
            request.Source = fromLanguage;

            var response = await request.ExecuteAsync();
            var translation = response.Translations.First().TranslatedText;

            await db.StringSetAsync(cacheKey, translation);
            Log.Information("Translation cached for key: {CacheKey} with value: {Translation}", cacheKey, translation);
            Console.ReadLine();
            return translation;
        }
        catch (GoogleApiException ex)
        {
            return HandleException(ex, "Google API error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage, "Google API error");
        }
        catch (RedisException ex)
        {
            return HandleException(ex, "Redis error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage, "Redis error");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "An unexpected error occurred while translating text from {FromLanguage} to {ToLanguage}.", fromLanguage, toLanguage, "unexpected error");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    string HandleException(Exception ex, string logMessage, string fromLanguage, string toLanguage, string errorType)
    {
        Log.Error(ex, logMessage, fromLanguage, toLanguage);
        throw new InvalidOperationException($"Failed to translate text due to {errorType}.", ex);
    }
}
//Windows enviromentAccess
//[System.Environment]::SetEnvironmentVariable("GOOGLE_API_KEY", "API_KEY", "User")
//[System.Environment]::SetEnvironmentVariable("REDIS_CONNECTION_STRING", "REDIS_CONNECTION_STRING", "User")
