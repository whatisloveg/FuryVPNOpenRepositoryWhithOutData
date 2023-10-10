using System.Security.Cryptography.X509Certificates;

namespace FuryVPN2.Models
{
    public class SubscriptionResult
    {
        public string Email { get; set; }
        public DateTime DateOfEnd { get; set; }
        public string AccessUrl { get; set; }
        public string? CancelAutoSubscriptionLink { get; set; }
    }
}
