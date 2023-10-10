using System.Globalization;
using System.Net.Mail;
using System.Net;
using Newtonsoft.Json.Linq;

namespace FuryVPN2.Services
{
    public class EmailSender
    {
        public void SendProduct(string toEmail, DateTime dateOfEnd, string accessUrl, string cancelSubscriptionLink)
        {
            string json = System.IO.File.ReadAllText("appsettings.json");
            JObject jsonObject = JObject.Parse(json);

            MailAddress fromAddress = new MailAddress("support@furyvpn.ru", "Fury VPN");
            MailAddress toAddress = new MailAddress(toEmail);
            // Настройка SMTP клиента
            SmtpClient smtpClient = new SmtpClient
            {
                Host = "connect.smtp.bz",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(jsonObject["EmailSenderId"].ToString(), jsonObject["EmailSenderPassword"].ToString())
            };


            MailMessage msg = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Подписка активирована",
                Body = $"<html>\r\n<body style=\"font-family: Arial, sans-serif;" +
            $" background-color: #f5f5f5; padding: 20px;\">\r\n\t<div style=\"max-width:" +
            $" 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px;" +
            $" padding: 20px;\">\r\n\t\t<div style=\"text-align: center;\">\r\n\t\t\t<img src=\"https://i.imgur.com/lWiAXt9.png\"" +
            $" alt=\"Логотип\" style=\"max-width: 100%; height: auto;\">\r\n\t\t\t<h1" +
            $" style=\"font-size: 28px; color: #000000; margin-top: 20px;\">Спасибо за покупку</h1>\r\n\t\t" +
            $"</div>\r\n\t\t" +
            $"<p style =\"font-size: 16px; color: #000000; text-align: center; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b> Ваша подписка истекает {dateOfEnd.ToString("d MMMM yyyy 'года'", new CultureInfo("ru-RU"))}" +
            $", после данной даты ключ перестанет быть действительным, нужна будет новая</b></p>" +
            $"<p style=\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b>Вам осталось только скачать приложение Outline в соответствии с вашей платформой " +
            $"по кнопке ниже" +
            $" и добавить наш ключ к серверу</b></p>\r\n\t\t" +
            $"<div style=\"text-align: center; margin-top: 0px;\"><a " +
            $"href='https://s3.amazonaws.com/outline-releases/client/windows/stable/Outline-Client.exe' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            $"border-radius: 5px;\">Windows</a><a " +
            $"href='https://apps.apple.com/us/app/outline-app/id1356177741' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px;padding: 10px 20px; " +
            $"border-radius: 5px;\">IOS</a><a " +
            $"href='https://apps.apple.com/us/app/outline-app/id1356178125' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            $"border-radius: 5px;\">macOS</a><a " +
            $"href='https://play.google.com/store/apps/details?id=org.outline.android.client' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px;  padding: 10px 20px; " +
            $"border-radius: 5px;\">Android</a></div>" +

            $"<p style=\"font-size: 16px;  text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b>Чтобы добавить ключ нажмите на него или скопируйте и вставьте в официальное приложение Outline: " +
            $"</b></p>\r\n\t\t<div style=\"text-align: center; font-weight: bold; margin-right: 20px; margin-left: 20px; margin-top: 5px;\">\r\n\t\t\t<a " +
            $"href='{accessUrl}' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #F1F1F1; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
            $"border-radius: 5px;\">{accessUrl}</a></div>\r\n\t\t" +

             "<p style =\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
                $"margin-bottom: 10px; \"><b>Чтобы отменить автопродление подписки" +
                $" нажмите кнопку ниже:</b></p>\r\n\t\t<div style=\"text-align: center; margin-top: 5px;\">\r\n\t\t\t<a " +
                $"href='{cancelSubscriptionLink}' style=\"display: inline-block; font-weight: bold; background-color:" +
                $" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
                $"border-radius: 5px;\">Отключить автоплатеж</a></div>" +
            $"</div>\r\n</body>\r\n</html>"
             ,
                IsBodyHtml = true
            };
            msg.BodyEncoding = System.Text.Encoding.UTF8;
            try
            {
                smtpClient.Send(msg);
            }
            catch (Exception ex)
            {
                StreamWriter file1 = new StreamWriter("emailSenderLog.txt", true);
                file1.Write("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file1.Close();
            }
        }
        public void SendTestProduct(string toEmail, DateTime dateOfEnd, string accessUrl)
        {
            string json = System.IO.File.ReadAllText("appsettings.json");
            JObject jsonObject = JObject.Parse(json);

            MailAddress fromAddress = new MailAddress("support@furyvpn.ru", "Fury VPN");
            MailAddress toAddress = new MailAddress(toEmail);
            // Настройка SMTP клиента
            SmtpClient smtpClient = new SmtpClient
            {
                Host = "connect.smtp.bz",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(jsonObject["EmailSenderId"].ToString(), jsonObject["EmailSenderPassword"].ToString())
            };
            MailMessage msg = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Тестовый период активирован",
                Body = $"<html>\r\n<body style=\"font-family: Arial, sans-serif;" +
            $" background-color: #f5f5f5; padding: 20px;\">\r\n\t<div style=\"max-width:" +
            $" 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px;" +
            $" padding: 20px;\">\r\n\t\t<div style=\"text-align: center;\">\r\n\t\t\t<img src=\"https://i.imgur.com/lWiAXt9.png\"" +
            $" alt=\"Логотип\" style=\"max-width: 100%; height: auto;\">\r\n\t\t\t<h1" +
            $" style=\"font-size: 28px; color: #000000; margin-top: 20px;\">Тестовый период активирован</h1>\r\n\t\t" +
            $"</div>\r\n\t\t" +
            $"<p style =\"font-size: 16px; color: #000000; text-align: center; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b> Ваша подписка истекает {dateOfEnd.ToString("d MMMM yyyy 'года'", new CultureInfo("ru-RU"))}" +
            $", после данной даты ключ перестанет быть действительным, нужна будет новая</b></p>" +
            $"<p style=\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b>Вам осталось только скачать приложение Outline в соответствии с вашей платформой " +
            $"по кнопке ниже" +
            $" и добавить наш ключ к серверу</b></p>\r\n\t\t" +
            $"<div style=\"text-align: center; margin-top: 0px;\"><a " +
            $"href='https://s3.amazonaws.com/outline-releases/client/windows/stable/Outline-Client.exe' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            $"border-radius: 5px;\">Windows</a><a " +
            $"href='https://apps.apple.com/us/app/outline-app/id1356177741' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px;padding: 10px 20px; " +
            $"border-radius: 5px;\">IOS</a><a " +
            $"href='https://apps.apple.com/us/app/outline-app/id1356178125' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            $"border-radius: 5px;\">macOS</a><a " +
            $"href='https://play.google.com/store/apps/details?id=org.outline.android.client' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #ffffff; font-weight: bold; text-decoration: none; font-size: 16px; margin: 10px;  padding: 10px 20px; " +
            $"border-radius: 5px;\">Android</a></div>" +


            // "<p style =\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
            //$"margin-bottom: 10px; \"> <b> Вы можете посмотреть видеоинструкцию для своей платформы " +
            //$"" +
            //$"</b></p>\r\n\t\t" +
            //$"<div style=\"text-align: center; margin-top: 0px;\"></p><a " +
            //$"href='https://youtu.be/N-qEkV4pctE' style=\"display: inline-block; background-color:" +
            //$" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            //$"border-radius: 5px;\">Windows</a><a " +
            //$"href='https://youtu.be/wEOEaURTTtc' style=\"display: inline-block; background-color:" +
            //$" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; margin: 10px;padding: 10px 20px; " +
            //$"border-radius: 5px;\">IOS</a><a " +
            //$"href='https://youtu.be/N-qEkV4pctE' style=\"display: inline-block; background-color:" +
            //$" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; margin: 10px; padding: 10px 20px; " +
            //$"border-radius: 5px;\">macOS</a><a " +
            //$"href='https://youtu.be/iGdDQ1t1_jI' style=\"display: inline-block; background-color:" +
            //$" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; margin: 10px;  padding: 10px 20px; " +
            //$"border-radius: 5px;\">Android</a></div>" +

            $"<p style=\"font-size: 16px;  text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
            $"margin-bottom: 10px; \"><b>Чтобы добавить ключ нажмите на него или скопируйте и вставьте в официальное приложение Outline: " +
            $"</b></p>\r\n\t\t<div style=\"text-align: center; font-weight: bold; margin-right: 20px; margin-left: 20px; margin-top: 5px;\">\r\n\t\t\t<a " +
            $"href='{accessUrl}' style=\"display: inline-block; background-color:" +
            $" #1869B7; color: #F1F1F1; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
            $"border-radius: 5px;\">{accessUrl}</a></div>\r\n\t\t" +
             //$"<div style=\"text-align: center; margin-top: 15px;\">\r\n\t\t\t<a " +
             //$"href='{qrLink}' style=\"display: inline-block; background-color:" +
             //$" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
             //$"border-radius: 5px;\">Посмотреть QR-код</a>\r\n\t\t</div>\r\n\t" 

            $"</div>\r\n</body>\r\n</html>"
            ,
                IsBodyHtml = true
            };
            msg.BodyEncoding = System.Text.Encoding.UTF8;

