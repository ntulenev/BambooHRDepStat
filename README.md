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

## Configuration

Configuration is read from `src/BambooHR.Reporting/appsettings.json` under the `BambooHR` section.

Minimal config:

```json
{
  "BambooHR": {
    "Organization": "your-company-subdomain",
    "Token": "your-api-token",
    "EmployeeId": 42
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


## Output Files

By default, generated files are written under `reports/` relative to the application output directory.

Generated filenames include a timestamp suffix, for example:

- `reports/bamboohr-hierarchy-report_20260315_103522.html`
- `reports/bamboohr-hierarchy-report_20260315_103522.pdf`