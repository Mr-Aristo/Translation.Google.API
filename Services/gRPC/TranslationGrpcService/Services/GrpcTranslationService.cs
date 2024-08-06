using Grpc.Core;
using TranslationIntegrationService.Abstraction;


namespace TranslationGrpcService.Services
{
    public class GrpcTranslationService : TranslationService.TranslationServiceBase
    {
        private readonly ITranslationService _translationService;

        public GrpcTranslationService(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        public override async Task<TranslateResponse> Translate(TranslateRequest request, ServerCallContext context)
        {
            var translation = await _translationService.TranslateTextAsync(request.Text, request.FromLanguage, request.ToLanguage);
            return new TranslateResponse { TranslatedText = translation };
        }

        public override Task<ServiceInfoResponse> GetServiceInfo(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var info = _translationService.GetServiceInfo();
            return Task.FromResult(new ServiceInfoResponse { Info = info });
        }
    }
}
