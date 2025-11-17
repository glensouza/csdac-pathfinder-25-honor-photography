# Role-Based Grading System

## Overview

The Pathfinder Photography application includes a role-based grading system that allows instructors to grade photo submissions and pathfinders to resubmit failed photos.

## Roles

### Pathfinder (Default Role)

- All users start with the Pathfinder role when they first sign in
- Can submit photos for composition rules
- Can view their own submissions and grades
- Can resubmit photos that have been marked as "Fail"
- Cannot resubmit photos that have passed

### Instructor

- Can view all photo submissions from all pathfinders
- Can grade submissions as Pass or Fail
- Can view submission history and versions
- Can reset grades if needed
- Has access to the Grading page in the navigation menu

## Getting Started

### For the First Admin/Instructor

1. **Sign in** to the application using Google authentication
2. Navigate to `/admin/users` (manually enter this URL in your browser)
3. You will see a list of all users who have signed in
4. **Promote yourself** or another user to Instructor by clicking the "Promote to Instructor" button
5. Once promoted, the Instructor will see the "Grade Submissions" link in the navigation menu

### For Pathfinders

1. **Sign in** using Google authentication
2. Click **"Submit Photos"** in the navigation menu
3. Select a composition rule from the dropdown
4. Upload a photo and describe how you applied the rule
5. Click **"Submit Photo"**
6. View your submissions below the form with their grade status:
   - **Not Graded Yet** - Waiting for instructor review
   - **✓ Pass** - Approved by instructor
   - **✗ Fail** - Needs improvement, can be resubmitted

#### Resubmitting Failed Photos

1. Go to **"Submit Photos"**
2. Select the composition rule for which you received a "Fail" grade
3. You'll see a warning that you have an existing submission and its status
4. Upload a new photo and description
5. Submit - this will create a new version of your submission
6. The new submission will be marked as "Not Graded" and sent to the instructor for re-grading

### For Instructors

1. **Sign in** and ensure you have the Instructor role
2. Click **"Grade Submissions"** in the navigation menu
3. Filter submissions by:
   - Status (Not Graded, Pass, Fail)
   - Composition Rule
   - Pathfinder Name
4. Click a photo thumbnail to see full details in a modal
5. Grade the submission:
   - Click **"Pass"** if the photo demonstrates the rule correctly
   - Click **"Fail"** if the photo needs improvement
6. Pathfinders can see their grades and resubmit failed photos

## Features

### Grade Status Indicators

- **Not Graded** (Gray badge) - Awaiting instructor review
- **Pass** (Green badge with ✓) - Approved
- **Fail** (Red badge with ✗) - Needs resubmission

### Submission Versioning

- When a pathfinder resubmits a failed photo, it creates a new version
- Version numbers are displayed (v1, v2, v3, etc.)
- Instructors can see which submissions are resubmissions

### Gallery Updates

- The Gallery page now shows grade status for each submission
- Filter by grade status to see only passed, failed, or ungraded submissions
- Version badges indicate resubmissions

## User Management

### Accessing the Admin Panel

Navigate to `/admin/users` while signed in to:
- View all users in the system
- Promote users to Instructor role
- Demote instructors back to Pathfinder role
- Delete users who shouldn't have access to the system
- See when users joined

### Deleting Users

Admins can delete users who shouldn't be submitting photos to the system:

1. Navigate to `/admin/users`
2. Find the user you want to delete
3. Click the **"Delete"** button next to their name
4. Confirm the deletion in the popup dialog

**What gets deleted:**
- The user account
- All photo submissions by that user
- All votes involving their photos
- All votes made by that user

**What gets recalculated:**
- ELO ratings for photos that were voted on by the deleted user (ratings are recalculated based on the votes that remain after deletion)

