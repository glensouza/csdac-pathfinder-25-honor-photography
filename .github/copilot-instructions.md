# Copilot Instructions for csdac-pathfinder-25-honor-photography

## Project Overview

This repository contains the Pathfinder Photography Honor application for Pathfinders 2025. It's a .NET 9.0 Blazor Server application for managing photography submissions, voting, and grading with ELO ratings.

**Key Technologies:**
- .NET 9.0 / Blazor Server / C#
- PostgreSQL 16
- Google OAuth 2.0 authentication
- OpenTelemetry & SigNoz observability
- .NET Aspire for local development orchestration

## Deployment Architecture

**Development (Local):**
- .NET Aspire (recommended - integrated observability)
- Local .NET (direct development)
- See [SETUP.md](../SETUP.md)

**Production:**
- Ubuntu 22.04 LTS (VM, bare metal, or cloud instance)
- No Docker - native installation
- Automated deployments via GitHub Actions (optional)
- See [DEPLOY.md](../DEPLOY.md)

## Code of Conduct

All contributions must adhere to our [Code of Conduct](../CODE_OF_CONDUCT.md). Please be respectful, inclusive, and professional in all interactions.

## Contribution Guidelines

When contributing to this repository, please follow the guidelines in [CONTRIBUTING.md](../CONTRIBUTING.md).

### Git Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line
- Consider starting the commit message with an applicable emoji:
  - 🎨 `:art:` when improving the format/structure of the code
  - 🐛 `:bug:` when fixing a bug
  - ✨ `:sparkles:` when introducing new features
  - 📝 `:memo:` when writing docs
  - 🚀 `:rocket:` when improving performance
  - ✅ `:white_check_mark:` when adding tests
  - 🔒 `:lock:` when dealing with security

## Code Style

**C# Specific:**
- **Always use explicit types; never use `var`** - This is a hard requirement
- Follow .NET naming conventions (PascalCase for public members, camelCase for private)
- Use async/await properly - don't block on async code
- Dispose IDisposable objects properly (using statements)
- Keep methods focused and single-purpose

**General:**
- Write clear, readable code with meaningful variable and function names
- Comment complex logic, not obvious code
- Follow existing code patterns and conventions in the project
- Keep functions small and focused on a single task
- Write self-documenting code where possible
- Use LINQ for collections where appropriate

## Documentation

**Important Files:**
- `SETUP.md` - Local development setup (Aspire, local .NET)
- `DEPLOY.md` - Production deployment guide (wizard-style with step-by-step files in deploy/ directory)
- `deploy/*.md` - Individual deployment step files
- `DEPLOYMENT_CHECKLIST.md` - Deployment verification checklist
- `.github/workflows/deploy-bare-metal.yml` - Automated deployment workflow

**Documentation Requirements:**
- Update markdown files when changing functionality
- Document new features with clear examples
- Keep documentation in sync with code changes
- Use clear, concise language
- Ensure documentation is accessible for Pathfinders (10-15 years old) and leaders

## Security

**Critical Requirements:**
- Never commit sensitive information (API keys, passwords, personal data)
- Use `openssl rand -base64 32` for generating secure passwords
- Follow principle of least privilege (e.g., github-runner user NOT in sudo group)
- Use restrictive file patterns in sudoers (e.g., `[0-9a-f]*` for SHA hashes, not `*`)
- Set proper file permissions (600 for config files with secrets)
- Report security vulnerabilities privately via [SECURITY.md](../SECURITY.md)

**Production Security:**
- All services run as non-root users
- PostgreSQL listens only on localhost
- Nginx security headers configured
- Firewall with default-deny policy
- SSL/TLS with Let's Encrypt
- Automated deployments use restricted sudo via sudoers file

## Project-Specific Guidelines

### Application Architecture

**User Roles:**
- 0 = Pathfinder (default)
- 1 = Instructor
- 2 = Admin (first user auto-promoted)

**Key Features:**
- Photo submission with 10 composition rules
- ELO rating system for photo comparison voting
- Admin user management (promote, demote, delete with ELO recalculation)
- PDF export functionality
- Email notifications (optional)

### Photography Content

- Ensure all photography-related content is accurate and educational
- Use appropriate photography terminology
- Include practical examples and exercises where applicable
- Make content accessible for beginners while providing depth for advanced learners

### Educational Focus

- Content should be appropriate for Pathfinder age group (typically 10-15 years old)
- Use clear, age-appropriate language
- Include visual examples where helpful
- Provide step-by-step instructions for practical activities

### Database Considerations

- Always use Entity Framework migrations for schema changes
- Test migrations both up and down
- Consider data migration needs when changing models
- ELO ratings are recalculated when users are deleted (important for integrity)

### Performance

- Use async/await for I/O operations
- Consider query performance with Entity Framework (use `.AsNoTracking()` for read-only)
- Photo uploads limited to 10MB
- Database queries should use proper indexes

