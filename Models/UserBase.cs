namespace SalesBotApi.Models

{
    public abstract class UserBase
    {
        public string id { get; set; }
        public string user_name { get; set; }
        public string company_id { get; set; }
        public string role { get; set; }
    }
}