# Applaio architecture

## Principles

Applaio starts as a modular monolith. The desktop UI is not allowed to contain persistence or spreadsheet-specific business logic.

### Layers

- **Applaio.Domain** — recruitment entities, value objects and domain rules.
- **Applaio.Application** — use cases and ports/interfaces.
- **Applaio.Infrastructure** — SQLite/EF Core, imports and future external integrations.
- **Applaio.Desktop** — WinUI 3 composition root and presentation.

Dependencies always point inward:

`Desktop -> Application <- Infrastructure`
`Application -> Domain <- Infrastructure`

## Local-first storage

Recruitment data is stored in SQLite under the current user's local application data directory.

The schema will move to explicit EF Core migrations before the first distributable release. `EnsureCreated` is intentionally temporary during bootstrap.

## Spreadsheet migration

Spreadsheet import is a boundary, not a domain concern.

The migration process will be:

1. read workbook,
2. detect/validate source columns,
3. normalize rows into import DTOs,
4. show validation problems,
5. map accepted rows to domain objects,
6. preview deduplication,
7. persist the migration transactionally,
8. record an import fingerprint so the same workbook is not accidentally imported twice.

The existing recruitment workbook is the specification for the first adapter, but the core model will not depend on its exact column names.

## UI

The app targets Windows 11 and uses WinUI 3 with native system backdrops, Fluent controls and responsive desktop layouts.

Visual goals:

- Acrylic/Mica glass surfaces,
- compact Windows 11 navigation,
- clear stage/status colors,
- native light/dark theme support,
- keyboard accessibility,
- high-DPI support,
- no custom chrome unless it adds real value.

## Planned feature modules

- Dashboard
- Recruitments
- Companies
- Interviews / timeline
- Contacts
- Documents / CV variants
- Import
- Analytics
- Settings