            try
            {
               smtpClient.Send(msg);
            }
            catch (Exception ex)
            {
                StreamWriter file1 = new StreamWriter("emailSenderLog.txt", true);
                file1.Write("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file1.Close();
            }
        }
        public void SendShutdownAlert(string toEmail)
        {
            string json = System.IO.File.ReadAllText("appsettings.json");
            JObject jsonObject = JObject.Parse(json);

            MailAddress fromAddress = new MailAddress("support@furyvpn.ru", "Fury VPN");
            MailAddress toAddress = new MailAddress(toEmail);
            // Настройка SMTP клиента
            SmtpClient smtpClient = new SmtpClient
            {
                Host = "connect.smtp.bz",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(jsonObject["EmailSenderId"].ToString(), jsonObject["EmailSenderPassword"].ToString())
            };


            MailMessage msg = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Истек срок подписки",
                Body = $"<html>\r\n<body style=\"font-family: Arial, sans-serif;" +
             $" background-color: #f5f5f5; padding: 20px;\">\r\n\t<div style=\"max-width:" +
             $" 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px;" +
             $" padding: 20px;\">\r\n\t\t<div style=\"text-align: center;\">\r\n\t\t\t<img src=\"https://i.imgur.com/lWiAXt9.png\"" +
             $" alt=\"Логотип\" style=\"max-width: 100%; height: auto;\">\r\n\t\t\t<h1" +
             $" style=\"font-size: 28px; color: #000000; margin-top: 20px;\">Истек срок подписки</h1>\r\n\t\t" +
             $"</div>\r\n\t\t" +
             $"<p style =\"font-size: 16px; color: #000000; text-align: center; font-family: Arial; line-height: 1.5; " +
             $"margin-bottom: 10px; \"><b> Доброго времени суток, у вас закончилась подписка на наш сервис, теперь старый ключ работать не будет, " +
             $"нужна новая  " +
             $"</b></p>" +
             $"<p style=\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
             $"margin-bottom: 10px; \"><b>Чтобы перейти на сайт и купить новую подписку, нажмите на " +
             $"кнопку ниже:</b></p>\r\n\t\t<div style=\"text-align: center; margin-top: 30px;\">\r\n\t\t\t<a " +
             $"href='https://furyvpn.ru/Home/FastBuy' style=\"display: inline-block; background-color:" +
             $" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
             $"border-radius: 5px;\">Перейти к покупке</a></div>\r\n\t\t" +
             $"<div style=\"text-align: center; margin-top: 15px;\"></div>\r\n\t</div>\r\n</body>\r\n</html>"
             ,
                IsBodyHtml = true
            };
            msg.BodyEncoding = System.Text.Encoding.UTF8;
            try
            {
                smtpClient.Send(msg);
            }
            catch (Exception ex)
            {
                StreamWriter file1 = new StreamWriter("emailSenderLog.txt", true);
                file1.Write("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file1.Close();
            }

        }
        public void SendShutdownAlert2(string toEmail)
        {
            string json = System.IO.File.ReadAllText("appsettings.json");
            JObject jsonObject = JObject.Parse(json);

            MailAddress fromAddress = new MailAddress("support@furyvpn.ru", "Fury VPN");
            MailAddress toAddress = new MailAddress(toEmail);
            // Настройка SMTP клиента
            SmtpClient smtpClient = new SmtpClient
            {
                Host = "connect.smtp.bz",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(jsonObject["EmailSenderId"].ToString(), jsonObject["EmailSenderPassword"].ToString())
            };


            MailMessage msg = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Истек срок подписки",
                Body = $"<html>\r\n<body style=\"font-family: Arial, sans-serif;" +
             $" background-color: #f5f5f5; padding: 20px;\">\r\n\t<div style=\"max-width:" +
             $" 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px;" +
             $" padding: 20px;\">\r\n\t\t<div style=\"text-align: center;\">\r\n\t\t\t<img src=\"https://i.imgur.com/lWiAXt9.png\"" +
             $" alt=\"Логотип\" style=\"max-width: 100%; height: auto;\">\r\n\t\t\t<h1" +
             $" style=\"font-size: 28px; color: #000000; margin-top: 20px;\">Истек срок подписки</h1>\r\n\t\t" +
             $"</div>\r\n\t\t" +
             $"<p style =\"font-size: 16px; color: #000000; text-align: center; font-family: Arial; line-height: 1.5; " +
             $"margin-bottom: 10px; \"><b> Доброго времени суток, у вас закончилась подписка на наш сервис, теперь старый ключ работать не будет, " +
             $"нужна новая  " +
             $"</b></p>" +
             $"<p style=\"font-size: 16px; text-align: center; color: #000000; font-family: Arial; line-height: 1.5; " +
             $"margin-bottom: 10px; \"><b>Чтобы перейти на сайт и купить новую подписку, нажмите на " +
             $"кнопку ниже:</b></p>\r\n\t\t<div style=\"text-align: center; margin-top: 30px;\">\r\n\t\t\t<a " +
             $"href='https://furyvpn.ru/Home/FastBuy' style=\"display: inline-block; background-color:" +
             $" #1869B7; color: #ffffff; text-decoration: none; font-size: 16px; padding: 10px 20px; " +
             $"border-radius: 5px;\">Перейти к покупке</a></div>\r\n\t\t" +
             $"<div style=\"text-align: center; margin-top: 15px;\"></div>\r\n\t</div>\r\n</body>\r\n</html>"
             ,
                IsBodyHtml = true
            };
            msg.BodyEncoding = System.Text.Encoding.UTF8;
            try
            {
                smtpClient.Send(msg);
            }
            catch (Exception ex)
            {
                StreamWriter file1 = new StreamWriter("emailSenderLog.txt", true);
                file1.Write("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file1.Close();
            }

        }

    }
}
