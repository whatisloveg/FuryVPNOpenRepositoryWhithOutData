using FuryVPN2.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace FuryVPN2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<InvalidEmailResult> InvalidEmailResults { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<FreeTrial> FreeTrials { get; set; }
        public DbSet<OutlineKey> OutlineKeys { get; set; }
        public DbSet<TelegramUser> TelegramUsers { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<TelegramSession> TelegramSessions { get; set; }
        public DbSet<AutoSubscription> AutoSubscriptions { get; set; }
        public DbSet<PaymentDb> PaymentsDb { get; set; }
        public ApplicationDbContext()
        {

        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string json = System.IO.File.ReadAllText("appsettings.json");
             JObject jsonObject = JObject.Parse(json);
            var connectionString = jsonObject["ConnectionStrings"]["ApplicationDbContextConnection"].ToString();
            optionsBuilder.UseSqlServer(connectionString);
         }
    }
}
