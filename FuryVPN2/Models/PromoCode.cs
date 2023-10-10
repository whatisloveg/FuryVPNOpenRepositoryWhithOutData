namespace FuryVPN2.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int NumbersOfActivations { get; set; }
        public int Discount { get; set; }
    }
}
