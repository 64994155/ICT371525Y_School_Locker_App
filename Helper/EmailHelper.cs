using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ICT371525Y_School_Locker_App.Helper
{
    public static class EmailHelper
    {
        public static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Build config manually since this is a static class
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Important for non-web apps
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var fromEmail = config["EmailSettings:FromEmail"];
            var fromName = config["EmailSettings:FromName"];
            var fromPassword = config["EmailSettings:Password"];

            var fromAddress = new MailAddress(fromEmail, fromName);
            var toAddress = new MailAddress(toEmail);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                Timeout = 20000
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            };
            await smtp.SendMailAsync(message);
        }
    }
}