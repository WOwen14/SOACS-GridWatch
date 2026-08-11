# Changelog

All notable GridWatch changes should be recorded here as the repository moves forward from the initial controlled production baseline.

## Production baseline — 1.0.0.0

Status: **Live / Production**

### Current capabilities

- ICMP, TCP, and UDP monitoring
- ARP discovery
- Up to 20 monitored targets
- Per-target priority settings
- Priority polling intervals:
  - Critical: 2 seconds
  - High: 5 seconds
  - Normal: 10 seconds
  - Low: 30 seconds
- Manual connectivity checks
- Protocol-appropriate response/status display
- Summary View for compact operator monitoring
- Profile/configuration management
- Logging and export support
- Integrated operator documentation
- WPF user interface
- .NET Framework 4.8 production build

## Historical development lineage

The repository contains retained `README_v*.txt` files documenting earlier internal GridWatch builds and UI iterations. These include RC and 2.x package labels used during development.

Those filenames are retained for traceability but do not replace the controlled production version identified by the application assembly and GitHub release/tag metadata.

## Future entries

Future production changes should be documented using release-oriented entries such as:

```text
## v1.1.0 — YYYY-MM-DD
### Added
### Changed
### Fixed
```
