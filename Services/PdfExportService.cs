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
                        x.Item().Element(container => this.ComposeSubmissionsList(container, submissions));
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
                column.Item().Element(c => this.ComposeSubmissionItem(c, submission));
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

            // Image rendering with error handling - display as thumbnail
            if (submission.ImageData is not { Length: > 0 })
            {
                return;
            }

            try
            {
                column.Item().PaddingTop(5).Width(250).Image(submission.ImageData).FitArea();
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Failed to render image in PDF for submission {SubmissionId}", submission.Id);
                column.Item().Text("[Image could not be rendered]").FontSize(9).FontColor(Colors.Red.Medium);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to render image in PDF for submission {SubmissionId}", submission.Id);
                column.Item().Text("[Image could not be rendered]").FontSize(9).FontColor(Colors.Red.Medium);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
            {
                logger.LogWarning(ex, "Failed to render image in PDF for submission {SubmissionId}", submission.Id);
                column.Item().Text("[Image could not be rendered]").FontSize(9).FontColor(Colors.Red.Medium);
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
                        x.Item().Element(container => this.ComposeSubmissionsList(container, submissions));
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

    public async Task<byte[]> GenerateCompletionCertificateAsync(string pathfinderEmail)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail)
            .ToListAsync();

        if (submissions.Count == 0)
        {
            throw new InvalidOperationException("No submissions found for this pathfinder.");
        }

        string pathfinderName = submissions.First().PathfinderName;
        List<CompositionRule> allRules = ruleService.GetAllRules();

        // Check if all rules are passed
        HashSet<int> passedRuleIds = submissions
            .Where(s => s.GradeStatus == GradeStatus.Pass)
            .Select(s => s.CompositionRuleId)
            .ToHashSet();

        if (passedRuleIds.Count < allRules.Count)
        {
            throw new InvalidOperationException($"Not all composition rules have been passed. Passed: {passedRuleIds.Count}/{allRules.Count}");
        }

        DateTime completionDate = submissions
            .Where(s => s.GradeStatus == GradeStatus.Pass)
            .Max(s => s.GradedDate ?? s.SubmissionDate);

        Document document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Use tighter margins and slightly smaller fonts to ensure the layout fits on a single landscape page
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Certificate border (reduced padding)
                        column.Item().Border(3).BorderColor(Colors.Blue.Darken2).Padding(10).Column(innerColumn =>
                        {
                            innerColumn.Spacing(10);

                            // Title
                            innerColumn.Item().AlignCenter().Text("Certificate of Completion")
                                .FontSize(30)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            innerColumn.Item().AlignCenter().Text("Pathfinder Photography Honor")
                                .FontSize(20)
                                .FontColor(Colors.Blue.Medium);

                            // Decorative line
                            innerColumn.Item().PaddingVertical(8).AlignCenter()
                                .Width(380).Height(1).Background(Colors.Blue.Lighten2);

                            // Main text
                            innerColumn.Item().PaddingTop(8).AlignCenter().Text("This certifies that")
                                .FontSize(14);

                            innerColumn.Item().AlignCenter().Text(pathfinderName)
                                .FontSize(28)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            innerColumn.Item().PaddingTop(8).AlignCenter()
                                .Text("has successfully completed all composition requirements")
                                .FontSize(14);

                            innerColumn.Item().AlignCenter()
                                .Text("for the Pathfinder Photography Honor")
                                .FontSize(14);

                            // Composition rules completed
                            innerColumn.Item().PaddingTop(12).AlignCenter().Text($"All {allRules.Count} Composition Rules Mastered")
                                .FontSize(12)
                                .SemiBold();

                            // Date
                            innerColumn.Item().PaddingTop(10).AlignCenter()
                                .Text($"Date of Completion: {completionDate:MMMM dd, yyyy}")
                                .FontSize(14)
                                .FontColor(Colors.Grey.Darken1);

                            // Footer
                            innerColumn.Item().PaddingTop(8).AlignCenter()
                                .Text("Congratulations on your achievement!")
                                .FontSize(12)
                                .Italic()
                                .FontColor(Colors.Blue.Medium);
                        });
                    });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateTopPhotosReportAsync(string pathfinderEmail)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        List<CompositionRule> allRules = ruleService.GetAllRules();
        
        // Get top 3 photos for each rule (passed and failed)
        Dictionary<int, List<PhotoSubmission>> topPhotosByRule = new();
        
        foreach (CompositionRule rule in allRules)
        {
            List<PhotoSubmission> topPhotos = await context.PhotoSubmissions
                .Where(s => s.CompositionRuleId == rule.Id)
                .OrderByDescending(s => s.EloRating)
                .ThenByDescending(s => s.SubmissionDate)
                .Take(3)
                .ToListAsync();
            
            if (topPhotos.Any())
            {
                topPhotosByRule[rule.Id] = topPhotos;
            }
        }

        // Check if pathfinder has any photos in the top lists
        bool hasPhotosInList = topPhotosByRule.Values
            .Any(photos => photos.Any(p => p.PathfinderEmail.Equals(pathfinderEmail, StringComparison.OrdinalIgnoreCase)));

        if (!hasPhotosInList)
        {
            throw new InvalidOperationException("Pathfinder has no photos in the top 3 for any composition rule.");
        }

        string pathfinderName = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail)
            .Select(s => s.PathfinderName)
            .FirstOrDefaultAsync() ?? "Unknown";

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
                        column.Item().Text("Top Ranked Photos Report")
                            .SemiBold()
                            .FontSize(20)
                            .FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Prepared for: {pathfinderName}")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken2);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        foreach (CompositionRule rule in allRules)
                        {
                            if (!topPhotosByRule.TryGetValue(rule.Id, out List<PhotoSubmission>? photos) || !photos.Any())
                            {
                                continue;
                            }

                            // Only include rules where the pathfinder has at least one photo
                            if (!photos.Any(p => p.PathfinderEmail.Equals(pathfinderEmail, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            x.Item().Element(container => this.ComposeTopPhotosForRule(container, rule, photos, pathfinderEmail));
                        }
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

    private void ComposeTopPhotosForRule(IContainer container, CompositionRule rule, List<PhotoSubmission> photos, string highlightEmail)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"{rule.Id}. {rule.Name}").SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);
            
            foreach ((PhotoSubmission photo, int index) in photos.Select((p, i) => (p, i)))
            {
                bool isHighlighted = photo.PathfinderEmail.Equals(highlightEmail, StringComparison.OrdinalIgnoreCase);
                
                column.Item().Element(c => this.ComposeTopPhotoItem(c, photo, index + 1, isHighlighted));
            }
        });
    }

    private void ComposeTopPhotoItem(IContainer container, PhotoSubmission photo, int rank, bool isHighlighted)
    {
        container.Border(isHighlighted ? 3 : 1)
            .BorderColor(isHighlighted ? Colors.Orange.Medium : Colors.Grey.Lighten2)
            .Background(isHighlighted ? Colors.Orange.Lighten4 : Colors.White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span($"#{rank} - ").SemiBold();
                        text.Span(photo.PathfinderName);
                        if (isHighlighted)
                        {
                            text.Span(" ⭐ YOUR PHOTO").Bold().FontColor(Colors.Orange.Darken2);
                        }
                    });
                    row.AutoItem().Text($"Rating: {Math.Round(photo.EloRating, 0)}").FontSize(10).FontColor(Colors.Blue.Medium);
                });

                column.Item().Row(row =>
                {
                    row.AutoItem().Text($"Status: ");
                    row.AutoItem().Text(GetStatusBadge(photo.GradeStatus));
                    row.RelativeItem().AlignRight().Text($"Submitted: {photo.SubmissionDate:MMM dd, yyyy}").FontSize(9);
                });

                if (!string.IsNullOrEmpty(photo.Description))
                {
                    column.Item().PaddingTop(5).Text($"\"{photo.Description}\"").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                }

                // Image rendering with error handling
                if (photo.ImageData is not { Length: > 0 })
                {
                    return;
                }

                try
                {
                    column.Item().PaddingTop(5).MaxWidth(300).Image(photo.ImageData).FitArea();
                }
                catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
                {
                    logger.LogWarning(ex, "Failed to render image in PDF for submission {SubmissionId}", photo.Id);
                    column.Item().Text("[Image could not be rendered]").FontSize(9).FontColor(Colors.Red.Medium);
                }
            });
    }
}
