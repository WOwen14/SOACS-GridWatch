# Contributing to SOACS GridWatch

GridWatch uses a simple controlled branch workflow intended to keep the production baseline stable while allowing active development and testing.

## Branches

### `main`

Production branch. This branch should contain only tested, approved production source.

### `develop`

Primary integration branch for ongoing GridWatch development.

### `feature/<short-description>`

Use feature branches for larger, isolated, or experimental changes.

Examples:

```text
feature/summary-view-update
feature/monitoring-hardening
feature/profile-management
```

## Development workflow

1. Start work from `develop`.
2. Create a feature branch when the change is large enough to isolate.
3. Build and test the application locally.
4. Confirm existing monitoring behavior still works.
5. Merge completed feature work into `develop`.
6. Perform production/release testing from `develop`.
7. Open a pull request from `develop` to `main`.
8. Merge only after the build is considered production ready.
9. Tag the production release.

## Testing expectations

Changes should be checked against the primary GridWatch functions affected by the work, including as applicable:

- ICMP monitoring
- TCP monitoring
- UDP monitoring
- ARP discovery
- Priority polling
- Manual connectivity tests
- Summary View
- Configuration/profile loading and saving
- Logging/export behavior
- UI scaling and layout
- Application startup/shutdown

## Repository hygiene

Do not commit generated or local-only content such as:

- `bin/`
- `obj/`
- `.vs/`
- Debug symbols or compiled binaries
- Runtime logs
- Exported reports
- Local user settings
- Temporary/backup files
- ZIP release packages

Production release packages should be attached to GitHub Releases rather than stored as source files.

## Versioning

The initial GitHub production baseline uses the application assembly version `1.0.0.0`. Future production releases should use clear GitHub tags such as `v1.1.0`, `v1.2.0`, or `v2.0.0`, with matching entries in `CHANGELOG.md`.
