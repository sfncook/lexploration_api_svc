namespace SalesBotApi.Models

{
    public class Company
    {
        public string id { get; set; }
        public string company_id { get; set; }
        public long _ts { get; set; }
        public string start_url { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string contact_prompt { get; set; }
        public string contact_link { get; set; }
        public bool contact_demo_app_install { get; set; }
        public bool contact_form { get; set; }
        public string greeting { get; set; }
    }
}