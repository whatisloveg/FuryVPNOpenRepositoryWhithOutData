using Azure;
using FuryVPN2.Models;
using System.Net;
using Yandex.Checkout.V3;
using Newtonsoft.Json.Linq;
using FuryVPN2.Data;

namespace FuryVPN2.Services
{
    public class PaymentService
    {
        private ApplicationDbContext _context;
        static string json = System.IO.File.ReadAllText("appsettings.json");
        static JObject jsonObject = JObject.Parse(json);
        Client _client = new Yandex.Checkout.V3.Client(
        shopId: jsonObject["ShopId"].ToString(),
        secretKey: jsonObject["SecretKey"].ToString());


        public enum TypeOfPayment
        {
            TelegramAuth,
            FastBuy,
            AutoBuy
               
        }
        public void GeneratePayment(string telegramId, string email, string tariff, TypeOfPayment typeOfPayment, string promocode,
                                        string thankPageUrl, HttpResponse response)
        {
            ApplicationDbContext context = new ApplicationDbContext();
            try
            {
                decimal discount = 0;
                if (context.PromoCodes.FirstOrDefault(p => p.Code == promocode) != null)
                {
                    discount = context.PromoCodes.FirstOrDefault(p => p.Code == promocode).Discount;
                }
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                Dictionary<string, string> metadate = new Dictionary<string, string>();
                metadate.Add("Email", $"{email}");
                metadate.Add("TelegramId", $"{telegramId}");
                metadate.Add("Tariff", $"{tariff}");
                metadate.Add("Promocode", $"{promocode}");
                if (typeOfPayment == TypeOfPayment.AutoBuy)
                {
                    metadate.Add("Type", $"AutoBuy");
                }
                if (typeOfPayment == TypeOfPayment.TelegramAuth)
                {
                    metadate.Add("Type", $"TelegramAuth");
                }
                if(typeOfPayment == TypeOfPayment.FastBuy)
                {
                    metadate.Add("Type", $"FastBuy");
                }

                decimal amount;
                switch (tariff)
                {
                    case "monthWithOutPromo":
                        amount = 199;
                        break;
                    case "monthWithPromo":
                        amount = 199 - discount;
                        break;
                    case "3months":
                        amount = 499;
                        break;
                    case "6months":
                        amount = 1449;
                        break;
                    case "year":
                        amount = 1999;
                        break;
                    default:
                        amount = 199;
                        break;
                }
                
                Yandex.Checkout.V3.ReceiptItem receiptItem = new ReceiptItem();
                receiptItem.Description = "Подписка на сервис FuryVPN";
                receiptItem.Quantity = 1;
                receiptItem.PaymentMode = PaymentMode.FullPayment;
                receiptItem.PaymentSubject = PaymentSubject.Payment;
                receiptItem.VatCode = VatCode.NoVat;
                receiptItem.CountryOfOriginCode = "RU";
                Amount amount1 = new Amount();
                amount1.Value = amount;
                amount1.Currency = "RUB";
                receiptItem.Amount = amount1;
                receiptItem.Supplier = new Supplier() { Name = "https://furyvpn.ru", Phone = "+7 928 155 7113", Inn = "615521026680" };
                receiptItem.Supplier.Name = "https://furyvpn.ru";
                receiptItem.Supplier.Phone = "79281557113";
                receiptItem.Supplier.Inn = "615521026680";

                var newPayment = new NewPayment
                {
                    Description = "После оплаты Вы получите ключ для подключения к нашему серверу",
                    Amount = new Amount { Value = amount, Currency = "RUB" },
                    Confirmation = new Confirmation { Type = ConfirmationType.Redirect, ReturnUrl = thankPageUrl },
                    Receipt = new Receipt { Customer = new Customer { Email = email }, Items = { receiptItem } }//добавить items
                };
                newPayment.Metadata = metadate;
                newPayment.SavePaymentMethod = true;
                Payment payment = _client.CreatePayment(newPayment);
                

                string url = payment.Confirmation.ConfirmationUrl;
                response.Redirect(url);
            }
            catch (Exception ex)
            {
                StreamWriter file = new StreamWriter("paymentLog.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file.Close();
            }
        }
        public void GenerateAutoPayment(string telegramId, string email, string tariff, TypeOfPayment typeOfPayment, string promocode, string paymentMethodId)
        {
            ApplicationDbContext context = new ApplicationDbContext();
            try
            {
                decimal discount = 0;
                if (context.PromoCodes.FirstOrDefault(p => p.Code == promocode) != null)
                {
                    discount = context.PromoCodes.FirstOrDefault(p => p.Code == promocode).Discount;
                }
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                Dictionary<string, string> metadate = new Dictionary<string, string>();
                metadate.Add("Email", $"{email}");
                metadate.Add("TelegramId", $"{telegramId}");
                metadate.Add("Tariff", $"{tariff}");
                metadate.Add("Promocode", $"{promocode}");
                if (typeOfPayment == TypeOfPayment.AutoBuy)
                {
                    metadate.Add("Type", $"AutoBuy");
                }
                if (typeOfPayment == TypeOfPayment.TelegramAuth)
                {
                    metadate.Add("Type", $"TelegramAuth");
                }
                if (typeOfPayment == TypeOfPayment.FastBuy)
                {
                    metadate.Add("Type", $"FastBuy");
                }

                decimal amount;
                switch (tariff)
                {
                    case "monthWithOutPromo":
                        amount = 199;
                        break;
                    case "monthWithPromo":
                        amount = 199 - discount;
                        break;
                    case "3months":
                        amount = 499;
                        break;
                    case "6months":
                        amount = 1449;
                        break;
                    case "year":
                        amount = 1999;
                        break;
                    default:
                        amount = 199;
                        break;
                }

                Yandex.Checkout.V3.ReceiptItem receiptItem = new ReceiptItem();
                receiptItem.Description = "Подписка на сервис FuryVPN";
                receiptItem.Quantity = 1;
                receiptItem.PaymentMode = PaymentMode.FullPayment;
                receiptItem.PaymentSubject = PaymentSubject.Payment;
                receiptItem.VatCode = VatCode.NoVat;
                receiptItem.CountryOfOriginCode = "RU";
                Amount amount1 = new Amount();
                amount1.Value = amount;
                amount1.Currency = "RUB";
                receiptItem.Amount = amount1;
                receiptItem.Supplier = new Supplier() { Name = "https://furyvpn.ru", Phone = "+7 928 155 7113", Inn = "615521026680" };
                receiptItem.Supplier.Name = "https://furyvpn.ru";
                receiptItem.Supplier.Phone = "79281557113";
                receiptItem.Supplier.Inn = "615521026680";

                var newPayment = new NewPayment
                {
                    Description = "После оплаты Вы получите ключ для подключения к нашему серверу",
                    Amount = new Amount { Value = amount, Currency = "RUB" },
                    Confirmation = null,
                    Receipt = new Receipt { Customer = new Customer { Email = email }, Items = { receiptItem } }
                    //добавить items
                };
                newPayment.Metadata = metadate;
                newPayment.PaymentMethodId = paymentMethodId;
                //newPayment.SavePaymentMethod = true;
                Payment payment = _client.CreatePayment(newPayment);


            }
            catch (Exception ex)
            {
                StreamWriter file = new StreamWriter("paymentLog.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file.Close();
            }
        }
    }
}
