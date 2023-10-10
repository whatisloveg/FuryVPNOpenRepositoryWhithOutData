using BitMiracle.LibTiff.Classic;
using FuryVPN2.Data;
using FuryVPN2.Models;
using FuryVPN2.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Yandex.Checkout.V3;
using static QRCoder.PayloadGenerator;

namespace FuryVPN2.Controllers
{
    public class PaymentHandlingController : Controller
    {
        private ApplicationDbContext _context = new();
        private SubscriptionManagementService _subscriptionManagementService = new();
        private EmailSender _emailSender = new EmailSender();
        private string[] _youKassaIPs = { "185.71.76.0/27", "185.71.77.0/27", "77.75.153.0/25",
        "77.75.156.11", "77.75.156.35", "77.75.154.128/25", "2a02:5180::/32"};


        static string json = System.IO.File.ReadAllText("appsettings.json");
        static JObject jsonObject = JObject.Parse(json);

        Client _client = new Yandex.Checkout.V3.Client(
        shopId: jsonObject["ShopId"].ToString(),
        secretKey: jsonObject["SecretKey"].ToString());
        public IActionResult Index()
        {
            //try
            //{
            //    HandlePayment();

            //    return View();
            //}
            //catch (Exception ex)
            //{

            //    StreamWriter file = new StreamWriter("paymentLog.txt", true);
            //    file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"{ex.ToString()}" + "\n" + "--------------------------");
            //    file.Close();
            //    throw;
            //}
            return View();
        }
        public IActionResult Handle442()
        {
            try
            {
                HandlePayment();

                return View();
            }
            catch (Exception ex)
            {

                StreamWriter file = new StreamWriter("paymentLog.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"{ex.ToString()}" + "\n" + "--------------------------");
                file.Close();
                throw;
            }
        }
        protected void HandlePayment()
        {
            var syncIOFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
            {
                syncIOFeature.AllowSynchronousIO = true;
            }
            Thread.Sleep(1000);
            try
            {
                Message message = Client.ParseMessage(Request.Method, Request.ContentType, Request.Body);
                Payment payment = message?.Object;

                //var isPaymentExist = _context.PaymentsDb.FirstOrDefault(p => p.PaymentId == payment.Id);
                //if (isPaymentExist != null)
                //{
                //    _client.CapturePayment(payment);
                //    return;
                //}


                if (message?.Event == Event.PaymentWaitingForCapture && payment.Paid)
                {
                    string boughtType = payment.Metadata["Type"];
                    string paymentMethodId = payment.PaymentMethod.Id;
                    //_client.CapturePayment(payment);
                    if (boughtType == "AutoBuy")
                    {
                       
                        try
                        {
                            _client.CapturePayment(payment);
                        }
                        catch(Exception ex) 
                        {
                            StreamWriter file2 = new StreamWriter("paymentLog.txt", true);
                            file2.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                            file2.Close();
                            throw;
                        }
                        string telegramId = payment.Metadata["TelegramId"];
                        string email = payment.Metadata["Email"];
                        string tariff = payment.Metadata["Tariff"];
                        string promocode = payment.Metadata["Promocode"];
                        PaymentDb paymentDb = new PaymentDb
                        {
                            TelegramId = telegramId,
                            Email = email,
                            PaymentId = payment.Id,
                            PaymentMethodId = paymentMethodId,
                            Tariff = tariff,
                            PromoCode = promocode,
                            Paid = true,
                            DateOfPayment = DateTime.Now
                        };
                        _context.PaymentsDb.Add(paymentDb);
                        _context.SaveChanges();

                        _subscriptionManagementService.ExtendSubscriptionOutline(telegramId, email, tariff, promocode, paymentDb.Id, HttpContext);

                       

                        StreamWriter file = new StreamWriter("paymentLog.txt", true);
                        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"User:{email} payed by autopay" + "\n" + "--------------------------");
                        file.Close();
                        
                    }
                    if (boughtType == "FastBuy")
                    {
                        try
                        {
                            _client.CapturePayment(payment);
                        }
                        catch (Exception ex)
                        {
                            StreamWriter file2 = new StreamWriter("paymentLog.txt", true);
                            file2.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                            file2.Close();
                            throw;
                        }
                        string telegramId = payment.Metadata["TelegramId"];
                        string a = payment.PaymentMethod.Id;
                        string email = payment.Metadata["Email"];
                        string tariff = payment.Metadata["Tariff"];
                        string promocode = payment.Metadata["Promocode"];

                        var subscription = _context.Subscriptions.Where(s => s.Email == email).OrderByDescending(s => s.DateOfEnd).FirstOrDefault();
                        if (subscription != null && subscription.DateOfEnd > DateTime.Now)
                        {
                            PaymentDb paymentDb = new PaymentDb
                            {
                                TelegramId = telegramId,
                                PaymentId = payment.Id,
                                Email = email,
                                PaymentMethodId = paymentMethodId,
                                Tariff = tariff,
                                PromoCode = promocode,
                                Paid = true,
                                DateOfPayment = DateTime.Now
                            };
                            _context.PaymentsDb.Add(paymentDb);
                            _context.SaveChanges();

                            SubscriptionResult result = _subscriptionManagementService.ExtendSubscriptionOutline(telegramId, email, tariff, promocode, paymentDb.Id, HttpContext);
                            _emailSender.SendProduct(result.Email, result.DateOfEnd, result.AccessUrl, result.CancelAutoSubscriptionLink);
                        }
                        else 
                        { 
                           PaymentDb paymentDb = new PaymentDb
                            {
                                TelegramId = telegramId,
                                PaymentId = payment.Id,
                                Email = email,
                                PaymentMethodId = paymentMethodId,
                                Tariff = tariff,
                                PromoCode = promocode,
                                Paid = true,
                                DateOfPayment = DateTime.Now
                            };
                        _context.PaymentsDb.Add(paymentDb);
                        _context.SaveChanges();
                        SubscriptionResult result = _subscriptionManagementService.GiveSubscriptionOutline(null, email, tariff, promocode, paymentDb.Id, HttpContext);
                        _emailSender.SendProduct(result.Email, result.DateOfEnd, result.AccessUrl, result.CancelAutoSubscriptionLink);
                            //отправку email
                        }



                        StreamWriter file = new StreamWriter("paymentLog.txt", true);
                        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"User:{email} payed FASTBUY for tariff{tariff} with promocode:{promocode}" + "\n" + "--------------------------");
                        file.Close();

                    }
                    if (boughtType == "TelegramAuth")
                    {
                        try
                        {
                            _client.CapturePayment(payment);
                        }
                        catch (Exception ex)
                        {
                            StreamWriter file2 = new StreamWriter("paymentLog.txt", true);
                            file2.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                            file2.Close();
                            throw;
                        }
                        string telegramId = payment.Metadata["TelegramId"];
                        string email = payment.Metadata["Email"];
                        string tariff = payment.Metadata["Tariff"];
                        string promocode = payment.Metadata["Promocode"];

                        var subscription = _context.Subscriptions.Where(s => s.TelegramId == telegramId).OrderByDescending(s => s.DateOfEnd).FirstOrDefault();

                        if (subscription != null && subscription.DateOfEnd > DateTime.Now)
                        {
                            PaymentDb paymentDb = new PaymentDb
                            {
                                TelegramId = telegramId,
                                Email = email,
                                PaymentId = payment.Id,
                                PaymentMethodId = paymentMethodId,
                                Tariff = tariff,
                                PromoCode = promocode,
                                Paid = true,
                                DateOfPayment = DateTime.Now
                            };
                            _context.PaymentsDb.Add(paymentDb);
                            _context.SaveChanges();
                            _subscriptionManagementService.ExtendSubscriptionOutline(telegramId, email, tariff, promocode, paymentDb.Id, HttpContext);
                           
                           
                            StreamWriter file1 = new StreamWriter("paymentLog.txt", true);
                            file1.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" +
                                $"User:{telegramId} payed TelegramAuthExtend for tariff{tariff} with promocode:{promocode}" + "\n" +
                                "--------------------------");
                            file1.Close();
                            
                        }
                        else
                        {
                            PaymentDb paymentDb = new PaymentDb
                            {
                                TelegramId = telegramId,
                                Email = email,
                                PaymentId = payment.Id,
                                PaymentMethodId = paymentMethodId,
                                Tariff = tariff,
                                PromoCode = promocode,
                                Paid = true,
                                DateOfPayment = DateTime.Now
                            };
                            _context.PaymentsDb.Add(paymentDb);
                            _context.SaveChanges();
                            _subscriptionManagementService.GiveSubscriptionOutline(telegramId, email, tariff, promocode, paymentDb.Id,HttpContext);
                            
                           
                            StreamWriter file = new StreamWriter("paymentLog.txt", true);
                            file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" +
                                $"User:{telegramId} payed TelegramAuth for tariff{tariff} with promocode:{promocode}" + "\n" +
                                "--------------------------");
                            file.Close();
                            
                        }
                        
                        
                    }
                    
                }
                //if (message?.Event == Event.PaymentWaitingForCapture && payment.Status == PaymentStatus.Canceled)
                //{
                //    string boughtType = payment.Metadata["Type"];
                //    if (boughtType == "AutoBuy")
                //    {
                //        string paymentMethodId = payment.PaymentMethod.Id;
                //        string telegramId = payment.Metadata["TelegramId"];
                //        string email = payment.Metadata["Email"];
                //        string tariff = payment.Metadata["Tariff"];
                //        string promocode = payment.Metadata["Promocode"];

