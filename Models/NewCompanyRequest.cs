namespace SalesBotApi.Models

{
    public class NewCompanyRequest
    {
        public string name { get; set; }
        public string description { get; set; }
        public string email_for_leads { get; set; }

        // TODO: Remove this once real user auth is in place
        public string user_id { get; set; }
    }
}