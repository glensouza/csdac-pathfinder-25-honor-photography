using MailKit.Net.Smtp;
using MimeKit;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class EmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly bool _isEnabled;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Check if email is configured
        string? smtpHost = _configuration["Email:SmtpHost"];
        _isEnabled = !string.IsNullOrEmpty(smtpHost);
        
        if (!_isEnabled)
        {
            _logger.LogInformation("Email notifications are disabled. Configure Email:SmtpHost to enable.");
        }
    }

    public async Task SendGradingNotificationAsync(
        string pathfinderEmail,
        string pathfinderName,
        string compositionRuleName,
        GradeStatus gradeStatus,
        string gradedBy)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Email notifications disabled, skipping grading notification.");
            return;
        }

        try
        {
            MimeMessage message = new();
            message.From.Add(new MailboxAddress(
                _configuration["Email:FromName"] ?? "Pathfinder Photography",
                _configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));
            message.To.Add(new MailboxAddress(pathfinderName, pathfinderEmail));
            message.Subject = $"Your {compositionRuleName} photo has been graded";

            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Photo Submission Graded</h2>
                        <p>Dear {pathfinderName},</p>
                        <p>Your photo submission for <strong>{compositionRuleName}</strong> has been reviewed.</p>
                        <p><strong>Result:</strong> {GetGradeStatusText(gradeStatus)}</p>
                        <p><strong>Reviewed by:</strong> {gradedBy}</p>
                        {GetGradeMessage(gradeStatus)}
                        <p>You can view your submissions and progress at the Pathfinder Photography portal.</p>
                        <p>Best regards,<br/>Pathfinder Photography Team</p>
                    </body>
                    </html>",
                TextBody = $@"
Photo Submission Graded

Dear {pathfinderName},

Your photo submission for {compositionRuleName} has been reviewed.

Result: {GetGradeStatusText(gradeStatus)}
Reviewed by: {gradedBy}

{GetGradeMessagePlainText(gradeStatus)}

You can view your submissions and progress at the Pathfinder Photography portal.

Best regards,
Pathfinder Photography Team"
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendEmailAsync(message);
            _logger.LogInformation("Grading notification sent to {Email} for {Rule}", pathfinderEmail, compositionRuleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send grading notification to {Email}", pathfinderEmail);
        }
    }

    public async Task SendNewSubmissionNotificationAsync(
        string pathfinderEmail,
        string pathfinderName,
        string compositionRuleName,
        List<string> instructorEmails)
    {
        if (!_isEnabled || !instructorEmails.Any())
        {
            _logger.LogDebug("Email notifications disabled or no instructors to notify, skipping submission notification.");
            return;
        }

        try
        {
            MimeMessage message = new();
            message.From.Add(new MailboxAddress(
                _configuration["Email:FromName"] ?? "Pathfinder Photography",
                _configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));

            foreach (string instructorEmail in instructorEmails)
            {
                message.Bcc.Add(new MailboxAddress("Instructor", instructorEmail));
            }

            message.Subject = $"New photo submission - {compositionRuleName}";

            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>New Photo Submission</h2>
                        <p>A new photo has been submitted for review:</p>
                        <ul>
                            <li><strong>Pathfinder:</strong> {pathfinderName}</li>
                            <li><strong>Email:</strong> {pathfinderEmail}</li>
                            <li><strong>Composition Rule:</strong> {compositionRuleName}</li>
                        </ul>
                        <p>Please log in to the grading portal to review this submission.</p>
                        <p>Best regards,<br/>Pathfinder Photography System</p>
                    </body>
                    </html>",
                TextBody = $@"
New Photo Submission

A new photo has been submitted for review:

Pathfinder: {pathfinderName}
Email: {pathfinderEmail}
Composition Rule: {compositionRuleName}

Please log in to the grading portal to review this submission.

Best regards,
Pathfinder Photography System"
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendEmailAsync(message);
            _logger.LogInformation("New submission notification sent to {Count} instructors for {Rule}", 
                instructorEmails.Count, compositionRuleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new submission notification");
        }
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        string? smtpHost = _configuration["Email:SmtpHost"];
        int smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        string? smtpUsername = _configuration["Email:SmtpUsername"];
        string? smtpPassword = _configuration["Email:SmtpPassword"];
        bool useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");

        if (string.IsNullOrEmpty(smtpHost))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        using SmtpClient client = new();
        
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, useSsl);

            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SMTP");
            throw;
        }
    }

    private static string GetGradeStatusText(GradeStatus status)
    {
        return status switch
        {
            GradeStatus.Pass => "✓ PASSED",
            GradeStatus.Fail => "✗ FAILED",
            GradeStatus.NotGraded => "Pending Review",
            _ => "Unknown"
        };
    }

    private static string GetGradeMessage(GradeStatus status)
    {
        return status switch
        {
            GradeStatus.Pass => "<p style='color: green;'><strong>Congratulations!</strong> Your photo demonstrates good understanding of this composition rule.</p>",
            GradeStatus.Fail => "<p style='color: orange;'>Your submission needs improvement. Please review the composition rule and try again.</p>",
            _ => ""
        };
    }

    private static string GetGradeMessagePlainText(GradeStatus status)
    {
        return status switch
        {
            GradeStatus.Pass => "Congratulations! Your photo demonstrates good understanding of this composition rule.",
            GradeStatus.Fail => "Your submission needs improvement. Please review the composition rule and try again.",
            _ => ""
        };
    }
}
