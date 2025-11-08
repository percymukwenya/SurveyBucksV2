namespace WebApi.Models.Auth
{
    public class ExternalLoginTokenRequest
    {
        public string Provider { get; set; }
        public string AccessToken { get; set; }
    }
}
