namespace SalesBotApi.Models

{
    public class Conversation
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public string company_id { get; set; }
        public bool user_wants_to_be_contacted { get; set; }
        public bool user_wants_to_install_the_demo { get; set; }
        public string user_first_name { get; set; }
        public string user_last_name { get; set; }
        public string user_email { get; set; }
        public string user_phone_number { get; set; }
        public long _ts { get; set; }
    }
}