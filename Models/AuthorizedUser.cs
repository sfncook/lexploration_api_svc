namespace SalesBotApi.Models

{
    public class AuthorizedUser
    {
        public string id { get; set; }
        public string user_name { get; set; }
        public string company_id { get; set; }
        public string jwt { get; set; }
    }
}