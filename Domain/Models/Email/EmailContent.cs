namespace Domain.Models.Email
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string PlainTextBody { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
    }
}
