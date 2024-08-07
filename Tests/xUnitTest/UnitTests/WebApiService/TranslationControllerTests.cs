using global::TranslationWebApi.Controllers;
using global::TranslationWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using System;
using System.Threading.Tasks;
using TranslationIntegrationService.Abstraction;
using TranslationWebApi.Controllers;
using TranslationWebApi.Models;
using Xunit;

namespace UnitTests.WebApiService
{
    public class TranslationControllerTests
    {
        private readonly Mock<ITranslationService> _mockTranslationService;
        private readonly Mock<ILogger<TranslationController>> _mockLogger;
        private readonly TranslationController _controller;

        public TranslationControllerTests()
        {
            _mockTranslationService = new Mock<ITranslationService>();
            _mockLogger = new Mock<ILogger<TranslationController>>();
            _controller = new TranslationController(_mockTranslationService.Object, _mockLogger.Object);
        }

        [Fact]
        public void GetServiceInfo_Success_ReturnsOk()
        {
            // Arrange
            var expectedInfo = "Service info";
            _mockTranslationService.Setup(ts => ts.GetServiceInfo())
                                   .Returns(expectedInfo);

            // Act
            var result = _controller.GetServiceInfo();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedInfo, okResult.Value);

            //System.NotSupportedException : Unsupported expression: ERROR!!!
            //_mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), "An error occurred while retrieving service info."), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Service Infor retrieved succesfully"),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public void GetServiceInfo_Error_ReturnsInternalServerError()
        {
            // Arrange
            _mockTranslationService.Setup(ts => ts.GetServiceInfo())
                                   .Throws(new Exception("Service info error"));

            // Act
            var result = _controller.GetServiceInfo();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving service info.", statusCodeResult.Value);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while retrieving service info.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task TranslateText_Success_ReturnsOk()
        {
            // Arrange
            var request = new TranslateRequestDTO
            {
                Text = "Hello",
                FromLanguage = "en",
                ToLanguage = "es"
            };
            var expectedTranslation = "Hola";
            _mockTranslationService.Setup(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage))
                                   .ReturnsAsync(expectedTranslation);

            // Act
            var result = await _controller.TranslateText(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(new { TranslatedText = expectedTranslation }, okResult.Value);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Text translated successfully form{request.FromLanguage} to {request.ToLanguage}")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task TranslateText_RequestIsNull_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.TranslateText(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Request cannot be null", badRequestResult.Value);
            _mockLogger.Verify(l => l.LogWarning("TranslateText request is null"), Times.Once);
        }

        [Fact]
        public async Task TranslateText_Error_ReturnsInternalServerError()
        {
            // Arrange
            var request = new TranslateRequestDTO
            {
                Text = "Hello",
                FromLanguage = "en",
                ToLanguage = "es"
            };
            _mockTranslationService.Setup(ts => ts.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage))
                                   .ThrowsAsync(new Exception("Translation error"));

            // Act
            var result = await _controller.TranslateText(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while translating text from en to es.", statusCodeResult.Value);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"An error occurred while translating text from {request.FromLanguage} to {request.ToLanguage}")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }
    }
}
