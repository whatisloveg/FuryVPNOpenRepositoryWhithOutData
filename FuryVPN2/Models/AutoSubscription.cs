namespace FuryVPN2.Models
{
    public class AutoSubscription
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordToSwitchStatus { get; set; }
        public string CancelAutoSubscriptionLink { get; set; }
        public bool SubscriptionStatus { get; set; }
        public DateTime DateOfLasEnable { get; set; }
        public DateTime? DateOfLasDisable { get; set; }
    }
}
