using System;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESP32DataCollector
{
    public class BrevoNotification
    {
        private readonly IConfiguration _configuration;
        private readonly TransactionalEmailsApi _emailApi;
        private readonly TransactionalSMSApi _smsApi; // Changed to TransactionalSMSApi

        public BrevoNotification(IConfiguration configuration)
        {
            _configuration = configuration;

            var apiKey = _configuration["Brevo:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Brevo API key is missing from configuration.");
            }

            // Use a local Configuration instance instead of static Default
            var config = new Configuration();
            config.ApiKey.Add("api-key", apiKey);
            _emailApi = new TransactionalEmailsApi(config);
            _smsApi = new TransactionalSMSApi(config); // Initialize TransactionalSMSApi
        }

        public async System.Threading.Tasks.Task SendEmailAsync(string recipientEmail, string subject, string message)
        {
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Default Sender"; // Fallback if not set

            if (string.IsNullOrEmpty(senderEmail))
            {
                throw new InvalidOperationException("Sender email is missing from EmailSettings.");
            }

            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(senderName, senderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(recipientEmail) },
                subject: subject,
                htmlContent: message
            );

            try
            {
                var result = await _emailApi.SendTransacEmailAsync(sendSmtpEmail);
                Console.WriteLine($"Email sent successfully to {recipientEmail}. Message ID: {result.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {recipientEmail}: {ex.Message}");
                throw; // Rethrow to notify caller
            }
        }

        public async System.Threading.Tasks.Task SendSmsAsync(string recipientPhoneNumber, string message)
        {
            var senderName = _configuration["SmsSettings:SenderName"] ?? "DefaultSender"; // Fallback if not set

            var sendTransacSms = new SendTransacSms(
                sender: senderName,
                recipient: recipientPhoneNumber,
                content: message
            );

            try
            {
                var result = await _smsApi.SendTransacSmsAsync(sendTransacSms);
                Console.WriteLine($"SMS sent successfully to {recipientPhoneNumber}. Message ID: {result.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send SMS to {recipientPhoneNumber}: {ex.Message}");
                throw; // Rethrow to notify caller
            }
        }
    }
}
