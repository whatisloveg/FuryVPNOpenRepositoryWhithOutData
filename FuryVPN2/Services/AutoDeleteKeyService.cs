using FuryVPN2.Data;
using FuryVPN2.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace FuryVPN2.Services
{
    public class AutoDeleteKeyService
    {
        ApplicationDbContext _context = new ApplicationDbContext();
        SubscriptionManagementService _subscriptionManagementService = new SubscriptionManagementService();
        EmailSender _emailSender = new EmailSender();
        TelegramBotService _telegramBotService = new TelegramBotService();
        public void StartAutoDelete()
        {
            //неправильно сортирую? надо по дате? та не нихуя надо по ключу, потому что в условиях дата
            while(true)
            {
                var query = from sub in _context.Subscriptions
                            join ok in _context.OutlineKeys
                            on sub.OutlineKeyId equals ok.Id
                            where sub.DateOfEnd < DateTime.Now && ok.IsDelete == false
                            orderby sub.DateOfEnd 
                            select new
                            {
                                sub.TelegramId,
                                sub.Email,
                                sub.DateOfEnd,
                                sub.PaymentDbId,
                                sub.OutlineKeyId,
                                sub.IsExtend,
                                sub.PromoCodeId,
                                ok.AccessUrl,
                                ok.IdOnServer,
                                ok.ServerId
                            };
                //оно выбирает старые подписки нужно придумать поле которое будет отвечать есть ли еще одна подписка
                //сортировку по email
                var usersToDeleteFirstCheck = query.Where(u => u.IsExtend == false).ToList();
                var usersToDelete = usersToDeleteFirstCheck.ToList();

                //здесь удалили всех у кого нет автоподписки
                foreach (var user in usersToDeleteFirstCheck)
                {
                    try
                    {
                        var autoSub = _context.AutoSubscriptions.FirstOrDefault(s => s.Email == user.Email);
                        //так если равно null то над тоже отключать потому что freetrial
                        if (autoSub != null)
                        {
                            if (autoSub.SubscriptionStatus == false)
                            {
                                var server = _context.Servers.FirstOrDefault(s => s.Id == user.ServerId);
                                
                                UpdateKeyDeleteStatus(user.OutlineKeyId, _context);
                                usersToDelete.Remove(user);
                                if (user.TelegramId != null & user.TelegramId != "")
                                {
                                    _telegramBotService.SendMessageAboutDelete(user.TelegramId);
                                }
                                else
                                {
                                    _emailSender.SendShutdownAlert(user.Email);
                                }
                                _subscriptionManagementService.DeleteOutlineUser(server.ApiLink, user.IdOnServer);
                                continue;
                            }
                        }
                        if(user.PaymentDbId == 0)
                        {
                            var server = _context.Servers.FirstOrDefault(s => s.Id == user.ServerId);
                            
                            UpdateKeyDeleteStatus(user.OutlineKeyId, _context);
                            usersToDelete.Remove(user);
                            if (user.TelegramId != null & user.TelegramId != "")
                            {
                                _telegramBotService.SendMessageAboutDelete(user.TelegramId);
                            }
                            else
                            {
                                _emailSender.SendShutdownAlert(user.Email);
                            }
                            _subscriptionManagementService.DeleteOutlineUser(server.ApiLink, user.IdOnServer);
                            continue;
                        }

                    }
                    catch (Exception ex)
                    {
                        StreamWriter file = new StreamWriter("deleterLog.txt", true);
                        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                        file.Close();
                        continue;
                    }
                }

                //выставление платежей
                foreach (var user in usersToDelete)
                {
                    try
                    {
                        var payment = _context.PaymentsDb.Where(p => p.Id == user.PaymentDbId).OrderByDescending(p => p.DateOfPayment).FirstOrDefault();
                        PromoCode promo = _context.PromoCodes.FirstOrDefault(c => c.Id == user.PromoCodeId);
                        string promoCode = null;
                        if (promo != null)
                        {
                            promoCode = promo.Code;
                        }
                        PaymentService paymentService = new PaymentService();
                        paymentService.GenerateAutoPayment(user.TelegramId, user.Email, payment.Tariff,  PaymentService.TypeOfPayment.AutoBuy, promoCode, payment.PaymentMethodId);
                    }
                    catch (Exception ex)
                    {
                        StreamWriter file = new StreamWriter("deleterLog.txt", true);
                        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                        file.Close();
                        continue;
                    }
                }

                Thread.Sleep(600000);

                //удалить тех у кого дата последнего платежа меньше времени нынешнего - врмя ожидания + оповещение
                foreach (var user in usersToDelete)
                {
                    try
                    {
                        var payment = _context.PaymentsDb.Where(p => p.Email == user.Email).OrderByDescending(p => p.DateOfPayment).FirstOrDefault();
                        if (payment.DateOfPayment < DateTime.Now - TimeSpan.FromMinutes(40))
                        {
                            var server = _context.Servers.FirstOrDefault(s => s.Id == user.ServerId);
                            
                            //тут оповестить пользователя
                            if (user.TelegramId != null & user.TelegramId != "")
                            {
                                _telegramBotService.SendMessageAboutDelete(user.TelegramId);
                            }
                            else
                            {
                                _emailSender.SendShutdownAlert(user.Email);
                            }
                            UpdateKeyDeleteStatus(user.OutlineKeyId, _context);
                            _subscriptionManagementService.DeleteOutlineUser(server.ApiLink, user.IdOnServer);
                        }
                    }
                    catch (Exception ex)
                    {
                        StreamWriter file = new StreamWriter("deleterLog.txt", true);
                        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                        file.Close();
                        continue;
                    }
                }
                StreamWriter file1 = new StreamWriter("deleterLog.txt", true);
                file1.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + "Удаление пользователей произведено" + "\n" + "--------------------------");
                file1.Close();
                Thread.Sleep(3000000);
            }
        }
        private void UpdateKeyDeleteStatus(int keyId, ApplicationDbContext context)
        {
            try
            {
                var key = context.OutlineKeys.FirstOrDefault(k => k.Id == keyId);
                key.IsDelete = true;
                _context.Update(key);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                StreamWriter file1 = new StreamWriter("deleterLog.txt", true);
                file1.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file1.Close();
            }
            
        }
    }
}
