namespace FuryVPN2.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string? TelegramId { get;set; }
        public string Email { get; set; }
        public int OutlineKeyId { get; set; }
        public int? PaymentDbId { get; set; }
        public DateTime DateOfBuy { get; set; }
        public DateTime DateOfEnd { get; set;}
        public int PromoCodeId { get; set; }
        public bool IsExtend { get; set; }
    }
}
