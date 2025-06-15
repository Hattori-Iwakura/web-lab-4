using web_lab_4.Models;

namespace web_lab_4.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, Order order);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendOrderStatusUpdateEmailAsync(string toEmail, string customerName, Order order, string oldStatus);
    }
}