**Important Notes:**
- Admin users cannot be deleted through the UI (must use direct database access)
- You cannot delete your own account
- This action is permanent and cannot be undone
- When a user is deleted, ELO ratings are recalculated for affected photos based on the remaining votes (the deleted user's votes are excluded from the recalculation)

## Workflow Example

### Typical Student Workflow

1. Student signs in → Automatically created as Pathfinder
2. Submits 10 photos (one for each composition rule)
3. Instructor grades the submissions
4. Student sees their grades on the Submit page
5. If any photos failed, student resubmits improved versions
6. Instructor re-grades the resubmissions
7. Process continues until all 10 photos pass

### Typical Instructor Workflow

1. Instructor is promoted via `/admin/users`
2. Signs in and sees "Grade Submissions" in menu
3. Reviews ungraded submissions
4. Grades each photo as Pass or Fail
5. Watches for resubmissions from students
6. Re-grades improved photos

## Tips

- **For Pathfinders:** Review the composition rule description carefully before submitting
- **For Instructors:** Use filters to focus on ungraded submissions first
- **For Admins:** Promote instructors before the students start submitting photos
- **For Everyone:** The Gallery page is a great way to see examples of good photos that have passed

## Completion Certificates

### Automatic Certificate Generation

When a pathfinder successfully passes all 11 composition rules, the system automatically:

1. **Generates a completion certificate** - A professional PDF certificate with the pathfinder's name and completion date
1. **Stores it in the database** - The certificate is saved for future re-download
1. **Emails it to the pathfinder** - An automatic email is sent with the certificate attached
1. **Makes it available for download** - Pathfinders can access their certificate anytime from the "My Certificate" page

### Accessing Your Certificate

Pathfinders can view and download their completion certificate:

1. Navigate to **"My Certificate"** in the navigation menu
2. If all rules are completed:
   - See a congratulations message
   - View completion and issue dates
   - Download the certificate as a PDF
   - Re-download anytime (no limit)
3. If not yet completed:
   - See progress toward completion
   - View how many rules remain
   - Get links to submit more photos

### Manual Certificate Management (Admin)

Admins can manually trigger certificate generation and sending:

1. Navigate to **"Admin Dashboard"** → **"Export to PDF"**
2. Click **"Process & Send Certificates"**
3. The system will:
   - Check for newly completed pathfinders
   - Generate certificates for those without one
   - Email the certificates automatically

## Top Photos Reports

### Personalized Top Photos Reports

The system can generate personalized PDF reports for pathfinders whose photos rank in the top 3 for any composition rule:

1. **Report includes:**
   - Top 3 photos for each composition rule
   - Only shown if the pathfinder has at least one photo in the rankings
   - Pathfinder's own photos are highlighted with a special border and "YOUR PHOTO" label
   - Photo details: rank, ELO rating, grade status, and description
   - Thumbnail images of all top photos

2. **Sending Top Photos Reports (Admin):**
   - Navigate to **"Admin Dashboard"** → **"Export to PDF"**
   - Click **"Send Top Photos Reports"**
   - Reports are generated and emailed to all eligible pathfinders

### How Top Photos Are Ranked

Photos are ranked using the ELO rating system based on community voting:

- Each photo starts with a rating of 1000
- When users vote, ratings adjust based on comparisons
- Higher ratings indicate consistently preferred photos
- Top 3 photos for each composition rule are featured in reports

## Email Notifications

The system sends automatic email notifications for:

1. **Grading Notifications** - When a photo is graded (Pass or Fail)
2. **New Submission Notifications** - Sent to instructors when pathfinders submit photos
3. **Completion Certificates** - When a pathfinder completes all rules
4. **Top Photos Reports** - Personalized reports for pathfinders with ranked photos

**Note:** Email notifications require SMTP configuration. If not configured, the system will log notifications but won't send emails.

## Support

If you encounter any issues:
1. Ensure you're signed in with Google
2. Check that the database migrations have been applied
3. Verify that instructors have been promoted via `/admin/users`
4. Check the application logs for any errors
