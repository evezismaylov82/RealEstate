using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendVerificationEmailAsync(string email, string token, string baseUrl);
        Task SendPasswordResetEmailAsync(string email, string token, string baseUrl);
        Task SendContactMessageNotificationAsync(string adminEmail, string fromName, string fromEmail, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public EmailService(
            string host,
            int port,
            string username,
            string password,
            string fromEmail,
            string fromName,
            bool enableSsl)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _fromEmail = fromEmail;
            _fromName = fromName;
            _enableSsl = enableSsl;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(_host, _port, _enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                await client.AuthenticateAsync(_username, _password);

                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send error: {ex.Message}");
                throw new Exception($"Failed to send email: {ex.Message}");
            }
        }

        public async Task SendVerificationEmailAsync(string email, string token, string baseUrl)
        {
            var verificationUrl = $"{baseUrl}/verify-email?token={token}";

            var subject = "Verify Your Email - Real Estate App";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Welcome to Real Estate App!</h2>
                        <p>Thank you for registering. Please verify your email address by clicking the button below:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{verificationUrl}'
                               style='background-color: #4CAF50; color: white; padding: 12px 30px;
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Verify Email
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <a href='{verificationUrl}'>{verificationUrl}</a>
                        </p>
                        <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                            If you didn't create an account, please ignore this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string email, string token, string baseUrl)
        {
            var resetUrl = $"{baseUrl}/reset-password?token={token}";

            var subject = "Reset Your Password - Real Estate App";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>You requested to reset your password. Click the button below to reset it:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}'
                               style='background-color: #2196F3; color: white; padding: 12px 30px;
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <a href='{resetUrl}'>{resetUrl}</a>
                        </p>
                        <p style='color: #e74c3c; font-size: 14px;'>
                            This link will expire in 24 hours.
                        </p>
                        <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                            If you didn't request a password reset, please ignore this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendContactMessageNotificationAsync(string adminEmail, string fromName, string fromEmail, string message)
        {
            var subject = $"New Contact Message from {fromName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>New Contact Message</h2>
                        <p><strong>From:</strong> {fromName} ({fromEmail})</p>
                        <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'>{message}</p>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            Please respond to this message via your admin panel.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(adminEmail, subject, body);
        }
    }
}