## Testing & Validation

**Before Committing:**
- Test locally with Aspire or local .NET
- Run Entity Framework migrations if model changes
- Verify Google OAuth still works
- Check for obvious errors in browser console
- Test photo upload functionality if related to changes

**Documentation Changes:**
- Verify all internal links work
- Check markdown formatting
- Ensure consistency with related docs

## Deployment Paths

**Local Development:**
1. Aspire: `dotnet run --project PathfinderPhotography.AppHost` (automatic SigNoz)
2. Local .NET: `dotnet run` (manual PostgreSQL setup)

**Production Deployment:**
1. Manual: Follow [DEPLOY.md](../DEPLOY.md) step-by-step
2. Automated: Set up GitHub Actions runner on server (section 7 of deployment guide)
   - Every push to `main` triggers automated deployment
   - Includes backup, health checks, and automatic rollback

## Common Tasks

**Add a new EF migration:**
```bash
dotnet ef migrations add MeaningfulName
dotnet ef database update
```

**Test production build locally:**
```bash
dotnet publish -c Release -o ./publish
cd ./publish
ASPNETCORE_ENVIRONMENT=Production dotnet PathfinderPhotography.dll
```

**View logs in production:**
```bash
sudo journalctl -u pathfinder-photography -f
```

## Licensing

- All contributions will be licensed under the MIT License
- Ensure any third-party content or images used have appropriate permissions
- Provide attribution for external resources

## Questions and Support

If you have questions or need help:
- Check existing issues and discussions
- Review [SETUP.md](../SETUP.md) for development setup
- Review [DEPLOY.md](../DEPLOY.md) for production
- Open an issue with your question
- Reach out to the project maintainers

Thank you for contributing to this educational project! 🎉

---

## Guidance for Copilot when writing documentation

Purpose: provide explicit, actionable rules so Copilot produces consistent, well-formatted, and audience-appropriate Markdown documentation for this project.

- Prefer Markdown native elements; avoid HTML unless necessary.
- Use headings to show document structure. Start with a single H1 and use H2/H3 for sub-sections.
- Keep paragraphs short (1-3 sentences). Use bulleted or numbered lists for steps and enumerations.
- Use backticks for inline code, file names, directories, function names, classes, and commands (e.g., `SETUP.md`, `dotnet run`, `PathfinderPhotography.AppHost`).
- Use fenced code blocks for commands and code examples. Specify language for syntax highlighting (e.g., ```bash, ```csharp).
- When documenting commands, show the full command and a short explanation on the next line.
- Provide examples and expected outputs for commands where helpful.
- Ensure all repository-relative links use correct relative paths (e.g., `[SETUP.md](../SETUP.md)`). Verify links exist before referencing them.
- Use accessible language: prefer simple sentences, define jargon, and include a short "Who this is for" note when the audience may vary.
- For content aimed at Pathfinders (10-15 years old), include brief explanations for why steps are done and actionable next steps.
- When writing step-by-step guides, use numbered lists and include prerequisite checks at the top.
- Keep list items concise; break complex steps into sub-steps.
- Add a "Troubleshooting" section for common errors and how to diagnose them (include exact error text and suggested checks).
- Add a "Verification" or "How to test" section that shows how to confirm the step worked.
- For configuration examples, include realistic default values but never include secrets or real credentials. Add a placeholder pattern: `<YOUR_VALUE_HERE>`.
- Use consistent tense and voice (imperative for instructions).
- Keep line length reasonable (~80-100 chars) to help readability in editors.
- Use emojis sparingly and only when they add clarity (e.g., checklists).
- When adding or updating docs, update the `DEPLOYMENT_CHECKLIST.md` or related index files if applicable.

Checklist for generated docs (use this as a final-pass template):
1. Title and short description (one sentence)
2. Audience and prerequisites
3. Step-by-step instructions with code blocks and examples
4. Verification steps and expected results
5. Troubleshooting guidance for common failures
6. Links to related docs and references
7. No secrets included
8. Short summary or next steps

Examples (format):

Title: How to run the app locally

Short description: Run the Blazor Server app locally for development.

Prerequisites:
1. .NET 9 SDK installed
2. PostgreSQL running and a `pathfinder` database created

Steps:
1. Restore packages:

```bash
dotnet restore
```

2. Run the app:

```bash
dotnet run --project PathfinderPhotography.AppHost
```

Verification:
- Open `https://localhost:5001` in your browser and verify the home page loads.

Troubleshooting:
- If port conflict occurs, run `dotnet run --urls "https://localhost:5002"` and retry.


Notes for Copilot authors:
- When in doubt, ask for clarification instead of guessing architecture or sensitive values.
- Keep suggestions short and focused; prefer linking to existing docs in the repo over creating new ones.
- When creating or editing docs, ensure changes follow existing naming and style patterns in this repo.

---
