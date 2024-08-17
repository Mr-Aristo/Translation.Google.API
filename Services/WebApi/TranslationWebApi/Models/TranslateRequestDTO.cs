namespace TranslationWebApi.Models
{
    public class TranslateRequestDTO
    {
        public string[] Text { get; set; }  
        public string FromLanguage { get; set; }
        public string ToLanguage { get; set; }
    }
}
