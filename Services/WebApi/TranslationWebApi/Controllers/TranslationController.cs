using Microsoft.AspNetCore.Mvc;
using TranslationIntegrationService.Abstraction;
using TranslationWebApi.Models;

namespace TranslationWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TranslationController : Controller
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(ITranslationService translationService, ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpGet("info")]
        public IActionResult GetServiceInfo()
        {
            try
            {
                var info = _translationService.GetServiceInfo();
                _logger.LogInformation("Service Infor retrieved succesfully");
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"An error occurred while retrieving service info.");
                return StatusCode(500, "An error occurred while retrieving service info.");
            }
        }

        [HttpPost("translate")]
        public async Task<IActionResult> TranslateText([FromBody] TranslateRequestDTO request)
        {
            if (request is null)
            {
                _logger.LogWarning("TranslateText request is null");
                return BadRequest("Request cannot be null");

            }

            try
            {
                var translation = await _translationService.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage);
                _logger.LogInformation("Text translated successfully form{FromLanguage} to {ToLanguage}",request.FromLanguage,request.ToLanguage);
                return Ok(new { TranslatedText = translation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"An error occurred while translating text from {FromLanguage} to {ToLanguage}.", request.FromLanguage, request.ToLanguage);
                return StatusCode(500,"");
            }
        }
    }
}
