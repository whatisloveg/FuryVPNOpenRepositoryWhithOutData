using FuryVPN2.Data;
using FuryVPN2.Models;
using FuryVPN2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.MSIdentity.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace FuryVPN2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult ToManual() 
        {
            string urlWithAnchor = Url.Action("Index", "Home") + "#manual";
            return Redirect(urlWithAnchor);
        }
        public IActionResult Offer()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult CancelSubscription(string email, string password)
        {
            var autoSubscription = _context.AutoSubscriptions.FirstOrDefault(s => s.Email == email);
            if (autoSubscription != null)
            {
                if (autoSubscription.PasswordToSwitchStatus != password)
                {
                    return View("ErrorToSwitch");
                }
                else
                {
                    if (autoSubscription.SubscriptionStatus == true)
                    {
                        autoSubscription.SubscriptionStatus = false;
                        autoSubscription.DateOfLasDisable = DateTime.Now;
                        _context.Update(autoSubscription);
                        _context.SaveChanges();
                        ViewBag.SubcriptionStatus = "отключено";
                        ViewBag.BudetSpisanie = "не будут";
                        ViewBag.SecondPart = "очень жаль что Вы не будете продлевать подписку, ждем вас еще";
                        return View();
                    }
                    if (autoSubscription.SubscriptionStatus == false)
                    {
                        autoSubscription.SubscriptionStatus = true;
                        autoSubscription.DateOfLasEnable = DateTime.Now;
                         _context.Update(autoSubscription);
                        _context.SaveChanges();
                        ViewBag.SubcriptionStatus = "включено";
                        ViewBag.BudetSpisanie = "снова будут";
                        ViewBag.SecondPart = "спасибо, что выбираете нас";
                        return View();
                    }
                    
                }
                
            }
            return View("ErrorToSwitch");
        }
        

        //public IActionResult DownloadFile(string name, string password)
        //{
        //    //Добавить суда проверку на соовтетсвие паролю
        //    // Путь к файлу на сервере
        //    string serverFilePath = $"{Directory.GetCurrentDirectory()}\\Configs\\{name}.conf";
        //    string fileName = "furyvpn.conf"; // Имя файла для скачивания клиентом

        //    // Полный путь к файлу на сервере
        //    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), serverFilePath);

        //    // Скачиваем файл
        //    return PhysicalFile(fullPath, "application/octet-stream", fileName);
        //}
        //public IActionResult ShowQRCode(string name, string password)
        //{
        //    //проверка на соответствие паролю
        //    string imagePath = $"QrCodes/{name}.png";
        //    ViewBag.ImagePath = imagePath;
        //    return View();
        //}
    }
}