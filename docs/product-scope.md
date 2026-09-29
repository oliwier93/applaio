# Product scope

## Primary goal

Replace the recruitment workbook with a Windows desktop application that makes the current state of every job application immediately visible while preserving the data that already matters in the user's workflow.

## First migration baseline

The supplied workbook contains 64 recruitment processes and is the baseline for migration 001.

The source workbook tracks:

- company,
- position / seniority label,
- status,
- priority,
- fit,
- source,
- recruiter,
- offer URL,
- start/application date,
- last contact date,
- next action,
- next-action due date,
- work model,
- location,
- cooperation/contract type,
- minimum and maximum rate,
- rate type,
- primary technology/language,
- key requirements,
- risks,
- notes.

The workbook also calculates days since last contact and days in process. Those values are derived in Applaio and are not persisted during import.

Placeholder values such as `b/d`, `n/d` and `Nie podano` are normalized to null.

## Workbook-derived status pipeline

1. To review
2. Recruiter contact
3. Replied
4. Applied
5. HR screening
6. Technical interview
7. Recruitment task
8. Next stage
9. Final interview
10. Offer
11. Paused
12. Ghosted
13. Rejected
14. Withdrawn
15. Accepted

Closed states are Rejected, Withdrawn and Accepted.

## UX direction

Recruitments require two complementary views:

- **List** — dense, sortable and filterable for fast daily work.
- **Pipeline** — visual stage columns for process progression.

The dashboard is an action center rather than a duplicate spreadsheet. It should highlight:

- all and active processes,
- applications sent,
- HR and technical interviews,
- offers and rejected processes,
- processes requiring action,
- recently started processes,
- upcoming follow-ups,
- important/high-fit opportunities,
- breakdown by status,
- source,
- work model,
- primary stack/language.

## Future modules

- recruitment timeline/history,
- recruiter/contact directory,
- CV/document version used for a given application,
- reminders and follow-ups,
- analytics and stage conversion,
- optional email/calendar integrations.

Personal recruitment data must remain local by default. The public source repository must not contain imported workbook data.
