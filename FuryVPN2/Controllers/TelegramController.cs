using FuryVPN2.Data;
using FuryVPN2.Models;
using FuryVPN2.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Windows;
using Telegram.Bot.Types;
using static QRCoder.PayloadGenerator;

namespace FuryVPN2.Controllers
{
    public class TelegramController : Controller
    {
        private ApplicationDbContext _context;
        private SubscriptionManagementService _configurationManagementService = new();
        private PaymentService _paymentService = new();
        public TelegramController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            string id = Request.Query["id"];
            //здесь проверка последнего действия с ip пользователя и сверка с последним id если последний id другой - нет доступа
            var sessionIp = HttpContext.Connection.RemoteIpAddress.ToString();
            var sessionContext = _context.TelegramSessions.FirstOrDefault(s => s.SessionIp == sessionIp);
            if (sessionContext == null)
            {
                TelegramSession session = new TelegramSession();
                session.SessionIp = sessionIp;
                session.AccountId = id;
                _context.TelegramSessions.Add(session);
                _context.SaveChanges();
            }
            if (sessionContext?.AccountId != id & sessionIp == sessionContext?.SessionIp)
            {
                return Redirect("https://furyvpn.ru/Home/ErrorPage");
            }




            string firstName = Request.Query["first_name"];
            string lastName = Request.Query["last_name"];
            string username = Request.Query["username"];
            string authDate = Request.Query["auth_date"];
            string hash = Request.Query["hash"];

            if (firstName == null)
            {
                firstName = "null";
            }
            if (username == null)
            {
                username = "unknown";
            }


            ViewBag.Id = id;
            ViewBag.FirstName = firstName;
            ViewBag.Username = username;


            //найти пользователя по username если нет создать, проверить есть ли у него email если нет перевести на страницу с добавлением emaila

