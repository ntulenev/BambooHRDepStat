# BambooHRDepStat

Console utility for building hierarchy-based BambooHR reports and exporting them to console, HTML, PDF, and CSV.

## What It Does

The app starts from one root employee, loads that employee's reporting hierarchy from BambooHR, enriches people with profile data, and generates:

- holidays table for the selected availability window
- hierarchy tree with employee details
- recent joiners table
- team summaries
- team size and grade distribution
- flat team reports with one table per team
- location distribution
- country to city breakdown
- age distribution
- company tenure distribution
- availability for a configurable forward-looking window

Output targets:

- interactive console report via `Spectre.Console`
- standalone HTML report with dark dashboard styling and charts
- PDF report via `QuestPDF`
- CSV employee export

## Current Report Content

For each employee in the selected hierarchy, the report includes:

- employee id
- display name
- department
- team
- job title
- location
- birth date
- employment start date
- manager
- availability for the report week

Additional report sections:

- `Holidays`
- `Hierarchy`
- `New Joiners`
- `Job Titles`
- `Teams`
  Team means a manager plus direct reports that do not have their own subordinates.
- `Team Size`
- `Team Grade Distribution`
- `Flat Team Reports`
  One flat table per team using the same columns as the `Hierarchy` table.
  This section is controlled by `ShowTeamReports` and is enabled by default.
- `Location Distribution`
- `Location Distribution By Country Cities`
- `Age Distribution`
- `Company Tenure Distribution`

## Team Field Resolution

The report includes a `Team` column for each employee.

The application resolves this value dynamically from BambooHR field metadata. In environments where BambooHR shows `Team` in the UI but does not expose it as a `team` API field, the app also supports fallback fields such as `division`.

For the Altenar BambooHR setup, the UI `Team` value is currently resolved from BambooHR field `division`.

## Availability Rules

Availability is built from BambooHR `Who's Out` data for the configured window:

- start: today
- end: today + `AvailabilityLookaheadDays`

Availability status is resolved like this:

- green `Available`: no matching unavailability entries
- yellow `Upcoming: ...`: employee is available today, but has a future unavailability inside the configured window
- red `Time off: ...` or `Holiday (...): ...`: employee is unavailable today

Supported availability types in the employee row:

- personal time off matched explicitly by `employeeId`
- holidays only when they are explicitly mapped in config through `HolidayCountryMappings` and the employee country matches

Holiday behavior:

- all holidays returned by BambooHR for the window are shown in the separate `Holidays` section
- the `Holidays` table also shows associated countries from config, if present
- if a holiday has no configured country mapping, it is still shown in `Holidays`, but it is not added to employee `Availability`

## Configuration

Configuration is read from `src/BambooHR.Reporting/appsettings.json` under the `BambooHR` section.

Minimal config:

```json
{
  "BambooHR": {
    "Organization": "your-company-subdomain",
    "Token": "your-api-token",
    "EmployeeId": 42,
    "AvailabilityLookaheadDays": 7,
    "RecentHirePeriodDays": 90,
    "ShowTeamReports": true,
    "HolidayCountryMappings": {
      "Good Friday": [ "Malta" ]
    }
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
    "AvailabilityLookaheadDays": 7,
    "RecentHirePeriodDays": 90,
    "ShowTeamReports": true,
    "HolidayCountryMappings": {
      "Good Friday": [ "Malta" ],
      "Worker's Day": [ "Malta", "Greece" ]
    },
    "Html": {
      "Enabled": true,
      "OpenInBrowser": true,
      "OutputPath": "reports/bamboohr-hierarchy-report.html"
    },
    "Pdf": {
      "Enabled": true,
      "OutputPath": "reports/bamboohr-hierarchy-report.pdf"
    },
    "Export": {
      "Enabled": true,
      "OpenByProcess": false,
      "OutputPath": "reports/bamboohr-employee-export.csv"
    }
  }
}
```

Configuration fields:

- `Organization`: BambooHR subdomain
- `Token`: BambooHR API token
- `EmployeeId`: root employee id for the hierarchy
- `AvailabilityLookaheadDays`: how many days ahead to include in the availability window, starting from today
- `RecentHirePeriodDays`: how many days back to include in the `New Joiners` section
- `ShowTeamReports`: show or hide the final `Flat Team Reports` section; default is `true`
- `HolidayCountryMappings`: explicit holiday-to-country associations used to add holidays into employee availability
- `Html.Enabled`: enable or disable HTML generation
- `Html.OpenInBrowser`: open generated HTML in the default browser
- `Html.OutputPath`: base output path for HTML report
- `Pdf.Enabled`: enable or disable PDF generation
- `Pdf.OutputPath`: base output path for PDF report
- `Export.Enabled`: enable or disable CSV generation
- `Export.OpenByProcess`: open generated CSV in the default associated application
- `Export.OutputPath`: base output path for CSV export

Example for gradually improving holiday mappings:

1. Run the report.
2. Check the `Holidays` section for entries that have no country association yet.
3. Add the missing holiday name to `HolidayCountryMappings` in `appsettings.json`.
4. Run the report again and verify that the holiday now appears in `Availability` for employees from the mapped countries.


## Output Files

By default, generated files are written under `reports/` relative to the application output directory.

Generated filenames include a timestamp suffix, for example:

- `reports/bamboohr-hierarchy-report_20260315_103522.html`
- `reports/bamboohr-hierarchy-report_20260315_103522.pdf`
- `reports/bamboohr-employee-export_20260315_103522.csv`
