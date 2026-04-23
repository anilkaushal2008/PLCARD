using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using PLCARD.Models;
using Microsoft.Extensions.Configuration; // Required for IConfiguration

namespace PLCARD.Services
{
    public class EmailNotificationService
    {
        private readonly PLCARDContext _context;
        private readonly IConfiguration _config;

        // Inject both Context and Configuration
        public EmailNotificationService(PLCARDContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task SendCorporateNotificationAsync(string companyName)
        {
            // 1. Fetch only ACTIVE recipients
            var activeRecipients = await _context.EmailMasters
                .Where(e => e.IsActive == true)
                .ToListAsync();

            if (!activeRecipients.Any()) return;

            // Get SMTP settings from appsettings.json
            var smtpServer = _config["SmtpSettings:Server"];
            var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];
            var username = _config["SmtpSettings:Username"];
            var password = _config["SmtpSettings:Password"];
            var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");

            foreach (var recipient in activeRecipients)
            {
                var log = new EmailLogs
                {
                    CompanyName = companyName,
                    RecipientEmail = recipient.EmailAddress,
                    SentAt = DateTime.Now
                };

                try
                {
                    using (var message = new MailMessage())
                    {
                        message.To.Add(new MailAddress(recipient.EmailAddress));
                        message.From = new MailAddress(senderEmail, senderName);
                        message.Subject = $"New Corporate Partner: {companyName}";
                        message.Body = $"Dear {recipient.RecipientName},\n\nA new corporate company '{companyName}' has been registered in the PLCARD system. Corporate discounts are now applicable for this client.";
                        message.IsBodyHtml = false;
                        using (var client = new SmtpClient(smtpServer, smtpPort))
                        {
                            client.Credentials = new NetworkCredential(username, password);
                            client.EnableSsl = enableSsl;
                            await client.SendMailAsync(message);
                        }
                    }

                    log.SentStatus = "Success";
                }
                catch (Exception ex)
                {
                    log.SentStatus = "Failed";
                    // Truncate error message if it's too long for the DB column
                    log.ErrorMessage = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                }

                _context.EmailLogs.Add(log);
            }

            // Save all logs at once after the loop
            await _context.SaveChangesAsync();
        }
    }
}