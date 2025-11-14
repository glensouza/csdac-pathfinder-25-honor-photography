using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class EmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly bool _isEnabled; // keep field declaration to avoid hot-reload related analyzer errors

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        this._configuration = configuration;
        this._logger = logger;
        
        // Validate required email configuration — email notifications are required
        string? smtpHost = this._configuration["Email:SmtpHost"];
        string? fromAddress = this._configuration["Email:FromAddress"];

        // Set _isEnabled based on presence of smtpHost (will be true if configured)
        this._isEnabled = !string.IsNullOrWhiteSpace(smtpHost);

        if (!this._isEnabled)
        {
            throw new InvalidOperationException("Email notifications are required. Missing configuration 'Email:SmtpHost'.");
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Email notifications are required. Missing configuration 'Email:FromAddress'.");
        }

        this._logger.LogInformation("Email notifications are enabled.");
    }

    public async Task SendGradingNotificationAsync(
        string pathfinderEmail,
        string pathfinderName,
        string compositionRuleName,
        GradeStatus gradeStatus,
        string gradedBy)
    {
        try
        {
            using MimeMessage message = new();
            message.From.Add(new MailboxAddress(this._configuration["Email:FromName"] ?? "Pathfinder Photography", this._configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));
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

            await this.SendEmailAsync(message);
            this._logger.LogInformation("Grading notification sent for {Rule}", compositionRuleName);
        }
        catch (SmtpCommandException ex)
        {
            this._logger.LogError(ex, "SMTP command error while sending grading notification for {Rule}", compositionRuleName);
        }
        catch (SmtpProtocolException ex)
        {
            this._logger.LogError(ex, "SMTP protocol error while sending grading notification for {Rule}", compositionRuleName);
        }
        catch (FormatException ex)
        {
            this._logger.LogError(ex, "Email format error while sending grading notification for {Rule}", compositionRuleName);
        }
        catch (IOException ex)
        {
            this._logger.LogError(ex, "IO error while sending grading notification for {Rule}", compositionRuleName);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            this._logger.LogError(ex, "Failed to send grading notification for {Rule}", compositionRuleName);
        }
    }

    public async Task SendNewSubmissionNotificationAsync(
        string pathfinderEmail,
        string pathfinderName,
        string compositionRuleName,
        List<string> instructorEmails)
    {
        if (!instructorEmails.Any())
        {
            this._logger.LogDebug("No instructors to notify for new submission, skipping email.");
            return;
        }

        try
        {
            using MimeMessage message = new();
            message.From.Add(new MailboxAddress(this._configuration["Email:FromName"] ?? "Pathfinder Photography", this._configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));

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

            await this.SendEmailAsync(message);
            this._logger.LogInformation("New submission notification sent to {Count} instructors for {Rule}", 
                instructorEmails.Count, compositionRuleName);
        }
        catch (SmtpCommandException ex)
        {
            this._logger.LogError(ex, "SMTP command error while sending new submission notification");
        }
        catch (SmtpProtocolException ex)
        {
            this._logger.LogError(ex, "SMTP protocol error while sending new submission notification");
        }
        catch (FormatException ex)
        {
            this._logger.LogError(ex, "Email format error while sending new submission notification");
        }
        catch (IOException ex)
        {
            this._logger.LogError(ex, "IO error while sending new submission notification");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            this._logger.LogError(ex, "Failed to send new submission notification");
        }
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        string? smtpHost = this._configuration["Email:SmtpHost"];
        
        int smtpPort;
        if (!int.TryParse(this._configuration["Email:SmtpPort"], out smtpPort))
        {
            smtpPort = 587;
        }
        
        string? smtpUsername = this._configuration["Email:SmtpUsername"];
        string? smtpPassword = this._configuration["Email:SmtpPassword"];
        
        bool useSsl;
        if (!bool.TryParse(this._configuration["Email:UseSsl"], out useSsl))
        {
            useSsl = true;
        }

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
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            this._logger.LogError(ex, "Error sending email via SMTP");
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

    public async Task SendCompletionCertificateAsync(
        string pathfinderEmail,
        string pathfinderName,
        byte[] certificatePdf)
    {
        if (!this._isEnabled)
        {
            this._logger.LogDebug("Email notifications disabled, skipping certificate notification.");
            return;
        }

        try
        {
            using MimeMessage message = new();
            message.From.Add(new MailboxAddress(this._configuration["Email:FromName"] ?? "Pathfinder Photography", this._configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));
            message.To.Add(new MailboxAddress(pathfinderName, pathfinderEmail));
            message.Subject = "Congratulations! Photography Honor Completed";

            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>🎉 Congratulations on Completing the Photography Honor! 🎉</h2>
                        <p>Dear {pathfinderName},</p>
                        <p>We are thrilled to inform you that you have successfully completed <strong>all composition requirements</strong> for the Pathfinder Photography Honor!</p>
                        <p>Your dedication to learning and mastering the various photography composition techniques has been outstanding. You have demonstrated excellence in all 11 composition rules.</p>
                        <p>Please find your <strong>Certificate of Completion</strong> attached to this email. You can download, print, and proudly display it as a testament to your achievement.</p>
                        <p>This certificate is also stored in the system and can be re-downloaded at any time from your profile.</p>
                        <p><strong>What's Next?</strong></p>
                        <ul>
                            <li>Continue practicing and refining your photography skills</li>
                            <li>Share your knowledge with other Pathfinders</li>
                            <li>Keep voting on photos to help others improve</li>
                            <li>Consider pursuing advanced photography honors</li>
                        </ul>
                        <p>Once again, congratulations on this remarkable achievement!</p>
                        <p>Best regards,<br/>Pathfinder Photography Team</p>
                    </body>
                    </html>",
                TextBody = $@"
Congratulations on Completing the Photography Honor!

Dear {pathfinderName},

We are thrilled to inform you that you have successfully completed all composition requirements for the Pathfinder Photography Honor!

Your dedication to learning and mastering the various photography composition techniques has been outstanding. You have demonstrated excellence in all 11 composition rules.

Please find your Certificate of Completion attached to this email. You can download, print, and proudly display it as a testament to your achievement.

This certificate is also stored in the system and can be re-downloaded at any time from your profile.

What's Next?
- Continue practicing and refining your photography skills
- Share your knowledge with other Pathfinders
- Keep voting on photos to help others improve
- Consider pursuing advanced photography honors

Once again, congratulations on this remarkable achievement!

Best regards,
Pathfinder Photography Team"
            };

            // Attach certificate PDF
            bodyBuilder.Attachments.Add("Photography_Honor_Certificate.pdf", certificatePdf, new ContentType("application", "pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            await this.SendEmailAsync(message);
            this._logger.LogInformation("Completion certificate sent to {Email}", pathfinderEmail);
        }
        catch (SmtpCommandException ex)
        {
            this._logger.LogError(ex, "SMTP command error while sending completion certificate");
        }
        catch (SmtpProtocolException ex)
        {
            this._logger.LogError(ex, "SMTP protocol error while sending completion certificate");
        }
        catch (FormatException ex)
        {
            this._logger.LogError(ex, "Email format error while sending completion certificate");
        }
        catch (IOException ex)
        {
            this._logger.LogError(ex, "IO error while sending completion certificate");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            this._logger.LogError(ex, "Failed to send completion certificate");
        }
    }

    public async Task SendTopPhotosReportAsync(
        string pathfinderEmail,
        string pathfinderName,
        byte[] topPhotosReportPdf)
    {
        if (!this._isEnabled)
        {
            this._logger.LogDebug("Email notifications disabled, skipping top photos report.");
            return;
        }

        try
        {
            using MimeMessage message = new();
            message.From.Add(new MailboxAddress(this._configuration["Email:FromName"] ?? "Pathfinder Photography", this._configuration["Email:FromAddress"] ?? "noreply@pathfinderphotography.local"));
            message.To.Add(new MailboxAddress(pathfinderName, pathfinderEmail));
            message.Subject = "Your Photos in Top Rankings!";

            BodyBuilder bodyBuilder = new()
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>🏆 Your Photos Are in the Top Rankings! 🏆</h2>
                        <p>Dear {pathfinderName},</p>
                        <p>Great news! One or more of your photos have made it into the <strong>top 3 rankings</strong> for their composition categories!</p>
                        <p>This achievement shows that your photography skills are being recognized by the community through our voting system.</p>
                        <p>Attached is a personalized report highlighting your top-ranked photos. Your photos are specially marked in the report for easy identification.</p>
                        <p><strong>Keep up the excellent work!</strong></p>
                        <ul>
                            <li>Continue submitting high-quality photos</li>
                            <li>Experiment with different composition techniques</li>
                            <li>Learn from other top-ranked photos</li>
                            <li>Help others by voting and providing feedback</li>
                        </ul>
                        <p>We're proud of your progress and can't wait to see what you'll create next!</p>
                        <p>Best regards,<br/>Pathfinder Photography Team</p>
                    </body>
                    </html>",
                TextBody = $@"
Your Photos Are in the Top Rankings!

Dear {pathfinderName},

Great news! One or more of your photos have made it into the top 3 rankings for their composition categories!

This achievement shows that your photography skills are being recognized by the community through our voting system.

Attached is a personalized report highlighting your top-ranked photos. Your photos are specially marked in the report for easy identification.

Keep up the excellent work!
- Continue submitting high-quality photos
- Experiment with different composition techniques
- Learn from other top-ranked photos
- Help others by voting and providing feedback

We're proud of your progress and can't wait to see what you'll create next!

Best regards,
Pathfinder Photography Team"
            };

            // Attach top photos report PDF
            bodyBuilder.Attachments.Add("Top_Photos_Report.pdf", topPhotosReportPdf, new ContentType("application", "pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            await this.SendEmailAsync(message);
            this._logger.LogInformation("Top photos report sent to {Email}", pathfinderEmail);
        }
        catch (SmtpCommandException ex)
        {
            this._logger.LogError(ex, "SMTP command error while sending top photos report");
        }
        catch (SmtpProtocolException ex)
        {
            this._logger.LogError(ex, "SMTP protocol error while sending top photos report");
        }
        catch (FormatException ex)
        {
            this._logger.LogError(ex, "Email format error while sending top photos report");
        }
        catch (IOException ex)
        {
            this._logger.LogError(ex, "IO error while sending top photos report");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            this._logger.LogError(ex, "Failed to send top photos report");
        }
    }
}
