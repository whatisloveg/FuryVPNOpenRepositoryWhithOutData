namespace FuryVPN2.Models
{
    public class PaymentDb
    {
        public int Id { get; set; }
        public string? TelegramId { get; set; }
        public string Email{ get; set; }
        public string PaymentId { get; set; }
        public string? PaymentMethodId { get; set; }
        public string Tariff { get; set; }
        public string PromoCode { get; set; }
        public bool Paid { get; set; }
        public DateTime DateOfPayment { get; set; }

    }
}
