﻿using Google.Apis.Services;
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
                if (texts == null || texts.Length == 0 || string.IsNullOrEmpty(fromLanguage) || string.IsNullOrEmpty(toLanguage))
                {
                    throw new ArgumentException("Texts, fromLanguage, and toLanguage cannot be null or empty.");
                }

                var db = _redis.GetDatabase();
                if (db == null)
                {
                    Log.Error("Failed to get Redis database.");
                    throw new InvalidOperationException("Failed to get Redis database.");
                }

                // Create cache keys for all texts
                var cacheKeys = texts.Select(text => $"{fromLanguage}-{toLanguage}-{text}").ToArray();

                Log.Information("Attempting to get cached translations for keys: {CacheKeys}", cacheKeys);

                // Get cached translations in bulk
                var cachedTranslations = await db.StringGetAsync(cacheKeys.Select(k => (RedisKey)k).ToArray());

                var results = new List<string>();
                var textsToTranslate = new List<string>();
                var missingIndexes = new List<int>();

                for (int i = 0; i < cachedTranslations.Length; i++)
                {
                    if (cachedTranslations[i].HasValue)
                    {
                        Log.Information("Cache hit for key: {CacheKey}", cacheKeys[i]);
                        results.Add(cachedTranslations[i]);
                    }
                    else
                    {
                        Log.Information("Cache miss for key: {CacheKey}", cacheKeys[i]);
                        textsToTranslate.Add(texts[i]);
                        missingIndexes.Add(i);
                    }
                }

                // If there are texts that need to be translated
                if (textsToTranslate.Count > 0)
                {
                    Log.Information("Translating {Count} texts with Google API.", textsToTranslate.Count);
                    var translations = await TranslateText(textsToTranslate.ToArray(), fromLanguage, toLanguage);

                    // Store translations in Redis and add them to results
                    for (int i = 0; i < translations.Length; i++)
                    {
                        var cacheKey = cacheKeys[missingIndexes[i]];
                        await db.StringSetAsync(cacheKey, translations[i]);
                        Log.Information("Translation cached for key: {CacheKey} with value: {Translation}", cacheKey, translations[i]);
                        results.Insert(missingIndexes[i], translations[i]);
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

        private async Task<string[]> TranslateText(string[] texts, string fromLanguage, string toLanguage)
        {
            try
            {
                var request = _translateService.Translations.List(texts, toLanguage);
                request.Source = fromLanguage;

                var response = await request.ExecuteAsync();
                return response.Translations.Select(t => t.TranslatedText).ToArray();
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
