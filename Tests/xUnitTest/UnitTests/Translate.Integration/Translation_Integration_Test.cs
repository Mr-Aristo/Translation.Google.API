using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Google.Apis.Services;
using Google.Apis.Translate.v2;
using Google.Apis.Translate.v2.Data;
using GoogleApi.Entities.Translate.Translate.Response;
using Moq;
using StackExchange.Redis;

namespace UnitTests.Translate.Integration
{
    public class Translation_Integration_Test
    {
        private readonly Mock<TranslateService> _mockTranslateService;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly GoogleTranslationService _translationService;

        public Translation_Integration_Test()
        {
            _mockTranslateService = new Mock<TranslateService>(MockBehavior.Strict, new BaseClientService.Initializer
            {
                ApiKey = "fakeApiKey"
            });

            _mockDatabase = new Mock<IDatabase>();
            var mockConnectionMultiplexer = new Mock<ConnectionMultiplexer>();
            mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

            _translationService = new GoogleTranslationService("fakeApiKey", "localhost");
        }


        [Fact]
        public async Task TranslateTextAsync_CacheHit_ReturnsCachedTranslation()
        {
            // Arrange
            var text = "Hello";
            var fromLanguage = "en";
            var toLanguage = "ru";
            var cacheKey = $"{fromLanguage}-{toLanguage}-{text}";
            var cachedTranslation = "Привет";

            _mockDatabase.Setup(db => db.StringGetAsync(cacheKey, CommandFlags.None)).ReturnsAsync(cachedTranslation);

            // Act
            var result = await _translationService.TranslateTextAsync(text, fromLanguage, toLanguage);

            // Assert
            Assert.Equal(cachedTranslation, result);
        }

        [Fact]
        public async Task TranslateTextAsync_CacheMiss_TranslatesAndCachesResult()
        {
            // Arrange
            var text = "Hello";
            var fromLanguage = "en";
            var toLanguage = "ru";
            var cacheKey = $"{fromLanguage}-{toLanguage}-{text}";
            var translation = "Привет";

            var translateResponse = new Google.Apis.Translate.v2.Data.TranslationsListResponse
            {
                Translations = new List<Google.Apis.Translate.v2.Data.TranslationsResource>
                    {
                        new Google.Apis.Translate.v2.Data.TranslationsResource { TranslatedText = translation }
                    }
            };

            _mockTranslateService.Setup(service => service.Translations.List(new[] { text }, toLanguage).ExecuteAsync())
                .ReturnsAsync(translateResponse);

            _mockDatabase.Setup(db => db.StringGetAsync(cacheKey, CommandFlags.None)).ReturnsAsync((string)null);
            _mockDatabase.Setup(db => db.StringSetAsync(cacheKey, translation, null, When.Always, CommandFlags.None)).ReturnsAsync(true);

            // Act
            var result = await _translationService.TranslateTextAsync(text, fromLanguage, toLanguage);

            // Assert
            Assert.Equal(translation, result);
            _mockDatabase.Verify(db => db.StringSetAsync(cacheKey, translation, null, When.Always, CommandFlags.None), Times.Once);
        }

        [Fact]
        public void GetServiceInfo_ReturnsServiceInfo()
        {
            // Act
            var result = _translationService.GetServiceInfo();

            // Assert
            Assert.Contains("Google Translate API", result);
            Assert.Contains("Redis", result);
        }
    }
}