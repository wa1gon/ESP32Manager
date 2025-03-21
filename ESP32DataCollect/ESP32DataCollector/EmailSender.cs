namespace ESP32DataCollector;

using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

public class EmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string message)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        string senderEmail = emailSettings["SenderEmail"];
        string smtpServer = emailSettings["SmtpServer"];
        string smtpPort = emailSettings["SmtpPort"];
        string senderPassword = emailSettings["SenderPassword"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(smtpServer) ||
            string.IsNullOrEmpty(smtpPort) || string.IsNullOrEmpty(senderPassword))
        {
            throw new InvalidOperationException("Missing required email settings.");
        }
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("Sender Name", emailSettings["SenderEmail"]));
        email.To.Add(new MailboxAddress("", recipientEmail));
        email.Subject = subject;

        email.Body = new TextPart("plain")
        {
            Text = message
        };

        using (var client = new SmtpClient())
        {
            try
            {
                // var emailserver = smtpServer;// emailSettings["SmtpServer"];
                // var port = Convert.ToInt32(smtpPort);// Convert.ToInt32(emailSettings["SmtpPort"]);
                // var username = senderEmail;// emailSettings["SmtpUsername"];
                // var password = senderPassword;//["SmtpPassword"];
                await client.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]), false);
                await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["SenderPassword"]);
                await client.SendAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending email: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
