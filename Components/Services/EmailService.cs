using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace PDTActivityTracker.Components.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otpCode);
        Task SendRoleRequestEmailAsync(IEnumerable<string> adminEmails, string userName, string userEmail, string requestedRole, string remarks);
        Task SendRoleApprovedEmailAsync(string toEmail, string userName, string newRole);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["FromName"] ?? "SMR Activity Tracker",
                emailSettings["FromEmail"]!
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Password Reset - Verification Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Password Reset Verification</h2>
                    <p>Your verification code is:</p>
                    <h1 style='color: #7e6fff; font-size: 36px; letter-spacing: 8px;'>{otpCode}</h1>
                    <p>This code will expire in 5 minutes.</p>
                    <p style='color: #666;'>If you didn't request this, please ignore this email.</p>
                </div>
            "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                bool.TryParse(emailSettings["EnableSsl"], out var enableSsl);
                var options = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;

                await client.ConnectAsync(
                    emailSettings["Host"] ?? "localhost",
                    int.Parse(emailSettings["Port"] ?? "25"),
                    options
                );

                if (!string.IsNullOrWhiteSpace(emailSettings["Password"]))
                {
                    await client.AuthenticateAsync(
                        emailSettings["Username"],
                        emailSettings["Password"]
                    );
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }

        public async Task SendRoleApprovedEmailAsync(string toEmail, string userName, string newRole)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["FromName"] ?? "SMR Activity Tracker",
                emailSettings["FromEmail"]!
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Role Upgrade Approved";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #2e7d32;'>Role Request Approved</h2>
                    <p>Dear {userName},</p>
                    <p>Your request for a role upgrade has been approved. Your account has been promoted to:</p>
                    <h2 style='color: #0072cf;'>{newRole}</h2>
                    <p>Please log out and log back in to your account to apply the new permissions.</p>
                    <p style='color: #666; margin-top: 30px;'>Thank you,<br/>SMR Activity Tracker Team</p>
                </div>
            "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                bool.TryParse(emailSettings["EnableSsl"], out var enableSsl);
                var options = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;

                await client.ConnectAsync(
                    emailSettings["Host"] ?? "localhost",
                    int.Parse(emailSettings["Port"] ?? "25"),
                    options
                );

                if (!string.IsNullOrWhiteSpace(emailSettings["Password"]))
                {
                    await client.AuthenticateAsync(
                        emailSettings["Username"],
                        emailSettings["Password"]
                    );
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }

        public async Task SendRoleRequestEmailAsync(IEnumerable<string> adminEmails, string userName, string userEmail, string requestedRole, string remarks)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            using var client = new SmtpClient();

            try
            {
                bool.TryParse(emailSettings["EnableSsl"], out var enableSsl);
                var options = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;

                await client.ConnectAsync(
                    emailSettings["Host"] ?? "localhost",
                    int.Parse(emailSettings["Port"] ?? "25"),
                    options
                );

                if (!string.IsNullOrWhiteSpace(emailSettings["Password"]))
                {
                    await client.AuthenticateAsync(
                        emailSettings["Username"],
                        emailSettings["Password"]
                    );
                }

                int successCount = 0;
                var errors = new List<string>();

                foreach (var adminEmail in adminEmails.Distinct())
                {
                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(
                            emailSettings["FromName"] ?? "SMR Activity Tracker",
                            emailSettings["FromEmail"]!
                        ));
                        message.To.Add(new MailboxAddress("", adminEmail));
                        message.Subject = $"Role Request: {requestedRole} for {userName}";

                        var bodyBuilder = new BodyBuilder
                        {
                            HtmlBody = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                                <h2 style='color: #0072cf;'>User Role Request</h2>
                                <p>A user has requested an elevated role. Please review their details below:</p>
                                <table style='width: 100%; max-width: 600px; border-collapse: collapse; margin-top: 20px;'>
                                    <tr>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee; font-weight: bold; width: 30%;'>User Fullname:</td>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{userName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee; font-weight: bold;'>User Email:</td>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><a href='mailto:{userEmail}'>{userEmail}</a></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee; font-weight: bold;'>Requested Role:</td>
                                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span style='background-color: #0072cf; color: white; padding: 4px 8px; border-radius: 12px; font-weight: bold;'>{requestedRole}</span></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px; font-weight: bold; vertical-align: top;'>Remarks:</td>
                                        <td style='padding: 10px;'>{(string.IsNullOrWhiteSpace(remarks) ? "<i>No remarks provided.</i>" : remarks)}</td>
                                    </tr>
                                </table>
                                <p style='margin-top: 30px; font-size: 14px; color: #666;'>Log in to the <b>SMR Activity Tracker</b> User Management Page to update this user's role.</p>
                            </div>
                        "
                        };

                        message.Body = bodyBuilder.ToMessageBody();
                        await client.SendAsync(message);
                        successCount++;
                    }
                    catch (SmtpCommandException ex)
                    {
                        // Specifically catch recipient errors (like 5.1.1) and continue
                        errors.Add($"{adminEmail}: {ex.Message}");
                        Console.WriteLine($"Mail delivery failed for {adminEmail}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{adminEmail}: {ex.Message}");
                        Console.WriteLine($"Unexpected error sending to {adminEmail}: {ex.Message}");
                    }
                }

                await client.DisconnectAsync(true);

                // If we failed to send to ANYONE, throw an exception
                if (successCount == 0 && adminEmails.Any())
                {
                    throw new Exception($"Failed to send to all recipients. Errors: {string.Join("; ", errors)}");
                }
            }
            catch (Exception ex) when (!(ex is Exception && ex.Message.StartsWith("Failed to send")))
            {
                // Catch connection/auth errors
                throw new Exception($"SMTP Connection Error: {ex.Message}", ex);
            }
        }
    }
}