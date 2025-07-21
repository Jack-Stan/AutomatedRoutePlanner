# Email Configuratie Guide - Hoppy Route

## Overzicht
De Hoppy Route applicatie gebruikt een Email Service voor het verzenden van tijdelijke wachtwoorden en andere belangrijke berichten naar gebruikers.

## Configuratie

### 1. Gmail Setup (Aanbevolen voor ontwikkeling)

**Stap 1: Google Account Setup**
1. Ga naar uw Google Account instellingen
2. Navigeer naar "Security" → "2-Step Verification"
3. Scroll naar beneden en klik op "App passwords"
4. Selecteer "Mail" en "Other (custom name)"
5. Voer "Hoppy Route" in als naam
6. Kopieer het gegenereerde app-password (16 karakters)

**Stap 2: Configuratie in appsettings.json**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-16-character-app-password",
    "FromEmail": "noreply@hoppy.com",
    "FromName": "Hoppy Route",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

### 2. Outlook/Hotmail Setup

**Configuratie:**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@outlook.com",
    "SmtpPassword": "your-account-password",
    "FromEmail": "noreply@hoppy.com",
    "FromName": "Hoppy Route",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

### 3. Productie Email Services

**SendGrid (Aanbevolen voor productie)**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SmtpUsername": "apikey",
    "SmtpPassword": "your-sendgrid-api-key",
    "FromEmail": "noreply@hoppy.com",
    "FromName": "Hoppy Route",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

**Amazon SES**
```json
{
  "EmailSettings": {
    "SmtpHost": "email-smtp.eu-west-1.amazonaws.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-aws-smtp-username",
    "SmtpPassword": "your-aws-smtp-password",
    "FromEmail": "noreply@hoppy.com",
    "FromName": "Hoppy Route",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

## Email Templates

### Tijdelijke Wachtwoord Email
- **Onderwerp**: "Welkom bij Hoppy Route - Uw tijdelijke inloggegevens"
- **Inhoud**: Professionele HTML template met:
  - Welkomstbericht
  - Inloggegevens (username + tijdelijk wachtwoord)
  - Veiligheidswaarschuwingen
  - Bedrijfsbranding

### Wachtwoord Reset Email
- **Onderwerp**: "Hoppy Route - Wachtwoord Reset"
- **Inhoud**: Beveiligde reset link met vervaldatum

### Welkomst Email
- **Onderwerp**: "Welkom bij Hoppy Route!"
- **Inhoud**: Algemene welkomstboodschap en functionaliteiten

## Ontwikkeling & Testing

### Lokale Testing zonder Echte Emails
Voor ontwikkeling kunt u een lokale SMTP server gebruiken:

**MailHog (Aanbevolen)**
```bash
# Installeer MailHog
go install github.com/mailhog/MailHog@latest

# Start MailHog
MailHog
```

Configuratie voor MailHog:
```json
{
  "EmailSettings": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@hoppy.com",
    "FromName": "Hoppy Route Development",
    "EnableSsl": false,
    "TimeoutSeconds": 30
  }
}
```

### Logging
De EmailService logt automatisch:
- Succesvolle email verzendingen
- SMTP fouten
- Algemene fouten

Logs zijn beschikbaar in de console en kunnen worden geconfigureerd via `appsettings.json`.

## Implementatie Status

### ✅ Voltooid
- Email Service interface en implementatie
- HTML email templates (Nederlands)
- Configuratie setup
- Integration met AuthService
- Unit tests voor EmailService
- Foutafhandeling en logging

### 🔄 In Gebruik
- Tijdelijke wachtwoord emails bij gebruiker aanmaken
- Automatische email verzending na account creatie
- Fallback bij email fouten (gebruiker wordt nog steeds aangemaakt)

### 🎯 Aanbevelingen voor Productie
1. **Domein Setup**: Configureer SPF, DKIM, en DMARC records
2. **Email Verificatie**: Implementeer bounce handling
3. **Template Verbeteringen**: Voeg meer branding en styling toe
4. **Monitoring**: Implementeer email delivery monitoring
5. **Compliance**: Zorg voor GDPR compliance in email templates

## Troubleshooting

### Veel Voorkomende Problemen

**1. Authentication Failed**
- Controleer username en password
- Voor Gmail: gebruik app-password, geen account password
- Voor Outlook: activeer "less secure app access"

**2. Connection Timeout**
- Controleer SMTP host en port
- Controleer firewall instellingen
- Verhoog TimeoutSeconds in configuratie

**3. SSL/TLS Errors**
- Controleer EnableSsl setting
- Voor poort 465: EnableSsl = true
- Voor poort 587: EnableSsl = true (STARTTLS)

**4. Emails in Spam**
- Configureer SPF record voor uw domein
- Gebruik professionele "FromEmail"
- Voeg DKIM signing toe

## Monitoring

### Productie Monitoring
- Implementeer email delivery rate monitoring
- Log failed email attempts
- Setup alerting voor hoge failure rates
- Monitor bounce rates

### Metriken om te Volgen
- Email delivery success rate
- Email open rates (indien tracking geïmplementeerd)
- Bounce rates
- Complaint rates
