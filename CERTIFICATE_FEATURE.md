# Certificate and Top Photos Reporting Feature

## Overview

This feature automatically generates and distributes PDF certificates and personalized reports for the Pathfinder Photography Honor application.

## Features

### 1. Completion Certificates

**Automatic Generation:**
- Triggers when a pathfinder passes all 11 composition rules
- Professional PDF certificate with pathfinder's name and completion date
- Stored in database for permanent access
- Automatically emailed to the pathfinder

**User Access:**
- Available via "My Certificate" menu item
- Download/re-download anytime
- Shows completion status and progress

**Certificate Design:**
- Landscape A4 format
- Decorative blue border
- Lists all 11 composition rules mastered
- Includes completion date

### 2. Top Photos Reports

**Personalized Reports:**
- Generated for pathfinders with photos in top 3 for any composition rule
- Highlights user's own photos with orange border and "YOUR PHOTO" label
- Includes photo thumbnails, ratings, descriptions, and status

**Content:**
- Top 3 photos for each composition rule (based on ELO ratings)
- Only includes rules where pathfinder has a top-ranked photo
- Professional PDF format

### 3. Admin Controls

Located in **Admin Dashboard → Export to PDF**:

1. **Process & Send Certificates**
   - Checks for newly completed pathfinders
   - Generates certificates if not already created
   - Sends emails automatically
   - Provides status feedback

2. **Send Top Photos Reports**
   - Identifies all pathfinders with top-ranked photos
   - Generates personalized reports
   - Sends batch emails
   - Handles errors gracefully

### 4. Email Notifications

**Certificate Email:**
- Subject: "Congratulations! Photography Honor Completed"
- Professional HTML email with congratulations message
- PDF certificate attached
- Instructions for re-download

**Top Photos Report Email:**
- Subject: "Your Photos in Top Rankings!"
- Recognition of achievement
- PDF report attached
- Encouragement to continue

## Technical Implementation

### Database

**New Table: CompletionCertificates**
```
- Id (Primary Key)
- PathfinderEmail (indexed, case-insensitive)
- PathfinderName
- CompletionDate (indexed)
- CertificatePdfData (binary)
- IssuedDate
- EmailSent (boolean)
- EmailSentDate (nullable)
```

### Services

**CertificateService:**
- `HasCompletedAllRulesAsync()` - Check completion status
- `GenerateAndStoreCertificateAsync()` - Create and store certificate
- `GetCertificateAsync()` - Retrieve existing certificate
- `SendCertificateEmailAsync()` - Email certificate
- `ProcessNewCompletionsAsync()` - Batch process new completions
- `SendTopPhotosReportsAsync()` - Batch send top photos reports

**PdfExportService Extensions:**
- `GenerateCompletionCertificateAsync()` - Create certificate PDF
- `GenerateTopPhotosReportAsync()` - Create personalized report PDF

**EmailNotificationService Extensions:**
- `SendCompletionCertificateAsync()` - Email certificate with attachment
- `SendTopPhotosReportAsync()` - Email report with attachment

### Automatic Triggers

**PhotoSubmissionService.GradeSubmissionAsync:**
- After grading a submission as "Pass"
- Checks if all rules are now completed
- Triggers certificate generation and email
- Runs asynchronously (non-blocking)

## Usage

### For Pathfinders

1. **Complete all 11 composition rules** by submitting photos and getting them graded
2. **Automatic certificate** is generated and emailed when final rule passes
3. **Access certificate** anytime via "My Certificate" menu item
4. **Download PDF** for printing or sharing

### For Admins

1. Navigate to **Admin Dashboard → Export to PDF**
2. Click **"Process & Send Certificates"** to process any new completions
3. Click **"Send Top Photos Reports"** to send reports to all eligible pathfinders
4. View status messages for success/failure

## Configuration

### Email Settings

Required in `appsettings.json` for email functionality:
```json
{
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUsername": "username",
    "SmtpPassword": "password",
    "UseSsl": true,
    "FromName": "Pathfinder Photography",
    "FromAddress": "noreply@pathfinderphotography.local"
  }
}
```

**Note:** If email is not configured:
- Certificates are still generated and stored
- Email operations are logged but not sent
- Pathfinders can still download from "My Certificate" page

### QuestPDF License

Uses QuestPDF Community license (suitable for non-commercial use):
- Free for open-source projects
- Free for educational purposes
- No attribution required
- See: https://www.questpdf.com/license/

## Migration

Apply the database migration:
```bash
dotnet ef database update
```

This creates the `CompletionCertificates` table with appropriate indexes.

## Testing

### Manual Testing

1. **Test Certificate Generation:**
   - Create a test user
   - Submit photos for all 11 rules
   - Grade all as "Pass"
   - Verify certificate appears in "My Certificate"
   - Download and verify PDF content

2. **Test Top Photos Report:**
   - Ensure test user has at least one photo in top 3
   - Go to Admin Export page
   - Click "Send Top Photos Reports"
   - Verify email received (if configured)
   - Verify photo highlighting in PDF

3. **Test Admin Controls:**
   - Navigate to Admin Export page
   - Click "Process & Send Certificates"
   - Verify status message
   - Check database for new certificates

### Error Scenarios

- Pathfinder with incomplete rules → Shows progress
- Missing email configuration → Logs warning, no email sent
- Invalid image data in submission → Gracefully handled in PDF
- Database errors → Logged, user notified

## Troubleshooting

### Certificate Not Generated

1. Verify all 11 rules are passed
2. Check application logs for errors
3. Manually trigger via Admin Export page

### Email Not Received

1. Check spam/junk folder
2. Verify SMTP configuration in appsettings.json
3. Check application logs for SMTP errors
4. Download certificate directly from "My Certificate" page

### PDF Generation Errors

1. Check logs for specific error messages
2. Verify image data is valid in database
3. Ensure QuestPDF Community license is acceptable
4. Check disk space for large image data

## Future Enhancements

Potential improvements:
- Scheduled batch processing (daily/weekly)
- Certificate templates selection
- Multi-language support
- Certificate verification QR codes
- Email template customization
- Report generation history
- Bulk certificate printing view

## Security Considerations

- ✅ CodeQL security scan passed (0 issues)
- ✅ No sensitive data in PDFs
- ✅ Email addresses case-insensitive (citext)
- ✅ Proper async/await patterns
- ✅ Graceful error handling
- ✅ No SQL injection risks
- ✅ Binary data properly handled
- ✅ Admin-only controls for bulk operations

## Performance Notes

- Certificate PDFs stored in database (consider archival strategy for large deployments)
- Async email sending prevents blocking
- Bulk operations process all eligible users
- Top photos queries optimized with indexes
- Large images may impact PDF generation time

## Support

For issues or questions:
1. Check application logs
2. Verify configuration
3. Review this documentation
4. Contact system administrator