                //        PaymentDb paymentDb = new PaymentDb
                //        {
                //            TelegramId = telegramId,
                //            Email = email,
                //            PaymentMethodId = paymentMethodId,
                //            Tariff = tariff,
                //            PromoCode = promocode,
                //            Paid = false
                //        };
                //        _context.PaymentsDb.Add(paymentDb);

                //        var autopay = _context.AutoSubscriptions.FirstOrDefault(a => a.Email == email);
                //        autopay.DateOfLasDisable = DateTime.Now;
                //        autopay.SubscriptionStatus = false;
                //        _context.AutoSubscriptions.Update(autopay);

                //        _context.SaveChanges();

                //        StreamWriter file = new StreamWriter("paymentLog.txt", true);
                //        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"User:{email} dont pay by autopay" + "\n" + "--------------------------");
                //        file.Close();
                //        _client.CapturePayment(payment.Id);
                //    }
                //    //посылаю нахуй чела добавляю в таблицу платежей с false и удаляторе проверяю это уловие и удяляю с оповещением
                //}
            }
            catch (Exception ex)
            {
                StreamWriter file = new StreamWriter("paymentLog.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file.Close();
            }
        }

        private bool IsTrustIp(string ip)
        {
            bool result = false;
            foreach (string yip in _youKassaIPs)
            {
                if (ip == yip)
                {
                    result = true;
                    return result;
                }
            }
            return result;
        }
    }
}
