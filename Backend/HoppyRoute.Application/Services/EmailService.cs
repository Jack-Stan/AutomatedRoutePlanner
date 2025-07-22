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

                // Log email details for development
                _logger.LogInformation("=== EMAIL WORDT VERSTUURD ===");
                _logger.LogInformation("Naar: {Email}", email);
                _logger.LogInformation("Onderwerp: {Subject}", subject);
                _logger.LogInformation("Gebruikersnaam: {Username}", username);
                _logger.LogInformation("Tijdelijk wachtwoord: {Password}", temporaryPassword);
                _logger.LogInformation("===========================");
                
                // Check if we have valid SMTP settings before trying to send
                if (string.IsNullOrEmpty(_emailSettings.SmtpHost) || 
                    string.IsNullOrEmpty(_emailSettings.SmtpUsername) || 
                    _emailSettings.SmtpUsername == "test@gmail.com")
                {
                    _logger.LogWarning("SMTP instellingen niet geconfigureerd. Email wordt alleen gelogd.");
                    // Simulate async operation for development
                    await Task.Delay(100);
                    return true;
                }

                // Try to send actual email
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
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        
        body {{ 
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; 
            line-height: 1.6; 
            color: #2D3748; 
            margin: 0; 
            padding: 0;
            background-color: #F7FAFC;
        }}
        
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 20px; 
            background-color: #FFFFFF;
            border-radius: 12px;
            box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.15);
        }}
        
        .header {{ 
            background: linear-gradient(135deg, #39C5A3 0%, #46DAA5 100%);
            color: white; 
            padding: 40px 30px; 
            text-align: center; 
            border-radius: 12px 12px 0 0;
            margin: -20px -20px 20px -20px;
        }}
        
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
            letter-spacing: -0.02em;
        }}
        
        .header p {{
            margin: 8px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
            font-weight: 500;
        }}
        
        .logo {{ 
            width: 60px; 
            height: 60px; 
            background-color: rgba(255,255,255,0.2); 
            border-radius: 16px; 
            display: inline-flex; 
            align-items: center; 
            justify-content: center; 
            font-size: 30px; 
            margin-bottom: 16px;
        }}
        
        .content {{ 
            padding: 0 30px; 
            color: #2D3748;
        }}
        
        .content h2 {{
            color: #2D3748;
            font-size: 24px;
            font-weight: 600;
            margin: 0 0 24px 0;
        }}
        
        .credentials {{ 
            background: linear-gradient(135deg, #EDF2F7 0%, #E2E8F0 100%);
            padding: 24px; 
            border-radius: 12px; 
            margin: 24px 0;
            border-left: 4px solid #39C5A3;
        }}
        
        .credentials h3 {{
            color: #2D3748;
            font-size: 18px;
            font-weight: 600;
            margin: 0 0 16px 0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .credential-item {{
            background-color: #FFFFFF;
            padding: 12px 16px;
            border-radius: 8px;
            margin: 8px 0;
            border: 1px solid #CBD5E0;
        }}
        
        .credential-label {{
            color: #718096;
            font-size: 14px;
            font-weight: 500;
            margin-bottom: 4px;
        }}
        
        .credential-value {{
            color: #2D3748;
            font-size: 16px;
            font-weight: 600;
            font-family: 'Courier New', monospace;
        }}
        
        .warning {{ 
            background: linear-gradient(135deg, #FFF3CD 0%, #FFF8E1 100%);
            border: 1px solid #D69E2E; 
            padding: 20px; 
            border-radius: 12px; 
            margin: 24px 0;
            border-left: 4px solid #D69E2E;
        }}
        
        .warning h4 {{
            color: #B7791F;
            font-size: 16px;
            font-weight: 600;
            margin: 0 0 12px 0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .warning ul {{
            margin: 0;
            padding-left: 20px;
            color: #975A16;
        }}
        
        .warning li {{
            margin-bottom: 8px;
        }}
        
        .action-button {{ 
            background: linear-gradient(135deg, #39C5A3 0%, #46DAA5 100%);
            color: white; 
            padding: 16px 32px; 
            text-decoration: none; 
            border-radius: 8px; 
            display: inline-block; 
            margin: 24px 0;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0px 2px 4px rgba(57, 197, 163, 0.3);
            transition: all 0.2s ease;
        }}
        
        .action-button:hover {{
            transform: translateY(-1px);
            box-shadow: 0px 4px 8px rgba(57, 197, 163, 0.4);
        }}
        
        .footer {{ 
            text-align: center; 
            color: #718096; 
            font-size: 14px; 
            margin-top: 40px; 
            padding-top: 24px;
            border-top: 1px solid #E2E8F0;
        }}
        
        .footer p {{
            margin: 8px 0;
        }}
        
        .brand-name {{
            color: #39C5A3;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🚲</div>
            <h1><span class='brand-name'>Hoppy</span> Route</h1>
            <p>Welkom bij ons team!</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName} {lastName},</h2>
            <p>Welkom bij Hoppy Route! Uw account is succesvol aangemaakt door een administrator en u kunt nu direct aan de slag.</p>
            
            <div class='credentials'>
                <h3>� Uw inloggegevens</h3>
                <div class='credential-item'>
                    <div class='credential-label'>Gebruikersnaam</div>
                    <div class='credential-value'>{username}</div>
                </div>
                <div class='credential-item'>
                    <div class='credential-label'>Tijdelijk wachtwoord</div>
                    <div class='credential-value'>{temporaryPassword}</div>
                </div>
            </div>
            
            <div class='warning'>
                <h4>⚠️ Belangrijk om te weten</h4>
                <ul>
                    <li>Dit tijdelijke wachtwoord is <strong>7 dagen</strong> geldig</li>
                    <li>Bij uw eerste login wordt u gevraagd een nieuw, persoonlijk wachtwoord in te stellen</li>
                    <li>Bewaar deze gegevens veilig en deel ze niet met anderen</li>
                    <li>Na het instellen van uw nieuwe wachtwoord heeft u volledige toegang tot de Hoppy Route app</li>
                </ul>
            </div>
            
            <p>U kunt nu inloggen in de Hoppy Route app en direct beginnen met het efficiënt beheren van onze elektrische steps. Ons systeem helpt u bij het optimaliseren van batterijvervanging routes!</p>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht van <span class='brand-name'>Hoppy Route</span>.</p>
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
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        
        body {{ 
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; 
            line-height: 1.6; 
            color: #2D3748; 
            margin: 0; 
            padding: 0;
            background-color: #F7FAFC;
        }}
        
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 20px; 
            background-color: #FFFFFF;
            border-radius: 12px;
            box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.15);
        }}
        
        .header {{ 
            background: linear-gradient(135deg, #E53E3E 0%, #C53030 100%);
            color: white; 
            padding: 40px 30px; 
            text-align: center; 
            border-radius: 12px 12px 0 0;
            margin: -20px -20px 20px -20px;
        }}
        
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
            letter-spacing: -0.02em;
        }}
        
        .header p {{
            margin: 8px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
            font-weight: 500;
        }}
        
        .logo {{ 
            width: 60px; 
            height: 60px; 
            background-color: rgba(255,255,255,0.2); 
            border-radius: 16px; 
            display: inline-flex; 
            align-items: center; 
            justify-content: center; 
            font-size: 30px; 
            margin-bottom: 16px;
        }}
        
        .content {{ 
            padding: 0 30px; 
            color: #2D3748;
        }}
        
        .content h2 {{
            color: #2D3748;
            font-size: 24px;
            font-weight: 600;
            margin: 0 0 24px 0;
        }}
        
        .action-button {{ 
            background: linear-gradient(135deg, #E53E3E 0%, #C53030 100%);
            color: white; 
            padding: 16px 32px; 
            text-decoration: none; 
            border-radius: 8px; 
            display: inline-block; 
            margin: 24px 0;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0px 2px 4px rgba(229, 62, 62, 0.3);
            transition: all 0.2s ease;
        }}
        
        .action-button:hover {{
            transform: translateY(-1px);
            box-shadow: 0px 4px 8px rgba(229, 62, 62, 0.4);
        }}
        
        .warning {{ 
            background: linear-gradient(135deg, #FFF3CD 0%, #FFF8E1 100%);
            border: 1px solid #D69E2E; 
            padding: 20px; 
            border-radius: 12px; 
            margin: 24px 0;
            border-left: 4px solid #D69E2E;
        }}
        
        .warning h4 {{
            color: #B7791F;
            font-size: 16px;
            font-weight: 600;
            margin: 0 0 12px 0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .warning ul {{
            margin: 0;
            padding-left: 20px;
            color: #975A16;
        }}
        
        .warning li {{
            margin-bottom: 8px;
        }}
        
        .footer {{ 
            text-align: center; 
            color: #718096; 
            font-size: 14px; 
            margin-top: 40px; 
            padding-top: 24px;
            border-top: 1px solid #E2E8F0;
        }}
        
        .footer p {{
            margin: 8px 0;
        }}
        
        .brand-name {{
            color: #39C5A3;
            font-weight: 600;
        }}
        
        .security-info {{
            background: linear-gradient(135deg, #E8F5E8 0%, #F0FDF4 100%);
            border-left: 4px solid #38A169;
            padding: 20px;
            border-radius: 12px;
            margin: 24px 0;
        }}
        
        .security-info h4 {{
            color: #2F855A;
            font-size: 16px;
            font-weight: 600;
            margin: 0 0 12px 0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🔒</div>
            <h1><span class='brand-name'>Hoppy</span> Route</h1>
            <p>Wachtwoord Reset</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName},</h2>
            <p>U heeft een wachtwoord reset aangevraagd voor uw Hoppy Route account. Geen zorgen, dit gebeurt wel vaker!</p>
            
            <p>Klik op onderstaande knop om veilig een nieuw wachtwoord in te stellen:</p>
            
            <a href='{resetLink}' class='action-button'>🔑 Nieuw Wachtwoord Instellen</a>
            
            <div class='security-info'>
                <h4>🛡️ Voor uw veiligheid</h4>
                <p>Deze reset link brengt u naar een beveiligde pagina waar u uw nieuwe wachtwoord kunt instellen. Uw huidige wachtwoord blijft geldig tot u een nieuw wachtwoord heeft ingesteld.</p>
            </div>
            
            <div class='warning'>
                <h4>⚠️ Belangrijk om te weten</h4>
                <ul>
                    <li>Deze reset link is <strong>24 uur</strong> geldig</li>
                    <li>Heeft u deze reset niet aangevraagd? Dan kunt u deze email veilig negeren</li>
                    <li>De link kan slechts één keer gebruikt worden</li>
                    <li>Zorg ervoor dat uw nieuwe wachtwoord sterk en uniek is</li>
                </ul>
            </div>
            
            <p>Na het instellen van uw nieuwe wachtwoord kunt u direct weer aan de slag met de Hoppy Route app.</p>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht van <span class='brand-name'>Hoppy Route</span>.</p>
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
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        
        body {{ 
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; 
            line-height: 1.6; 
            color: #2D3748; 
            margin: 0; 
            padding: 0;
            background-color: #F7FAFC;
        }}
        
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 20px; 
            background-color: #FFFFFF;
            border-radius: 12px;
            box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.15);
        }}
        
        .header {{ 
            background: linear-gradient(135deg, #38A169 0%, #46DAA5 100%);
            color: white; 
            padding: 40px 30px; 
            text-align: center; 
            border-radius: 12px 12px 0 0;
            margin: -20px -20px 20px -20px;
        }}
        
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
            letter-spacing: -0.02em;
        }}
        
        .header p {{
            margin: 8px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
            font-weight: 500;
        }}
        
        .logo {{ 
            width: 60px; 
            height: 60px; 
            background-color: rgba(255,255,255,0.2); 
            border-radius: 16px; 
            display: inline-flex; 
            align-items: center; 
            justify-content: center; 
            font-size: 30px; 
            margin-bottom: 16px;
        }}
        
        .content {{ 
            padding: 0 30px; 
            color: #2D3748;
        }}
        
        .content h2 {{
            color: #2D3748;
            font-size: 24px;
            font-weight: 600;
            margin: 0 0 24px 0;
        }}
        
        .features {{ 
            background: linear-gradient(135deg, #E8F5E8 0%, #F0FDF4 100%);
            padding: 24px; 
            border-radius: 12px; 
            margin: 24px 0;
            border-left: 4px solid #38A169;
        }}
        
        .features h3 {{
            color: #2F855A;
            font-size: 18px;
            font-weight: 600;
            margin: 0 0 16px 0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        
        .feature-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 16px;
            margin-top: 16px;
        }}
        
        .feature-item {{
            background-color: #FFFFFF;
            padding: 16px;
            border-radius: 8px;
            border: 1px solid #CBD5E0;
        }}
        
        .feature-icon {{
            font-size: 24px;
            margin-bottom: 8px;
        }}
        
        .feature-title {{
            font-weight: 600;
            color: #2D3748;
            margin-bottom: 4px;
        }}
        
        .feature-desc {{
            color: #718096;
            font-size: 14px;
        }}
        
        .footer {{ 
            text-align: center; 
            color: #718096; 
            font-size: 14px; 
            margin-top: 40px; 
            padding-top: 24px;
            border-top: 1px solid #E2E8F0;
        }}
        
        .footer p {{
            margin: 8px 0;
        }}
        
        .brand-name {{
            color: #39C5A3;
            font-weight: 600;
        }}
        
        .highlight-box {{
            background: linear-gradient(135deg, #39C5A3 0%, #46DAA5 100%);
            color: white;
            padding: 20px;
            border-radius: 12px;
            margin: 24px 0;
            text-align: center;
        }}
        
        .highlight-box h3 {{
            margin: 0 0 8px 0;
            font-size: 18px;
            font-weight: 600;
        }}
        
        .highlight-box p {{
            margin: 0;
            opacity: 0.9;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🎉</div>
            <h1><span class='brand-name'>Hoppy</span> Route</h1>
            <p>Welkom bij ons team!</p>
        </div>
        
        <div class='content'>
            <h2>Hallo {firstName} {lastName},</h2>
            <p>Van harte welkom bij Hoppy Route! We zijn enorm blij dat u deel uitmaakt van ons groeiende team. U speelt een belangrijke rol in het duurzame mobiliteitsecosysteem van Europa.</p>
            
            <div class='highlight-box'>
                <h3>🚀 U bent nu klaar om te beginnen!</h3>
                <p>Uw account is volledig geconfigureerd en u heeft toegang tot alle functies van onze platform.</p>
            </div>
            
            <div class='features'>
                <h3>🎯 Wat kunt u verwachten van Hoppy Route</h3>
                
                <div class='feature-grid'>
                    <div class='feature-item'>
                        <div class='feature-icon'>📍</div>
                        <div class='feature-title'>Slimme Routeplanning</div>
                        <div class='feature-desc'>Geoptimaliseerde routes voor efficiënte batterijvervanging</div>
                    </div>
                    
                    <div class='feature-item'>
                        <div class='feature-icon'>⚡</div>
                        <div class='feature-title'>Real-time Monitoring</div>
                        <div class='feature-desc'>Live overzicht van step locaties en batterijstatus</div>
                    </div>
                    
                    <div class='feature-item'>
                        <div class='feature-icon'>📱</div>
                        <div class='feature-title'>Gebruiksvriendelijke App</div>
                        <div class='feature-desc'>Intuïtieve interface voor al uw dagelijkse taken</div>
                    </div>
                    
                    <div class='feature-item'>
                        <div class='feature-icon'>🤝</div>
                        <div class='feature-title'>Team Ondersteuning</div>
                        <div class='feature-desc'>Ons team staat altijd klaar om u te helpen</div>
                    </div>
                </div>
            </div>
            
            <p>Als u vragen heeft of hulp nodig heeft bij het gebruik van de app, aarzel dan niet om contact op te nemen met uw administrator of teamleider. We staan altijd klaar om u te ondersteunen!</p>
            
            <p>Nogmaals welkom bij Hoppy Route - samen maken we Europa's steden schoner en groener! 🌱</p>
        </div>
        
        <div class='footer'>
            <p>Dit is een automatisch gegenereerd bericht van <span class='brand-name'>Hoppy Route</span>.</p>
            <p>© 2025 Hoppy Route. Alle rechten voorbehouden.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
