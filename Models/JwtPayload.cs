namespace SalesBotApi.Models

{
    public class JwtPayload
    {
        public string user_id { get; set; }
        public string user_name { get; set; }
        public string company_id { get; set; }
        public string role { get; set; }
        public long exp { get; set; }
    }
}