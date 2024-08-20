using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;

namespace TranslationIntegrationService.Services
{
    public class CachingTranslationService
    {
        private readonly ITranslationService _innerTranslationService;
        private readonly ConnectionMultiplexer _redis;

        public CachingTranslationService(ITranslationService innerTranslationService, string redisConnectionString)
        {
            _innerTranslationService = innerTranslationService;
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            _redis = ConnectionMultiplexer.Connect(options);

            if (!_redis.IsConnected)
            {
                throw new InvalidOperationException("Failed to connect to Redis.");
            }
        }

        public string GetServiceInfo()
        {
            var redisStatus = _redis.IsConnected ? "Connected" : "Disconnected";
            var info = new
            {
                Service = "Caching Translation Service",
                CacheType = "Redis",
                CacheStatus = redisStatus,
                InnerServiceInfo = _innerTranslationService.GetServiceInfo()
            };
            return JsonConvert.SerializeObject(info, Formatting.Indented);
        }

        public async Task<string[]> TranslateTextAsync(string[] texts, string fromLanguage, string toLanguage)
        {
            var db = _redis.GetDatabase();
            var cacheKeys = texts.Select(text => $"{fromLanguage}-{toLanguage}-{text}").ToArray();
            var cachedTranslations = await db.StringGetAsync(cacheKeys.Select(k => (RedisKey)k).ToArray());

            var results = new List<string>();
            var textsToTranslate = new List<string>();
            var missingIndexes = new List<int>();

            for (int i = 0; i < cachedTranslations.Length; i++)
            {
                if (cachedTranslations[i].HasValue)
                {
                    results.Add(cachedTranslations[i]);
                }
                else
                {
                    textsToTranslate.Add(texts[i]);
                    missingIndexes.Add(i);
                }
            }

            if (textsToTranslate.Count > 0)
            {
                var translations = await _innerTranslationService.TranslateTextAsync(textsToTranslate.ToArray(), fromLanguage, toLanguage);

                for (int i = 0; i < translations.Length; i++)
                {
                    var cacheKey = cacheKeys[missingIndexes[i]];
                    await db.StringSetAsync(cacheKey, translations[i]);
                    results.Insert(missingIndexes[i], translations[i]);
                }
            }

            return results.ToArray();
        }
    }
}
