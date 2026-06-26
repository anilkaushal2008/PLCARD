using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using PLCARD.Models;
using Microsoft.Extensions.Configuration;

namespace PLCARD.Services
{
    public class EmailNotificationService
    {
        private readonly PLCARDContext _context;
        private readonly IConfiguration _config;

        public EmailNotificationService(PLCARDContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string GetCorporateEmailTemplate(string recipientName, string companyName)
        {
            return $@"<table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color: #f4f5f7; font-family: Arial, sans-serif; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='500' cellpadding='0' cellspacing='0' border='0' style='background-color: #ffffff; border: 1px solid #e0e0e0; border-collapse: separate; border-radius: 16px;'>
                    <tr>
                        <td align='center' style='background-color: #0d6efd; padding: 40px 20px; border-top-left-radius: 16px; border-top-right-radius: 16px;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 22px; font-family: Arial, sans-serif;'>Corporate Partner Registered</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 30px; font-family: Arial, sans-serif;'>
                            <p style='color: #555; font-size: 15px;'>Dear <strong>{recipientName}</strong>,</p>
                            <p style='color: #555; font-size: 15px; margin-bottom: 30px;'>A new corporate partner has been registered in the PLCARD system. <strong>Corporate discounts are now applicable across all Indus Healthcare units.</strong></p>
                            <table width='100%' cellpadding='15' cellspacing='0' border='0' style='background-color: #f8f9fa; border-left: 4px solid #0d6efd;'>
                                <tr>
                                    <td>
                                        <p style='margin: 0; color: #888; font-size: 12px; text-transform: uppercase;'>Company Name</p>
                                        <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #000;'>{companyName}</p>
                                    </td>
                                </tr>
                            </table>
                            <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-top: 30px;'>
                                <tr>
                                    <td align='center'>
                                        <div><a href='#' style='background-color: #0d6efd; color: #ffffff; padding: 12px 25px; text-decoration: none; font-weight: bold; font-size: 14px; border-radius: 25px; display: inline-block; mso-hide:all;'>Login Dashboard For Details</a></div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 20px; text-align: center; background: #fdfdfd; border-top: 1px solid #eee; color: #999; font-size: 11px; border-bottom-left-radius: 16px; border-bottom-right-radius: 16px;'>
                            <p style='margin: 5px 0;'>PLCARD Administration | Automated Notification</p>
                            <p style='margin: 5px 0;'>Please do not reply to this email.</p>
                            <p style='margin: 5px 0;'>&copy; {DateTime.Now.Year} Indus Healthcare Group. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>";
        }


        public async Task SendCorporateNotificationAsync(string companyName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var activeRecipients = await _context.EmailMasters.Where(e => e.IsActive == true).ToListAsync();
            if (!activeRecipients.Any()) return;

            var smtpServer = _config["SmtpSettings:Server"];
            var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];
            var username = _config["SmtpSettings:Username"];
            var password = _config["SmtpSettings:Password"];
            var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");

            foreach (var recipient in activeRecipients)
            {
                bool alreadySent = await _context.EmailLogs.AnyAsync(l => l.RecipientEmail == recipient.EmailAddress && l.CompanyName == companyName && l.SentStatus == "Success");
                if (alreadySent) continue;

                var log = new EmailLogs { CompanyName = companyName, RecipientEmail = recipient.EmailAddress, SentAt = DateTime.Now };

                try
                {
                    using (var message = new MailMessage())
                    {
                        message.To.Add(new MailAddress(recipient.EmailAddress));
                        message.From = new MailAddress(senderEmail, senderName);
                        message.Subject = $"New Corporate Partner: {companyName}";
                        message.Body = GetCorporateEmailTemplate(recipient.RecipientName, companyName); // Used Template Helper
                        message.IsBodyHtml = true;

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
                    log.ErrorMessage = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                }
                _context.EmailLogs.Add(log);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ResendCorporateNotificationAsync(int logId)
        {
            var log = await _context.EmailLogs.FindAsync(logId);
            var recipient = await _context.EmailMasters.FirstOrDefaultAsync(e => e.EmailAddress == log.RecipientEmail);
            if (log == null) return false;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(new MailAddress(log.RecipientEmail));
                    message.From = new MailAddress(_config["SmtpSettings:SenderEmail"], _config["SmtpSettings:SenderName"]);
                    message.Subject = $"New Corporate Partner: {log.CompanyName}";
                    // Correctly using the same template helper
                    message.Body = GetCorporateEmailTemplate(recipient.RecipientName, log.CompanyName);
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_config["SmtpSettings:Server"], int.Parse(_config["SmtpSettings:Port"] ?? "587")))
                    {
                        client.Credentials = new NetworkCredential(_config["SmtpSettings:Username"], _config["SmtpSettings:Password"]);
                        client.EnableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");
                        await client.SendMailAsync(message);
                    }
                }
                log.SentStatus = "Success";
                log.SentAt = DateTime.Now;
                log.ErrorMessage = null;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                log.SentStatus = "Failed";
                log.ErrorMessage = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                await _context.SaveChangesAsync();
                return false;
            }
        }
    }
}