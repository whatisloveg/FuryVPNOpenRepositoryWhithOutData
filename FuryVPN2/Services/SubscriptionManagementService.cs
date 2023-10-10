using Azure;
using Microsoft.AspNetCore.Components.RenderTree;
using NuGet.DependencyResolver;
using QRCoder;
using Renci.SshNet;
using Renci.SshNet.Async;
using System;
using System.Drawing;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using IronBarCode;
using FuryVPN2.Data;
using IronSoftware.Drawing;
using FuryVPN2.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using Yandex.Checkout.V3;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace FuryVPN2.Services
{
    public class SubscriptionManagementService
    {
        public SubscriptionResult GiveSubscriptionOutline(string telegramId, string email, string tariff, string promocode, int paymentDbId, HttpContext httpContext)
        {
            try
            {
                var context = new ApplicationDbContext();
                string userName = GenerateRandomName();

                //выбор лучшего сервера и проверка на наличие такого же username
                Server server = DefineProfitServer(context);
                List<OutlineUserData> users = GetAllUsersFromServer(server.ApiLink);
                for (int i = 0; i < users.Count; i++)
                {
                    if (users[i].Name == userName)
                    {
                        userName = GenerateRandomName();
                        i = 0;
                    }
                }


                OutlineUserData userData = CreateUserOutline(server.ApiLink, userName);

                OutlineKey outlineKey = new OutlineKey
                {
                    IdOnServer = userData.Id,
                    Name = userData.Name,
                    AccessUrl = userData.AccessUrl,
                    ServerId = server.Id
                };

                context.OutlineKeys.Add(outlineKey);
                context.SaveChanges();

                if (tariff == "freeTrial")
                {
                    FreeTrial freeTrial = new FreeTrial { Email = email, IsUsed = true };
                    context.FreeTrials.AddAsync(freeTrial);
                    context.SaveChanges();
                }

                PromoCode promo = context.PromoCodes.FirstOrDefault(c => c.Code == promocode);
                if (promo != null) { promo.NumbersOfActivations++; context.Update(promo); context.SaveChanges(); }
                int promoCodeId = (promo != null) ? promo.Id : 0;


                DateTime dateOfEnd;
                switch (tariff)
                {
                    case "month":
                        dateOfEnd = DateTime.Now.AddDays(30);
                        break;
                    case "3months":
                        dateOfEnd = DateTime.Now.AddDays(90);
                        break;
                    case "6months":
                        dateOfEnd = DateTime.Now.AddDays(180);
                        break;
                    case "year":
                        dateOfEnd = DateTime.Now.AddDays(365);
                        break;
                    case "freeTrial":
                        dateOfEnd = DateTime.Now.AddDays(5);
                        break;
                    default:
                        dateOfEnd = DateTime.Now.AddDays(30);
                        break;
                }

                Subscription subscription = new Subscription
                {
                    TelegramId = telegramId,
                    Email = email,
                    DateOfBuy = DateTime.Now,
                    DateOfEnd = dateOfEnd,
                    OutlineKeyId = outlineKey.Id,
                    PromoCodeId = promoCodeId,
                    PaymentDbId = paymentDbId
                };

                context.Subscriptions.Add(subscription);
                context.SaveChanges();

                AutoSubscription autoSubscription = context.AutoSubscriptions.FirstOrDefault(s => s.Email == email);
                //if (tariff != "freeTrial")
                //{
                string domainName = "https://furyvpn.ru/"; //сменить здесь при смене домена, не проебать этот момент

                if (autoSubscription == null)
                {
                    autoSubscription = new AutoSubscription();
                    autoSubscription.Email = email;
                    autoSubscription.DateOfLasEnable = DateTime.Now;
                    autoSubscription.SubscriptionStatus = true;
                    autoSubscription.PasswordToSwitchStatus = GenerateRandomName();
                    autoSubscription.CancelAutoSubscriptionLink = domainName +
                                                $"Home/CancelSubscription?email={email}&password={autoSubscription.PasswordToSwitchStatus}";
                    context.AutoSubscriptions.Add(autoSubscription);
                    context.SaveChanges();

                }
                else
                {
                    autoSubscription.SubscriptionStatus = true;
                    context.AutoSubscriptions.Update(autoSubscription);
                    context.SaveChanges();
                }
                //}


                return new SubscriptionResult
                {
                    Email = email,
                    AccessUrl = outlineKey.AccessUrl,
                    DateOfEnd = dateOfEnd,
                    CancelAutoSubscriptionLink = autoSubscription?.CancelAutoSubscriptionLink
                };
            }
            catch (Exception ex)
            {
                StreamWriter file = new StreamWriter("Log.txt", true);
                file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + ex.ToString() + "\n" + "--------------------------");
                file.Close();
                throw;
            }

        }
        public SubscriptionResult ExtendSubscriptionOutline(string telegramId, string email, string tariff, string promocode, int paymentDbId, HttpContext httpContext)
        {
            var context = new ApplicationDbContext();

            var subscription = context.Subscriptions.Where(s => s.Email== email)
                    .OrderByDescending(s => s.DateOfEnd).FirstOrDefault(); 

            PromoCode promo = context.PromoCodes.FirstOrDefault(c => c.Code == promocode);
            if (promo != null) { promo.NumbersOfActivations++; context.Update(promo); context.SaveChanges(); }
            int promoCodeId = (promo != null) ? promo.Id : 0;

            

            DateTime dateOfEnd;
            switch (tariff)
            {
                case "month":
                    dateOfEnd = subscription.DateOfEnd.AddDays(30);
                    break;
                case "3months":
                    dateOfEnd = subscription.DateOfEnd.AddDays(90);
                    break;
                case "6months":
                    dateOfEnd = subscription.DateOfEnd.AddDays(180);
                    break;
                case "year":
                    dateOfEnd = subscription.DateOfEnd.AddDays(365);
                    break;
                case "freeTrial":
                    dateOfEnd = subscription.DateOfEnd.AddDays(1);
                    break;
                default:
                    dateOfEnd = subscription.DateOfEnd.AddDays(30);
                    break;
            }

            Subscription newSubscription = new Subscription{ DateOfBuy = subscription.DateOfBuy, Email = subscription.Email,
                     DateOfEnd = dateOfEnd, TelegramId = telegramId, OutlineKeyId = subscription.OutlineKeyId,
                            PromoCodeId = subscription.PromoCodeId, PaymentDbId = paymentDbId};

            subscription.IsExtend = true;
            context.Subscriptions.Update(subscription);
            context.Subscriptions.Add(newSubscription);
            context.SaveChanges();

            //добавление автоподписки
            AutoSubscription autoSubscription = context.AutoSubscriptions.FirstOrDefault(s => s.Email == email);
            string domainName = "https://furyvpn.ru/"; //сменить здесь при смене домена

            if (autoSubscription == null)
            {
                autoSubscription = new AutoSubscription();
                autoSubscription.Email = email;
                autoSubscription.DateOfLasEnable = DateTime.Now;
                autoSubscription.SubscriptionStatus = true;
                autoSubscription.PasswordToSwitchStatus = GenerateRandomName();
                autoSubscription.CancelAutoSubscriptionLink = domainName +
                                            $"Home/CancelSubscription?email={email}&password={autoSubscription.PasswordToSwitchStatus}";
                context.AutoSubscriptions.Add(autoSubscription);
                context.SaveChanges();

            }
            return new SubscriptionResult
            {
                Email = email,
                AccessUrl = context.OutlineKeys.FirstOrDefault(k => k.Id == subscription.OutlineKeyId).AccessUrl,
                DateOfEnd = dateOfEnd,
                CancelAutoSubscriptionLink = autoSubscription?.CancelAutoSubscriptionLink
            };
        }
        private Server DefineProfitServer(ApplicationDbContext context)
        {
            List<Server> servers = context.Servers.ToList();
            Server userProfitServer = servers[0];
            int amountOfUsers = GetAllUsersFromServer(userProfitServer.ApiLink).Count();
            foreach (var server in servers)
            {
                int resultOfCheck = GetAllUsersFromServer(server.ApiLink).Count();
                if (amountOfUsers > resultOfCheck)
                {
                    amountOfUsers = resultOfCheck;
                    userProfitServer = server;
                }
            }
            return userProfitServer;
        }

        public OutlineUserData DeleteOutlineUser(string apiUrl, string IdOnServer)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl + $"/access-keys/{IdOnServer}");
            request.Method = "DELETE";//Post - create DELETE - delete GET - get all
            request.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            string responseText = reader.ReadToEnd();

            OutlineUserData user = JsonConvert.DeserializeObject<OutlineUserData>(responseText);

            return user;
        }
        private List<OutlineUserData> GetAllUsersFromServer(string apiUrl)
        {
            HttpWebRequest requestToCreate = (HttpWebRequest)WebRequest.Create(apiUrl + "/access-keys/");
            requestToCreate.Method = "GET";
            //Post - create DELETE - delete GET - get all
            requestToCreate.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            HttpWebResponse response = (HttpWebResponse)requestToCreate.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            string jsonString = reader.ReadToEnd();

            List<OutlineUserData> userList = new List<OutlineUserData>();

            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            JArray accessKeys = jsonData["accessKeys"];

            foreach (JToken item in accessKeys)
            {
                OutlineUserData user = new OutlineUserData
                {
                    Id = item["id"].ToString(),
                    Name = item["name"].ToString(),
                    Password = item["password"].ToString(),
                    Port = Convert.ToInt32(item["port"]),
                    Method = item["method"].ToString(),
                    AccessUrl = item["accessUrl"].ToString()
                };
                userList.Add(user);
            }

            return userList;
        }

        private OutlineUserData CreateUserOutline(string apiUrl, string username)
        {
            HttpWebRequest requestToCreate = (HttpWebRequest)WebRequest.Create(apiUrl + "/access-keys");
            requestToCreate.Method = "POST";//Post - create DELETE - delete GET - get all
            requestToCreate.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            HttpWebResponse response = (HttpWebResponse)requestToCreate.GetResponse();
            
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            
            string responseText = reader.ReadToEnd();

            OutlineUserData user = JsonConvert.DeserializeObject<OutlineUserData>(responseText);

            string changedLink = user.AccessUrl+"#furyvpn.ru";
            user.AccessUrl = changedLink;


            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };
            HttpClient httpClient = new HttpClient(handler);

            string url = $"{apiUrl}/access-keys/{user.Id}/name";

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(username), "name");

            httpClient.PutAsync(url, formData).Result.ToString();
            
            user.Name = username;

            responseStream.Close();
            response.Close();
            return user;
        }

        private string GenerateRandomName()
        {
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 12)  // 8 символов в имени
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
        //public SubscriptionResult GiveSubscriptionWireGuard(string telegramId, string email, string tariff, string promocode)
        //{
        //    var context = new ApplicationDbContext();
        //    string password = GenerateRandomName();
        //    string fileName = GenerateRandomName();
        //    string localFilePath = $"{Directory.GetCurrentDirectory()}\\Configs\\\\{fileName}.conf";
        //    while (File.Exists(localFilePath))
        //    {
        //        fileName = GenerateRandomName();
        //    }
        //    //здесь заменить потом локалку на домен
        //    string configLink = $"https://localhost:7014/Home/DownloadFile?name={fileName}&password={password}";
        //    string qrLink = $"https://localhost:7014/Home/ShowQRCode?name={fileName}&password={password}";

        //    context.PromoCodes.FirstOrDefault(p => p.Code == "fury");

        //    //Server server = DefineUserProfitServerWireGuard(context);
        //    //CreatePeerWireGuard(fileName, server.Ip, server.Password);
        //    //DownloadFileFromServerWireGuard(fileName, server.Ip, server.Password);


        //    Config config = new Config { FileName = fileName, Password = password, ConfigLink = configLink, qrLink = qrLink };

        //    if (tariff == "freeTrial")
        //    {
        //        FreeTrial freeTrial = new FreeTrial { Email = email, IsUsed = true };
        //        context.FreeTrials.AddAsync(freeTrial);
        //    }

        //    context.Configs.Add(config);
        //    context.SaveChanges();

        //    DateTime dateOfEnd;
        //    switch (tariff)
        //    {
        //        case "month":
        //            dateOfEnd = DateTime.Now.AddDays(30);
        //            break;
        //        case "3month":
        //            dateOfEnd = DateTime.Now.AddDays(90);
        //            break;
        //        case "6month":
        //            dateOfEnd = DateTime.Now.AddDays(180);
        //            break;
        //        case "year":
        //            dateOfEnd = DateTime.Now.AddDays(365);
        //            break;
        //        case "freeTrial":
        //            dateOfEnd = DateTime.Now.AddDays(1);
        //            break;
        //        default:
        //            dateOfEnd = DateTime.Now.AddDays(30);
        //            break;
        //    }

        //    PromoCode promo = context.PromoCodes.FirstOrDefault(c => c.Code == promocode);
        //    if (promo != null) { promo.NumbersOfActivations++; context.Update(promo); context.SaveChanges(); }
        //    int promoCodeId = (promo != null) ? promo.Id : 0;

        //    Config foundConfig = context.Configs.FirstOrDefault(c => c.FileName == fileName);
        //    //сделать бесконечный поиск пока не найдет
        //    while (context.Configs.FirstOrDefault(c => c.FileName == fileName) == null)
        //    {
        //        foundConfig = context.Configs.FirstOrDefault(c => c.FileName == fileName);
        //    }

            
        //    Subscription subscription = new Subscription
        //    {
        //        TelegramId = telegramId,
        //        Email = email,
        //        ConfigId = foundConfig.Id,
        //        DateOfBuy = DateTime.Now,
        //        DateOfEnd = dateOfEnd,
        //        PromoCodeId = promoCodeId
        //    };

        //    context.Subscriptions.AddAsync(subscription);
        //    context.SaveChangesAsync();

        //    SubscriptionResult subscriptionResult = new SubscriptionResult
        //    {
        //        Email = email,
        //        DateOfEnd = dateOfEnd,
        //        ConfigDownloadLink = configLink,
        //        QrDownloadLink = qrLink
        //    };

        //    return subscriptionResult;
            
        //}
        
        ////private Server DefineUserProfitServerWireGuard(ApplicationDbContext context)
        ////{
        ////    var servers = context.Servers.ToArray();
        ////    Server userProfitServer = servers[0];
        ////    int amountOfUsers = CheckAmountOfPeersWireGuard(servers[0].Ip, servers[0].Password);
        ////    foreach (var server in servers)
        ////    {
        ////        int resultOfCheck = CheckAmountOfPeersWireGuard(server.Ip, server.Password);
        ////        if (amountOfUsers > resultOfCheck)
        ////        {
        ////            amountOfUsers = resultOfCheck;
        ////            userProfitServer = server;
        ////        }
        ////    }
        ////    return userProfitServer;
        ////}     

        //private async Task CreatePeerWireGuard(string name, string serverIp, string serverPassword)
        //{
        //    using (var sshClient = new SshClient($"{serverIp}", "root", $"{serverPassword}"))
        //    {
        //        await Task.Run(() => sshClient.Connect());

        //        var command = $"echo -e \"1\\n{name}\\n1\" | sudo bash wireguard.sh";


        //        await Task.Run(() => sshClient.RunCommand(command));


        //        sshClient.Disconnect();
        //    }
        //}
        //private void DeletePeerWireGuard(string name, string serverIp, string serverPassword)
        //{
        //    //здесь еще надо удалить файл и qr
        //    int idOnServer = FindUserByName(name, serverIp, serverPassword);
        //    if (idOnServer == -1)
        //    {
        //        StreamWriter file = new StreamWriter("deletePeerLog.txt", true);
        //        file.WriteLine("\n" + "--------------------------" + DateTime.Now.ToString() + "  \n" + $"Пользователь {name} не найден" + "\n" + "--------" +
        //            "------------------");
        //        file.Close();
        //    }
        //    using (var sshClient = new SshClient($"{serverIp}", "root", $"{serverPassword}"))
        //    {
        //        sshClient.Connect();

        //        var command = $"echo -e \"3\\n{idOnServer}\\ny\" | sudo bash wireguard.sh";


        //        var result = sshClient.RunCommand(command);

        //        Console.WriteLine(result.ToString());


        //        sshClient.Disconnect();
        //    }
        //}
        //private int CheckAmountOfPeersWireGuard(string serverIp, string serverPassword)
        //{
        //    using (var sshClient = new SshClient($"{serverIp}", "root", $"{serverPassword}"))
        //    {
        //        string pattern = @"Total:\s+(\d+)\s+clients";
        //        sshClient.Connect();

        //        var command = $"echo -e \"2\" | sudo bash wireguard.sh";


        //        var result = sshClient.RunCommand(command);

        //        Match match = Regex.Match(result.Result, pattern);
        //        int totalClients = Convert.ToInt32(match.Groups[1].Value);
        //        return totalClients;
        //    }
        //}
        //private int FindUserByName(string name, string serverIp, string serverPassword)
        //{
        //    using (var sshClient = new SshClient($"{serverIp}", "root", $"{serverPassword}"))
        //    {
        //        sshClient.Connect();

        //        // Команда для получения списка клиентов WireGuard и их ID
        //        var command = $"echo -e \"2\" | sudo bash wireguard.sh";
        //        var result = sshClient.RunCommand(command);

        //        // Поиск строки с ID и именем пользователя name в выводе
        //        string pattern = $@"(\d+)\)\s+{name}";
        //        MatchCollection matches = Regex.Matches(result.Result, pattern);

        //        int userId = -1; // Значение -1 будет использовано, если не удастся найти пользователя

        //        foreach (Match match in matches)
        //        {
        //            if (match.Groups.Count > 1)
        //            {
        //                int id;
        //                if (int.TryParse(match.Groups[1].Value, out id))
        //                {
        //                    userId = id;
        //                    break;
        //                }
        //            }
        //        }

        //        return userId;
        //    }
        //}
        //private async Task DownloadFileFromServerWireGuard(string name, string serverIp, string serverPassword)
        //{
        //    // Получаем путь к папке, где находится исполняемый файл
        //    string localPath = Directory.GetCurrentDirectory();
        //    string folderPath = $@"{localPath}\\Configs";

        //    // Используем метод CreateDirectory() из класса Directory, чтобы создать папку
        //    Directory.CreateDirectory(folderPath);


        //    string localFilePath =$"{localPath}\\Configs\\\\{name}.conf";

        //    using (var client = new SshClient($"{serverIp}", "root", $"{serverPassword}"))
        //    {
        //        await Task.Run(() => client.Connect());
        //        string commandToFind = $"find / -name {name}.conf";
        //        string remoteFilePath = client.RunCommand(commandToFind).Result.ToString();
        //        remoteFilePath = remoteFilePath.Replace("\n", "");

        //        // Создаем новый экземпляр класса SftpClient для работы с SFTP
        //        using (var sftp = new SftpClient(client.ConnectionInfo))
        //        {
        //            await Task.Run(() => sftp.Connect());

        //            // Скачиваем файл с удаленного сервера на локальный компьютер
        //            using (var fileStream = System.IO.File.Create(localFilePath))
        //            {
        //               await sftp.DownloadAsync(remoteFilePath, fileStream);
        //            }

        //            sftp.Disconnect();
        //        }
        //    }
        //    //Task.Run(() => GenerateQrCode(name));

        //}
        //private void GenerateQrCode(string name)
        //{
        //    string localPath = Directory.GetCurrentDirectory();
        //    string filePath = $"{localPath}\\Configs\\\\{name}.conf";
        //    string configFileContent = File.ReadAllText(filePath);
        //    Directory.CreateDirectory(localPath + "\\QrCodes");
        //    string qrCodeImagePath = Directory.GetCurrentDirectory() + $"\\wwwroot\\lib\\QrCodes\\{name}.png";


        //    QRCodeLogo qrCodeLogo = new QRCodeLogo(Directory.GetCurrentDirectory() + "\\wwwroot\\lib\\img\\logoForQr.png");
        //    GeneratedBarcode myQRCodeWithLogo = QRCodeWriter.CreateQrCodeWithLogo(configFileContent, qrCodeLogo);
        //    myQRCodeWithLogo.ResizeTo(500, 500).SetMargins(10).ChangeBarCodeColor(IronSoftware.Drawing.Color.Black);
        //    myQRCodeWithLogo.SaveAsPng(qrCodeImagePath);
        //}
    }
}
