namespace SalesBotApi.Models

{
    public class UserWithJwt : UserBase
    {
        public string jwt { get; set; }
    }
}