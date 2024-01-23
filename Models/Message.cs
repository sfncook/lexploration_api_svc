namespace SalesBotApi.Models

{
    public class Message
    {
        public string id { get; set; }
        public long _ts { get; set; }
        public string conversation_id { get; set; }
        public string user_msg { get; set; }
        public string assistant_response { get; set; }
        public string company_id { get; set; }
        public bool user_wants_to_be_contacted { get; set; }
        public bool user_wants_to_install_the_demo { get; set; }
        public bool user_wants_to_schedule_call_with_sales_rep { get; set; }
        public string user_first_name { get; set; }
        public string user_last_name { get; set; }
        public string user_email { get; set; }
        public string user_phone_number { get; set; }
        public string redirect_url { get; set; }
    }
}
