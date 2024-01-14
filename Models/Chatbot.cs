namespace SalesBotApi.Models

{
    public class Chatbot
    {
        public string id { get; set; }
        public string company_id { get; set; }
        public bool show_avatar { get; set; }
        public string llm_model { get; set; }
    }
}