            var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == id);
            if (user == null)
            {
                //регистрация нового пользователя и добавление в базу данных

                TelegramUser telegramUser = new TelegramUser();
                telegramUser.TelegramId = id;
                telegramUser.UserName = username;
                telegramUser.FirstName = firstName;
                telegramUser.Email = $"{id}@gmail.com";
                _context.TelegramUsers.Add(telegramUser);

                _context.SaveChanges();
                return View();
            }
            else
            {
                if (user.Email == "null")
                {
                    return View();
                }
                else
                {
                    return RedirectToAction("PersonalArea", new { userId = id });
                }
            }
        }
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult PersonalArea(int userId, string promoExistense, string promoText, int promoDiscount, string error)
        {

            ViewBag.Error = error;
            var sessionIp = HttpContext.Connection.RemoteIpAddress.ToString();
            var sessionContext = _context.TelegramSessions.FirstOrDefault(s => s.SessionIp == sessionIp);
            if (sessionContext == null)
            {
                TelegramSession session = new TelegramSession();
                session.SessionIp = sessionIp;
                session.AccountId = userId.ToString();
                _context.TelegramSessions.Add(session);
                _context.SaveChanges();
            }
            if (sessionContext.AccountId != userId.ToString() & sessionIp == sessionContext.SessionIp)
            {
                return Redirect("https://furyvpn.ru/Home/ErrorPage");
            }



            var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == userId.ToString());
            ViewBag.PriceWithDiscount = (199 - promoDiscount).ToString();
            ViewBag.PromoExistence = promoExistense;
            ViewBag.PromoText = promoText;
            ViewBag.UserId = userId;
            if (user == null)
            {
                return Redirect("https://furyvpn.ru/Home/ErrorPage");
            }
            ViewBag.UserName = user.UserName;
            ViewBag.EndOfSubcription = CheckDateOfEndSubscriptionId(userId.ToString());

            var autoSub = _context.AutoSubscriptions.FirstOrDefault(a => a.Email == user.Email);
            if (autoSub != null)
            {
                if (autoSub.SubscriptionStatus == true)
                {
                    ViewBag.SwitchAutoPayStatusButtonText = "Отключить автоплатеж";
                    return View();
                }
                if (autoSub.SubscriptionStatus == false)
                {
                    ViewBag.SwitchAutoPayStatusButtonText = "Включить автоплатеж";
                    return View();
                }
            }

            return View();
         }
        public IActionResult SetEmail(string email, string userId)
        {
            ViewBag.Id = userId;

            if (email == null )
            {
                var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == userId);

                user.Email = $"{userId}@gmail.com";
                _context.TelegramUsers.Update(user);
                _context.SaveChanges();

                return RedirectToAction("PersonalArea", new { userId = userId });
            }
            if (!IsValidEmail(email))
            {
                ViewBag.Error = "Авторизоваться с данной почтой нельзя";
                return View("Index");

            }
            else
            {
                var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == userId);

                user.Email = email;
                _context.TelegramUsers.Update(user);
                _context.SaveChanges();


                return RedirectToAction("PersonalArea", new { userId = userId });
            }
        }
        public bool IsValidEmail(string email)
        {
            if (_context.Subscriptions.FirstOrDefault(s => s.Email == email) != null)
            {
                return false;
            }
            string AllowedDomainsRegex = @"@(gmail\.com|mail\.ru|yandex\.ru|yahoo\.com|internet\.ru|bk\.ru|inbox\.ru|list\.ru|icloud\.com|me\.com|mac\.com)$";
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Check if the email format is valid
            bool isEmailValid = Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!isEmailValid)
                return false;

            // Check if the domain is one of the allowed ones
            bool isDomainAllowed = Regex.IsMatch(email, AllowedDomainsRegex);
            return isDomainAllowed;
        }


        public IActionResult CheckPromoCode(string code, string userId)
        {
            var promoCode = _context.PromoCodes.FirstOrDefault(p => p.Code == code);
            if (promoCode != null)
            {
                ViewBag.UserId = userId;
                ViewBag.EndOfSubcription = CheckDateOfEndSubscriptionId(userId);
                string promoText = $"{promoCode.Code}";
                string promoExistense = $"Вы активировали промокод \"{promoCode.Code}\"";
                return RedirectToAction("PersonalArea", new { userId = userId, promoExistense = promoExistense, promoText = promoText, 
                                                            promoDiscount = promoCode.Discount });
            }
            else
            {
                ViewBag.UserId = userId;
                ViewBag.EndOfSubcription = CheckDateOfEndSubscriptionId(userId);
                string promoExistense = $"Промокода не существует";
                return RedirectToAction("PersonalArea", new { userId = userId, promoExistense = promoExistense });
            }
        }
        public string CheckDateOfEndSubscriptionId(string id)
        {
            try
            {
                var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == id);
                var subscription = _context.Subscriptions.Where(s => s.TelegramId == user.TelegramId).OrderByDescending(s => s.DateOfEnd).FirstOrDefault();
                if (subscription == null || subscription.DateOfEnd < DateTime.Now)
                {
                    return "У вас нет активных подписок";
                }
                else
                {
                    return "Ваша подписка истекает " + subscription.DateOfEnd.ToString("dd/MM/yyyy");
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public IActionResult GetProduct(string userId)
        {
            ViewBag.UserId = userId;
            var sessionIp = HttpContext.Connection.RemoteIpAddress.ToString();
            var sessionContext = _context.TelegramSessions.FirstOrDefault(s => s.SessionIp == sessionIp);
            if (sessionContext.AccountId != userId.ToString() & sessionIp == sessionContext.SessionIp)
            {
                return Redirect("https://furyvpn.ru/Home/ErrorPage");
            }

             var query = from sub in _context.Subscriptions
                        join ok in _context.OutlineKeys
                        on sub.OutlineKeyId equals ok.Id
                        where sub.TelegramId == userId
                         orderby sub.DateOfEnd descending
                         select new
                        {
                            sub.TelegramId,
                            sub.DateOfEnd,
                            ok.AccessUrl
                        };
            if (query.FirstOrDefault() == null)
            {
                return RedirectToAction("PersonalArea", new { userId = userId, error = "У вас нет активных подписок, возьмите тестовый " +
                    "период или выберите один из тарифов"
                });
            }
            var a = query.FirstOrDefault().DateOfEnd;
            if (query.FirstOrDefault().DateOfEnd < DateTime.Now)
            {
                return RedirectToAction("PersonalArea", new { userId = userId, error = "У вас нет активных подписок, возьмите тестовый " +
                    "период или выберите один из тарифов" });
            }
            //здесь логика доставания ссылки из бд
            string key = query.FirstOrDefault().AccessUrl;
            string[] partsOfKey = SplitStringIntoEqualParts(key);
            ViewBag.Key1 = partsOfKey[0];
            ViewBag.Key2 = partsOfKey[1];
            ViewBag.Key3 = partsOfKey[2];
            ViewBag.Key4 = partsOfKey[3];
            return View();
        }
        public IActionResult RedirectToProduct(string link)
        {
            return Redirect(link);
        }
        private string[] SplitStringIntoEqualParts(string input)
        {
            int partCount = 4;
            int partLength = input.Length / partCount;

            string[] parts = new string[partCount];

            for (int i = 0; i < partCount; i++)
            {
                int startIndex = i * partLength;
                parts[i] = input.Substring(startIndex, partLength);

                // Для последней части, если длина не делится равномерно, добавляем оставшиеся символы
                if (i == partCount - 1)
                {
                    parts[i] += input.Substring(startIndex + partLength);
                }
            }

            return parts;
        }

        public async Task<IActionResult> BuyAction(string userId, string tariff, string promocode)
        {
            var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == userId);
            if (tariff == "freeTrial")
            {
                if (_context.Subscriptions.Where(p => p.TelegramId == userId).OrderByDescending(p => p.DateOfEnd)
                                                .FirstOrDefault()?.DateOfEnd > DateTime.Now)
                {
                    return RedirectToAction("PersonalArea", new { userId = userId, error = "У вас уже есть активная подписка" });
                }
                if (_context.FreeTrials.FirstOrDefault(f => f.Email == user.Email) == null)
                {
                    Task.Run(() =>
                    {
                        var subscriptionResult = _configurationManagementService.GiveSubscriptionOutline(userId, user.Email, "freeTrial", promocode, 0, HttpContext);

                    });

                    //сделать здесь переход на страницу с благодарностью
                    return RedirectToAction("TestPeriodThankPage", new { userId = userId });
                }
                else
                {
                    return RedirectToAction("PersonalArea", new { userId = userId, error = "Вы уже использовали свой тестовый период" }) ;
                }
            }
            else
            {
                string thankPage = $"https://furyvpn.ru/Telegram/PersonalArea?userId={userId}";
                _paymentService.GeneratePayment(user.TelegramId, user.Email, tariff, PaymentService.TypeOfPayment.TelegramAuth, promocode, thankPage ,Response);
            }


            return View("Index");
        }
        public IActionResult TestPeriodThankPage(string userId)
        {
            ViewBag.UserId = userId;
            return View();
        }
        public IActionResult ThankPage(string userId)
        {
            ViewBag.UserId = userId;
            return View();
        }
        public IActionResult CancelAutoSubscription(string userId)
        {
            var user = _context.TelegramUsers.FirstOrDefault(u => u.TelegramId == userId);
            var autoSub = _context.AutoSubscriptions.FirstOrDefault(s => s.Email == user.Email);
            if (autoSub != null)
            {
                return Redirect(autoSub.CancelAutoSubscriptionLink);
            }
            else
            {
                return RedirectToAction("PersonalArea", new { userId = userId, error = "У вас нет активных подписок" });
            }
            
        }
    }
}
