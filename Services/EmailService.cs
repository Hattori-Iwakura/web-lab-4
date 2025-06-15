using System.Net;
using System.Net.Mail;
using System.Text;
using web_lab_4.Models;

namespace web_lab_4.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, Order order)
        {
            try
            {
                var subject = $"Order Confirmation - Order #{order.Id}";
                var htmlBody = GenerateOrderConfirmationEmailTemplate(customerName, order);
                
                await SendEmailAsync(toEmail, subject, htmlBody);
                _logger.LogInformation($"Order confirmation email sent to {toEmail} for order {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order confirmation email to {toEmail}");
            }
        }

        public async Task SendOrderStatusUpdateEmailAsync(string toEmail, string customerName, Order order, string oldStatus)
        {
            try
            {
                var subject = $"Order Status Update - Order #{order.Id}";
                var htmlBody = GenerateOrderStatusUpdateEmailTemplate(customerName, order, oldStatus);
                
                await SendEmailAsync(toEmail, subject, htmlBody);
                _logger.LogInformation($"Order status update email sent to {toEmail} for order {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order status update email to {toEmail}");
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");
            
            // Check if email settings are configured
            if (string.IsNullOrEmpty(smtpSettings["SmtpServer"]) || 
                string.IsNullOrEmpty(smtpSettings["Username"]) || 
                string.IsNullOrEmpty(smtpSettings["Password"]))
            {
                _logger.LogWarning("Email settings not configured properly. Skipping email send.");
                return;
            }

            using var client = new SmtpClient(smtpSettings["SmtpServer"])
            {
                Port = int.Parse(smtpSettings["Port"] ?? "587"),
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        private string GenerateOrderConfirmationEmailTemplate(string customerName, Order order)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Order Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; }}
        .order-info {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .product-item {{ border-bottom: 1px solid #eee; padding: 15px 0; }}
        .product-item:last-child {{ border-bottom: none; }}
        .total {{ font-size: 18px; font-weight: bold; color: #007bff; text-align: right; margin-top: 20px; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Order Confirmation</h1>
            <p>Thank you for your order!</p>
        </div>
        
        <div class='content'>
            <h2>Hello {customerName},</h2>
            <p>We're excited to let you know that we've received your order and it's being processed.</p>
            
            <div class='order-info'>
                <h3>Order Details</h3>
                <p><strong>Order Number:</strong> #{order.Id}</p>
                <p><strong>Order Date:</strong> {order.OrderDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                <p><strong>Status:</strong> {order.Status}</p>");

            if (!string.IsNullOrEmpty(order.ShippingAddress))
            {
                sb.AppendLine($"<p><strong>Shipping Address:</strong><br>{order.ShippingAddress.Replace("\n", "<br>")}</p>");
            }

            if (!string.IsNullOrEmpty(order.Notes))
            {
                sb.AppendLine($"<p><strong>Notes:</strong> {order.Notes}</p>");
            }

            sb.AppendLine(@"
            </div>
            
            <h3>Order Items</h3>");

            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var item in order.OrderDetails)
                {
                    sb.AppendLine($@"
            <div class='product-item'>
                <div style='display: flex; justify-content: space-between; align-items: center;'>
                    <div>
                        <strong>{item.ProductName}</strong><br>
                        <span style='color: #666;'>Quantity: {item.Quantity}</span>");

                    if (!string.IsNullOrEmpty(item.Flavor))
                    {
                        sb.AppendLine($"<br><span style='color: #666;'>Flavor: {item.Flavor}</span>");
                    }

                    if (item.Weight > 0)
                    {
                        sb.AppendLine($"<br><span style='color: #666;'>Weight: {item.Weight} {item.WeightUnit}</span>");
                    }

                    sb.AppendLine($@"
                    </div>
                    <div style='text-align: right;'>
                        <strong>${item.Price:F2} each</strong><br>
                        <span style='color: #666;'>Total: ${(item.Price * item.Quantity):F2}</span>
                    </div>
                </div>
            </div>");
                }
            }

            sb.AppendLine($@"
            <div class='total'>
                <p>Order Total: ${order.TotalPrice:F2}</p>
            </div>
            
            <p>We'll send you another email with tracking information once your order ships.</p>
            <p>If you have any questions about your order, please don't hesitate to contact us.</p>
        </div>
        
        <div class='footer'>
            <p>Thank you for shopping with us!</p>
            <p>&copy; 2025 Fit Supplement Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        private string GenerateOrderStatusUpdateEmailTemplate(string customerName, Order order, string oldStatus)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Order Status Update</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; }}
        .status-update {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Order Status Update</h1>
        </div>
        
        <div class='content'>
            <h2>Hello {customerName},</h2>
            <p>Your order status has been updated!</p>
            
            <div class='status-update'>
                <h3>Order #{order.Id}</h3>
                <p><strong>Previous Status:</strong> {oldStatus}</p>
                <p><strong>Current Status:</strong> {order.Status}</p>
                <p><strong>Updated:</strong> {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
            </div>
            
            <p>Thank you for your patience!</p>
        </div>
        
        <div class='footer'>
            <p>&copy; 2025 Fit Supplement Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}