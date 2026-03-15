# BambooHRDepStat

Console utility for building hierarchy-based BambooHR reports and exporting them to console, HTML, and PDF.

## What It Does

The app starts from one root employee, loads that employee's reporting hierarchy from BambooHR, enriches people with profile data, and generates:

- hierarchy tree with employee details
- team summaries
- team size and grade distribution
- location distribution
- country to city breakdown
- age distribution
- company tenure distribution
- availability for the current work week

Output targets:

- interactive console report via `Spectre.Console`
- standalone HTML report with dark dashboard styling and charts
- PDF report via `QuestPDF`

## Current Report Content

For each employee in the selected hierarchy, the report includes:

- employee id
- display name
- department
- job title
- location
- birth date
- employment start date
- manager
- availability for the report week

Additional report sections:

- `Job Titles`
- `Teams`
  Team means a manager plus direct reports that do not have their own subordinates.
- `Team Size`
- `Team Grade Distribution`
- `Location Distribution`
- `Location Distribution By Country Cities`
- `Age Distribution`
- `Company Tenure Distribution`

## Availability Rules

Availability is built from BambooHR `Who's Out` data for the current work week.

Supported availability types:

- personal time off
- public holidays

Public holiday matching is best-effort:

- if the selected hierarchy contains employees from only one country, holidays are applied to everyone in that hierarchy
- if the hierarchy contains multiple countries, a holiday is applied only when its name matches the employee's country, city, or location string

Example:

- `Malta National Day` will be applied to employees whose BambooHR location resolves to Malta

Why this is needed:

- BambooHR `Who's Out` holiday items do not provide reliable employee-level or location-level assignment metadata in the current integration path, so exact holiday-to-employee mapping is not always available from the API response alone

## Console UX

The app shows interactive loading progress while it is fetching data from BambooHR:

- current stage
- spinner
- progress bar
- employee profile load progress

After loading finishes, the console report is printed immediately.

## HTML Report

The HTML report is generated automatically after the report is built.

Current HTML behavior:

- dark Notion-like theme
- donut chart for team size distribution
- stacked bar chart for team grade distribution
- `Available` shown as green
- time off and holiday availability shown as red
- generated file opens automatically in the default browser

## PDF Report

The PDF report is generated automatically after the report is built.

It contains the same business data as the console and HTML outputs, formatted for print/export.

## Configuration

Configuration is read from `src/BambooHR.Reporting/appsettings.json` under the `BambooHR` section.

Minimal config:

```json
{
  "BambooHR": {
    "Organization": "your-company-subdomain",
    "Token": "your-api-token",
    "EmployeeId": 38
  }
}
```

Full config with report options:

```json
{
  "BambooHR": {
    "Organization": "your-company-subdomain",
    "Token": "your-api-token",
    "EmployeeId": 38,
    "Html": {
      "Enabled": true,
      "OpenInBrowser": true,
      "OutputPath": "reports/bamboohr-hierarchy-report.html"
    },
    "Pdf": {
      "Enabled": true,
      "OutputPath": "reports/bamboohr-hierarchy-report.pdf"
    }
  }
}
```

Configuration fields:

- `Organization`: BambooHR subdomain
- `Token`: BambooHR API token
- `EmployeeId`: root employee id for the hierarchy
- `Html.Enabled`: enable or disable HTML generation
- `Html.OpenInBrowser`: open generated HTML in the default browser
- `Html.OutputPath`: base output path for HTML report
- `Pdf.Enabled`: enable or disable PDF generation
- `Pdf.OutputPath`: base output path for PDF report

Notes:

- HTML and PDF output paths are automatically turned into dated filenames at runtime
- relative output paths are resolved against the app base directory
- do not commit real BambooHR tokens

## Output Files

By default, generated files are written under `reports/` relative to the application output directory.

Generated filenames include a timestamp suffix, for example:

- `reports/bamboohr-hierarchy-report_20260315_103522.html`
- `reports/bamboohr-hierarchy-report_20260315_103522.pdf`

## How Team Grades Are Derived

Grades are inferred from `jobTitle`.

Recognized labels include:

- `Junior`
- `Middle`
- `Senior`
- `Lead`
- `Team Lead`
- `Tech Lead`
- `Manager`
- `Director`
- `Head`

If no predefined label matches, the original `jobTitle` is used.

## Requirements

- .NET 10 SDK
- BambooHR API access
- network access to your BambooHR tenant

## Run

From the repository root:

```powershell
dotnet run --project src/BambooHR.Reporting/BambooHR.Reporting.csproj
```

## Test

Run all tests:

```powershell
dotnet test src/BambooHRSandbox.slnx
```

Build the reporting app:

```powershell
dotnet build src/BambooHR.Reporting/BambooHR.Reporting.csproj
```

## Project Structure

- `src/BambooHR.Reporting`
  Entry point and dependency injection
- `src/Logic`
  Report building and hierarchy processing
- `src/Infrastructure`
  BambooHR API client, console writer, HTML renderer, PDF renderer
- `src/Models`
  Report, employee, and configuration models
- `src/Abstractions`
  Interfaces used across layers
- `src/Logic.Tests`
  Unit tests for hierarchy/report logic

## Known Limitations

- public holiday assignment is heuristic for multi-country hierarchies
- team detection is intentionally narrow: manager plus direct leaf reports only
- HTML and PDF are generated after the full dataset is loaded, not progressively

## Typical Flow

1. Load BambooHR metadata and current work week.
2. Resolve the hierarchy relationship field.
3. Load current employees.
4. Load employee profile fields in parallel.
5. Build the selected hierarchy.
6. Load `Who's Out` data for the week.
7. Calculate teams and distributions.
8. Render console output.
9. Save HTML and PDF reports.
10. Open the HTML report in the browser.
