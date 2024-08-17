using Moq;
using Grpc.Core;
using Xunit;
using TranslationIntegrationService.Abstraction;
using TranslationGrpcService.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TranslationGrpcService.Tests
{
    public class GrpcTranslationServiceTests
    {
        private readonly Mock<ITranslationService> _mockTranslationService;
        private readonly Mock<ILogger<GrpcService>> _mockLogger;
        private readonly GrpcService _service;

        public GrpcTranslationServiceTests()
        {
            _mockTranslationService = new Mock<ITranslationService>();
            _mockLogger = new Mock<ILogger<GrpcService>>();
            _service = new GrpcService(_mockTranslationService.Object, _mockLogger.Object);
        }

        private ServerCallContext CreateMockServerCallContext()
        {
            return Mock.Of<ServerCallContext>();
        }

        [Fact]
        public async Task Translate_Success_ReturnsTranslatedText()
        {
            // Arrange
            var request = new TranslateRequest
            {
                Text = "Hello",
                FromLanguage = "en",
                ToLanguage = "ru"
            };
            var expectedTranslation = "Привет";
            _mockTranslationService.Setup(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage))
                                   .ReturnsAsync(expectedTranslation);
            var context = CreateMockServerCallContext();

            // Act
            var response = await _service.Translate(request, context);

            // Assert
            Assert.Equal(expectedTranslation, response.TranslatedText);
            _mockTranslationService.Verify(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Translate_Error_ThrowsRpcException()
        {
            // Arrange
            var request = new TranslateRequest
            {
                Text = "Hello",
                FromLanguage = "en",
                ToLanguage = "es"
            };
            _mockTranslationService.Setup(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage))
                                   .ThrowsAsync(new System.Exception("Translation error"));
            var context = CreateMockServerCallContext();

            // Act & Assert
            await Assert.ThrowsAsync<RpcException>(() => _service.Translate(request, context));
            _mockTranslationService.Verify(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetServiceInfo_Success_ReturnsServiceInfo()
        {
            // Arrange
            var expectedInfo = "Service info";
            _mockTranslationService.Setup(ts => ts.GetServiceInfo())
                                   .Returns(expectedInfo);
            var context = CreateMockServerCallContext();

            // Act
            var response = await _service.GetServiceInfo(new Google.Protobuf.WellKnownTypes.Empty(), context);

            // Assert
            Assert.Equal(expectedInfo, response.Info);
            _mockTranslationService.Verify(ts => ts.GetServiceInfo(), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetServiceInfo_Error_ThrowsRpcException()
        {
            // Arrange
            _mockTranslationService.Setup(ts => ts.GetServiceInfo())
                                   .Throws(new System.Exception("Service info error"));
            var context = CreateMockServerCallContext();

            // Act & Assert
            await Assert.ThrowsAsync<RpcException>(() => _service.GetServiceInfo(new Google.Protobuf.WellKnownTypes.Empty(), context));
            _mockTranslationService.Verify(ts => ts.GetServiceInfo(), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }
    }
}
