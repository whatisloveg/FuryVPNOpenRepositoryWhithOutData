using Telegram.Bot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;

using Telegram.Bot.Polling;
namespace FuryVPN2.Services
{
    public class TelegramBotService
    {
        static ITelegramBotClient bot = new TelegramBotClient("5843822242:AAFxX6hafmyNc9mAxn3GknPbz7eVAUTGVtM");

        public async Task SendMessageAboutDelete(string userId)
        {
            try
            {
                await bot.SendTextMessageAsync(userId, "Доброго времени суток, хотим уведомить Вас что у вас закончилась " +
                    "подписка на наш VPN сервис, если Вы хотите продолжить пользоваться нашим сервисом, купите новый ключ, " +
                    "с уважением FuryVPN.\n furyvpn.ru");
            }
            catch (Exception ex)
            {
                StreamWriter file = new StreamWriter("telegramBottLog.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file.Close();
            }
            
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            StreamWriter file = new StreamWriter("telegramBottLog.txt", true);
            file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
            file.Close();
        }

        public void StartBot()
        {

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
        }
    }
}
