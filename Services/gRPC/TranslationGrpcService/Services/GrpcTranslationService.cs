﻿using Grpc.Core;
using TranslationIntegrationService.Abstraction;


namespace TranslationGrpcService.Services
{
    public class GrpcTranslationService : TranslationService.TranslationServiceBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger _logger;

        public GrpcTranslationService(ITranslationService translationService, ILogger logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        public override async Task<TranslateResponse> Translate(TranslateRequest request, ServerCallContext context)
        {
            //Log
            _logger.LogInformation("Translate method called with text: {Text}, from: {FromLanguage}, to: {ToLanguage}",
                request.Text, request.FromLanguage, request.ToLanguage);

            try
            {
                var translationArray = await _translationService.TranslateTextAsync(new[] { request.Text }, request.FromLanguage, request.ToLanguage);
                var translation = translationArray.FirstOrDefault();

                //Log
                _logger.LogInformation("Translation successful for text: {Text}, from: {FromLanguage}, to: {ToLanguage}",
                   request.Text, request.FromLanguage, request.ToLanguage);

                return new TranslateResponse { TranslatedText = translation };
            }
            catch (Exception ex)
            {   
                //Log
                _logger.LogError(ex, "Error occurred while translating text: {Text}, from: {FromLanguage}, to: {ToLanguage}",
                    request.Text, request.FromLanguage, request.ToLanguage);

                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while translating text."));
            }
        }

        public override Task<ServiceInfoResponse> GetServiceInfo(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            //Log
            _logger.LogInformation("GetServiceInfo method called.");

            try
            {
                var info = _translationService.GetServiceInfo();

                //Log
                _logger.LogInformation("Service info retrieved successfully.");

                return Task.FromResult(new ServiceInfoResponse { Info = info });
            }
            catch (Exception ex)
            {
                //Log
                _logger.LogError(ex, "Error occurred while getting Service info: {request},: {context}",
                    request, context);

                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while getting Service Info."));
            }
        }
    }
}
