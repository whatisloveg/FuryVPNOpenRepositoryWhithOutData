using FuryVPN2.Data;
using FuryVPN2.Models;
using FuryVPN2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace FuryVPN2.Controllers
{
    public class FastBuyController : Controller
    {
        private ApplicationDbContext _context;
        private PaymentService _paymentService = new();
        private EmailSender _emailSender = new ();
        private SubscriptionManagementService _configurationManagementService = new();
        public FastBuyController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> BuyAction(string email, string tariff, string promocode)
        {
            InvalidEmailResult invalidEmailResults = new InvalidEmailResult();
            invalidEmailResults.InvalidData = email;
            invalidEmailResults.Tariff = tariff;
            _context.InvalidEmailResults?.Add(invalidEmailResults);
            _context.SaveChanges();

            if (!IsValidEmail(email))
            {
                var promoCode = _context.PromoCodes.FirstOrDefault(p => p.Code == promocode);
                if (promoCode != null)
                {
                    ViewBag.PriceWithDiscount = (199 - promoCode.Discount).ToString();
                    ViewBag.PromoText = $"{promoCode.Code}";
                    ViewBag.PromoExistence = $"Вы активировали промокод \"{promoCode.Code}\"";
                }

                ViewBag.EmailValidation = "Проверьте почту которую вы указали, похоже она недействительна";
                return View("Index");
            }

            //выполнить проверку на нажатие коноки с тестовым периодом до этого на 30 минут если нет записать полседнее действие
            var sessionIp = HttpContext.Connection.RemoteIpAddress.ToString();
            var sessionContext = _context.Sessions?.FirstOrDefault(s => s.SessionIp == sessionIp);
            if (tariff == "freeTrial")
            {
                if (sessionContext?.DateOfLastAction.AddMinutes(30) >= DateTime.Now)
                {
                    ViewBag.EmailValidation = "Это действие сейчас не доступно, попробуйте позже";
                    return View("Index");
                }
                else
                {
                    if (sessionContext != null)
                    {
                        sessionContext.DateOfLastAction = DateTime.Now;
                        _context.Sessions.Update(sessionContext);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Session session = new Session();
                        session.SessionIp = sessionIp;
                        session.DateOfLastAction = DateTime.Now;
                        await _context.Sessions.AddAsync(session);
                        await _context.SaveChangesAsync();
                    }
                }
            }


            if (tariff == "freeTrial")
            {
                if (_context.Subscriptions.FirstOrDefault(u => u.Email == email) != null)
                {
                    if (_context.Subscriptions.Where(p => p.Email == email).OrderByDescending(p => p.DateOfEnd)
                                                    .FirstOrDefault().DateOfEnd > DateTime.Now &
                        _context.Subscriptions.Where(p => p.Email == email).OrderByDescending(p => p.DateOfEnd)
                        .FirstOrDefault().PaymentDbId != 0)
                    {
                        ViewBag.EmailValidation = "У вас уже есть подписка";
                        return View("Index");
                    }
                }
                if (_context.TelegramUsers.FirstOrDefault(u => u.Email == email) != null)
                {
                    ViewBag.EmailValidation = "Почта, которую вы указали, уже используется";
                    return View("Index");
                }
                if (_context.FreeTrials.FirstOrDefault(f => f.Email == email) == null)
                {
                    Task.Run(() =>
                    {
                        var subscriptionResult = _configurationManagementService.GiveSubscriptionOutline(null, email, "freeTrial", promocode,0, HttpContext);
                        _emailSender.SendTestProduct(subscriptionResult.Email,
                                                     subscriptionResult.DateOfEnd, subscriptionResult.AccessUrl);
                    });

                    return View("ThankPage");
                    //здесь вернуть сообщение о получении пробного периода
                }
                else
                {
                    ViewBag.EmailValidation = "Вы уже использовали свой тестовый период";
                    return View("Index");
                }
            }
            else
            {
                if (_context.Subscriptions.FirstOrDefault(u => u.Email == email) != null)
                {
                    if (_context.Subscriptions.Where(p => p.Email == email).OrderByDescending(p => p.DateOfEnd)
                                                    .FirstOrDefault().DateOfEnd > DateTime.Now & 
                        _context.Subscriptions.Where(p => p.Email == email).OrderByDescending(p => p.DateOfEnd)
                        .FirstOrDefault().PaymentDbId != 0)
                    {
                        ViewBag.EmailValidation = "У вас уже есть подписка, купите ключ на другую почту";
                        return View("Index");
                    }
                }
                string thankPage = "https://furyvpn.ru/FastBuy/ThankPage";
                _paymentService.GeneratePayment(null, email, tariff, PaymentService.TypeOfPayment.FastBuy, promocode, thankPage, Response);
            }


            return View("Index");
        }
        
        public IActionResult ThankPage()
        {
            return View();
        }
        public IActionResult CheckPromoCode(string code)
        {
            var promoCode = _context.PromoCodes.FirstOrDefault(p => p.Code == code);
            if (promoCode != null)
            {

                ViewBag.PromoText = $"{promoCode.Code}";
                ViewBag.PriceWithDiscount = (199 - promoCode.Discount).ToString();
                ViewBag.PromoExistence = $"Вы активировали промокод \"{promoCode.Code}\"";
                return View("Index");
            }
            else
            {
                ViewBag.PromoExistence = $"Промокода не существует";
                return View("Index");
            }
        }

        private bool IsValidEmail(string email)
        {
            bool result;
            if (email == null)
            {
                result = false;
            }
            else
            {
               result = email.Contains("@");
            }
            return result;
        }
    }
}
