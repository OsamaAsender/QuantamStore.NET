using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace QuantamStore.Webapi.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtpClient = new SmtpClient(_config["Smtp:Host"])
            {
                Port = int.Parse(_config["Smtp:Port"]),
                Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }

       public async Task SendResetPasswordEmail(string toEmail, string resetToken, DateTime expiresAt)
{
    var resetLink = $"https://yourdomain.com/reset-password?token={resetToken}";
    var expiryFormatted = expiresAt.ToString("f"); // e.g., "Saturday, 11 October 2025 10:45 AM"

    var subject = "Reset Your Password";
    var body = $@"
        You requested a password reset.

        Click the link below to reset your password:
        {resetLink}

        ⚠️ This link will expire on: {expiryFormatted} (your local time)

        If you didn’t request this, you can safely ignore this email.
    ";

    await SendAsync(toEmail, subject, body);
}

    }
}
