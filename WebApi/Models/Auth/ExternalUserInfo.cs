namespace WebApi.Models.Auth
{
    public class ExternalUserInfo
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProviderId { get; set; }
    }
}
