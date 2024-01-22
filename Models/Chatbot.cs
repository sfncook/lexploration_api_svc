namespace SalesBotApi.Models
{
    public class RedirectPrompts
    {
        public string prompt { get; set; }
        public string url { get; set; }
    }
    public class AnsweredQuestion
    {
        public string question { get; set; }
        public string answer { get; set; }
    }

    public class Chatbot
    {
        public string id { get; set; }
        public string company_id { get; set; }
        public string avatar_view { get; set; }
        public string llm_model { get; set; }
        public bool collect_user_info { get; set; }
        public bool show_call_to_action { get; set; }
        public string contact_link { get; set; }
        public string greeting { get; set; }
        public bool initialized { get; set; }
        public RedirectPrompts[] redirect_prompts { get; set; }
        public AnsweredQuestion[] answered_questions { get; set; }
    }
}