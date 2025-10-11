namespace QuantamStore.Webapi.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendResetPasswordEmail(string toEmail, string resetToken,DateTime expiresAt);
    }
}
