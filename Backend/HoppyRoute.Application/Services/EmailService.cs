using HoppyRoute.Application.Interfaces;
using HoppyRoute.Application.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace HoppyRoute.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendTemporaryPasswordEmailAsync(string email, string firstName, string lastName, string username, string temporaryPassword)
        {
            try
            {
                var subject = "Welkom bij Hoppy Route - Uw tijdelijke inloggegevens";
                var body = GenerateTemporaryPasswordEmailBody(firstName, lastName, username, temporaryPassword);

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het verzenden van tijdelijke wachtwoord email naar {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetLink)
        {
            try
            {
                var subject = "Hoppy Route - Wachtwoord Reset";
                var body = GeneratePasswordResetEmailBody(firstName, resetLink);

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het verzenden van wachtwoord reset email naar {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName)
        {
            try
            {
                var subject = "Welkom bij Hoppy Route!";
                var body = GenerateWelcomeEmailBody(firstName, lastName);

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het verzenden van welkomst email naar {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    Timeout = _emailSettings.TimeoutSeconds * 1000
                };

                using var message = new MailMessage()
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                message.To.Add(toEmail);

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation("Email succesvol verzonden naar {Email} met onderwerp: {Subject}", toEmail, subject);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP fout bij het verzenden van email naar {Email}: {Error}", toEmail, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onbekende fout bij het verzenden van email naar {Email}: {Error}", toEmail, ex.Message);
                return false;
            }
        }

        private string GenerateTemporaryPasswordEmailBody(string firstName, string lastName, string username, string temporaryPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welkom bij Hoppy Route</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .credentials {{ background-color: #e9ecef; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .button {{ background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 40px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚲 Hoppy Route</h1>
            <p>Welkom bij ons team!</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName} {lastName},</h2>
            <p>Welkom bij Hoppy Route! Uw account is succesvol aangemaakt door een administrator.</p>
            
            <div class='credentials'>
                <h3>📧 Uw inloggegevens:</h3>
                <p><strong>Gebruikersnaam:</strong> {username}</p>
                <p><strong>Tijdelijk wachtwoord:</strong> {temporaryPassword}</p>
            </div>
            
            <div class='warning'>
                <h4>⚠️ Belangrijk:</h4>
                <ul>
                    <li>Dit is een tijdelijk wachtwoord dat <strong>7 dagen</strong> geldig is</li>
                    <li>Bij uw eerste login wordt u gevraagd een nieuw wachtwoord in te stellen</li>
                    <li>Bewaar deze gegevens veilig en deel ze niet met anderen</li>
                </ul>
            </div>
            
            <p>U kunt nu inloggen in de Hoppy Route app met bovenstaande gegevens.</p>
            <p>Na het eerste inloggen kunt u direct aan de slag met het beheren van de elektrische steps!</p>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht. Reageer niet op deze email.</p>
            <p>© 2025 Hoppy Route. Alle rechten voorbehouden.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailBody(string firstName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Wachtwoord Reset - Hoppy Route</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .button {{ background-color: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 40px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚲 Hoppy Route</h1>
            <p>Wachtwoord Reset</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName},</h2>
            <p>U heeft een wachtwoord reset aangevraagd voor uw Hoppy Route account.</p>
            
            <p>Klik op onderstaande knop om uw wachtwoord opnieuw in te stellen:</p>
            
            <a href='{resetLink}' class='button'>Wachtwoord Resetten</a>
            
            <div class='warning'>
                <h4>⚠️ Belangrijk:</h4>
                <ul>
                    <li>Deze link is <strong>24 uur</strong> geldig</li>
                    <li>Heeft u deze reset niet aangevraagd? Dan kunt u deze email negeren</li>
                    <li>Uw huidige wachtwoord blijft geldig tot u een nieuw wachtwoord instelt</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht. Reageer niet op deze email.</p>
            <p>© 2025 Hoppy Route. Alle rechten voorbehouden.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateWelcomeEmailBody(string firstName, string lastName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welkom bij Hoppy Route</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .features {{ background-color: #e8f5e8; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 40px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚲 Hoppy Route</h1>
            <p>Welkom bij ons team!</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName} {lastName},</h2>
            <p>Welkom bij Hoppy Route! We zijn blij dat u deel uitmaakt van ons team.</p>
            
            <div class='features'>
                <h3>🎯 Wat kunt u verwachten:</h3>
                <ul>
                    <li>Efficiënte routeplanning voor batterijvervanging</li>
                    <li>Real-time overzicht van step locaties</li>
                    <li>Gebruiksvriendelijke interface</li>
                    <li>Ondersteuning van ons team</li>
                </ul>
            </div>
            
            <p>U heeft nu toegang tot de Hoppy Route app en kunt direct aan de slag!</p>
            <p>Heeft u vragen? Neem dan contact op met uw administrator.</p>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht. Reageer niet op deze email.</p>
            <p>© 2025 Hoppy Route. Alle rechten voorbehouden.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
