using Backend_Api.Services;
using System.Net;
using System.Net.Mail;

namespace Backend_Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient email is required.", nameof(to));
            
            string host = _config["Email:Host"] ?? throw new InvalidOperationException("Email host not configured");
            string username = _config["Email:Username"] ?? throw new InvalidOperationException("Email username not configured");
            string password = _config["Email:Password"] ?? throw new InvalidOperationException("Email password not configured");
            int port = int.TryParse(_config["Email:Port"], out var p) ? p : 587;

            using var smtp = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(username),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);
            await smtp.SendMailAsync(mail);
        }
    }
}
