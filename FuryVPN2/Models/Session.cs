namespace FuryVPN2.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string SessionIp { get; set; }
        public DateTime DateOfLastAction { get; set; }
    }
}

