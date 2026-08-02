using ApparkaTrainingFlowOnline.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ApparkaTrainingFlowOnline.Services;

public class InvitationEmailService(IOptions<EmailOptions> options, ILogger<InvitationEmailService> logger)
{
    private readonly EmailOptions _options = options.Value;

    public async Task<bool> SendAsync(string recipient, string fullName, string activationUrl, DateOnly accessFrom, DateOnly startDate)
    {
        if (!_options.IsConfigured) return false;
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = "Acceso a tu periodo de entrenamiento",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Hola, {WebUtility.HtmlEncode(fullName)}</h2>
                    <p>Has sido registrado en la plataforma de entrenamiento.</p>
                    <p>Podrás revisar materiales desde el <strong>{accessFrom:dd/MM/yyyy}</strong> y tu periodo operativo inicia el <strong>{startDate:dd/MM/yyyy}</strong>.</p>
                    <p><a href="{WebUtility.HtmlEncode(activationUrl)}">Activar mi acceso</a></p>
                    <p>Este enlace es personal y tiene vigencia limitada.</p>
                    """
            };
            message.To.Add(recipient);
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(_options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.Username, _options.Password)
            };
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar la invitación a {Recipient}", recipient);
            return false;
        }
    }

    public async Task<bool> SendSupervisorAsync(string recipient, string fullName, string activationUrl)
    {
        if (!_options.IsConfigured) return false;
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = "Activa tu acceso de supervisor",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Hola, {WebUtility.HtmlEncode(fullName)}</h2>
                    <p>Recursos Humanos creó tu acceso como supervisor en Apparka Training Flow.</p>
                    <p><a href="{WebUtility.HtmlEncode(activationUrl)}">Crear mi contraseña y activar el acceso</a></p>
                    <p>Este enlace es personal y tiene vigencia limitada.</p>
                    """
            };
            message.To.Add(recipient);
            using var client = CreateClient();
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar la invitación de supervisor a {Recipient}", recipient);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string recipient, string fullName, string resetUrl)
    {
        if (!_options.IsConfigured) return false;
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = "Restablece tu contraseña",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Hola, {WebUtility.HtmlEncode(fullName)}</h2>
                    <p>Recibimos una solicitud para crear una nueva contraseña de Apparka Training Flow.</p>
                    <p><a href="{WebUtility.HtmlEncode(resetUrl)}">Crear una nueva contraseña</a></p>
                    <p>El enlace vence en una hora y solo puede utilizarse una vez.</p>
                    <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    """
            };
            message.To.Add(recipient);
            using var client = CreateClient();
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar la recuperación de contraseña a {Recipient}", recipient);
            return false;
        }
    }

    private SmtpClient CreateClient() => new(_options.Host, _options.Port)
    {
        EnableSsl = _options.EnableSsl,
        Credentials = string.IsNullOrWhiteSpace(_options.Username)
            ? CredentialCache.DefaultNetworkCredentials
            : new NetworkCredential(_options.Username, _options.Password)
    };
}
