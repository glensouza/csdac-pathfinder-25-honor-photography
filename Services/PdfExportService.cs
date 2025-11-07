using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class PdfExportService(IDbContextFactory<ApplicationDbContext> contextFactory, CompositionRuleService ruleService, ILogger<PdfExportService> logger)
{
    public async Task<byte[]> GenerateSubmissionsReportAsync(string? pathfinderEmail = null, int? ruleId = null)
    {
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        IQueryable<PhotoSubmission> query = context.PhotoSubmissions;
        
        if (!string.IsNullOrEmpty(pathfinderEmail))
        {
            query = query.Where(s => s.PathfinderEmail == pathfinderEmail);
        }
        
        if (ruleId.HasValue)
        {
            query = query.Where(s => s.CompositionRuleId == ruleId.Value);
        }
        
        List<PhotoSubmission> submissions = await query
            .OrderBy(s => s.PathfinderName)
            .ThenBy(s => s.CompositionRuleName)
            .ToListAsync();

        Document document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Pathfinder Photography - Submissions Report")
                    .SemiBold()
                    .FontSize(20)
                    .FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        // Summary section
                        x.Item().Element(ComposeSubmissionSummary);

                        // Submissions list
                        x.Item().Element(container => ComposeSubmissionsList(container, submissions));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                        text.Span(" - Generated on ");
                        text.Span(DateTime.Now.ToString("MMMM dd, yyyy HH:mm"));
                    });
            });
        });

        return document.GeneratePdf();

        void ComposeSubmissionSummary(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
            {
                column.Spacing(5);
                column.Item().Text("Summary").SemiBold().FontSize(16);
                column.Item().Text($"Total Submissions: {submissions.Count}");
                column.Item().Text($"Passed: {submissions.Count(s => s.GradeStatus == GradeStatus.Pass)}");
                column.Item().Text($"Failed: {submissions.Count(s => s.GradeStatus == GradeStatus.Fail)}");
                column.Item().Text($"Not Graded: {submissions.Count(s => s.GradeStatus == GradeStatus.NotGraded)}");
            });
        }
    }

    private void ComposeSubmissionsList(IContainer container, List<PhotoSubmission> submissions)
    {
        container.Column(column =>
        {
            column.Spacing(15);
            column.Item().Text("Submissions Details").SemiBold().FontSize(16);

            foreach (PhotoSubmission submission in submissions)
            {
                column.Item().Element(c => ComposeSubmissionItem(c, submission));
            }
        });
    }

    private void ComposeSubmissionItem(IContainer container, PhotoSubmission submission)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Pathfinder: {submission.PathfinderName}").SemiBold();
                row.RelativeItem().AlignRight().Text(GetStatusBadge(submission.GradeStatus));
            });

            column.Item().Text($"Rule: {submission.CompositionRuleName}").FontColor(Colors.Blue.Medium);
            column.Item().Text($"Email: {submission.PathfinderEmail}").FontSize(9);
            column.Item().Text($"Submitted: {submission.SubmissionDate:MMMM dd, yyyy}").FontSize(9);
            
            if (submission.SubmissionVersion > 1)
            {
                column.Item().Text($"Version: {submission.SubmissionVersion}").FontSize(9);
            }

            if (!string.IsNullOrEmpty(submission.Description))
            {
                column.Item().PaddingTop(5).Text("Description:").SemiBold().FontSize(10);
                column.Item().Text(submission.Description).FontSize(9);
            }

            if (!string.IsNullOrEmpty(submission.GradedBy))
            {
                column.Item().PaddingTop(5).Text($"Graded by: {submission.GradedBy} on {submission.GradedDate:MMMM dd, yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);
            }

            // Image placeholder - note: actual image rendering would require the image bytes
            if (submission.ImageData != null && submission.ImageData.Length > 0)
            {
                try
                {
                    column.Item().PaddingTop(5).Image(submission.ImageData).FitWidth();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to render image in PDF for submission {SubmissionId}", submission.Id);
                    column.Item().Text("[Image could not be rendered]").FontSize(9).FontColor(Colors.Red.Medium);
                }
            }
        });
    }

    private static string GetStatusBadge(GradeStatus status)
    {
        return status switch
        {
            GradeStatus.Pass => "✓ PASSED",
            GradeStatus.Fail => "✗ FAILED",
            GradeStatus.NotGraded => "⧖ NOT GRADED",
            _ => "UNKNOWN"
        };
    }

    public async Task<byte[]> GeneratePathfinderProgressReportAsync(string pathfinderEmail)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail)
            .OrderBy(s => s.CompositionRuleName)
            .ToListAsync();

        if (submissions.Count == 0)
        {
            throw new InvalidOperationException("No submissions found for this pathfinder.");
        }

        string pathfinderName = submissions.First().PathfinderName;
        List<CompositionRule> allRules = ruleService.GetAllRules();

        Document document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Pathfinder Photography Progress Report")
                            .SemiBold()
                            .FontSize(20)
                            .FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Pathfinder: {pathfinderName}")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken2);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        // Progress overview
                        x.Item().Element(container => ComposeProgressOverview(container, allRules, submissions));

                        // Detailed submissions
                        x.Item().Element(container => ComposeSubmissionsList(container, submissions));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                        text.Span(" - Generated on ");
                        text.Span(DateTime.Now.ToString("MMMM dd, yyyy"));
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeProgressOverview(IContainer container, List<CompositionRule> allRules, List<PhotoSubmission> submissions)
    {
        container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
        {
            column.Spacing(5);
            column.Item().Text("Progress Overview").SemiBold().FontSize(16);
            
            int totalRules = allRules.Count;
            int completedRules = submissions.Count(s => s.GradeStatus == GradeStatus.Pass);
            int pendingRules = submissions.Count(s => s.GradeStatus == GradeStatus.NotGraded);
            int failedRules = submissions.Count(s => s.GradeStatus == GradeStatus.Fail);
            int notStarted = totalRules - submissions.Select(s => s.CompositionRuleId).Distinct().Count();

            column.Item().Text($"Completed: {completedRules}/{totalRules} rules").FontColor(Colors.Green.Medium);
            column.Item().Text($"Pending Review: {pendingRules} submissions");
            column.Item().Text($"Failed: {failedRules} submissions").FontColor(Colors.Red.Medium);
            column.Item().Text($"Not Started: {notStarted} rules");
        });
    }
}
