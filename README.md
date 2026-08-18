# SOACS GridWatch

<p align="center">
  <img src="Assets/GitHub-Logo.jpg" alt="SOACS GridWatch" width="400">
</p>

**Mission-focused network monitoring with ICMP, TCP, UDP, ARP discovery, priority polling, and real-time operational awareness.**

SOACS GridWatch is a Windows desktop network-monitoring application built for mission systems and disconnected environments where operators need immediate visibility into device and service availability without unnecessary complexity.

## Production baseline

- **Status:** Live / Production
- **Assembly version:** 1.0.0.0
- **Platform:** Windows
- **Framework:** .NET Framework 4.8
- **UI:** WPF
- **Build target:** Any CPU
- **NuGet:** No external packages required

`main` represents the stable production baseline. Active development should occur on `develop` or a feature branch and be merged into `main` only after testing and release approval.

## Core capabilities

- ICMP reachability monitoring
- TCP service monitoring
- UDP monitoring
- ARP-based network discovery
- Up to 20 configurable monitored targets
- Target name, IP address, protocol, and port configuration
- Priority-based polling intervals
- Manual connectivity checks
- Operator-friendly response/status display
- Compact Summary View for rapid system awareness
- Profile/configuration management
- Local logging and export support
- Operator documentation included with the application

## Priority polling

GridWatch supports different monitoring intervals based on operational priority:

| Priority | Poll interval |
| --- | ---: |
| Critical | 2 seconds |
| High | 5 seconds |
| Normal | 10 seconds |
| Low | 30 seconds |

This allows the most important mission systems to be checked more aggressively while reducing unnecessary traffic for lower-priority devices.

## Monitoring behavior

GridWatch presents protocol-appropriate responses rather than forcing every target into a single ping-style status model.

Examples include:

- ICMP response time in milliseconds
- TCP connection status
- UDP response/receive status
- Reachable/unreachable state indicators

The Summary View provides a reduced operator display showing monitored target status, name, and address for quick reference.

## Build

### Requirements

- Windows
- Visual Studio with .NET Framework desktop development support
- .NET Framework 4.8 Developer Pack

### Build steps

1. Clone or download the repository.
2. Open the GridWatch solution in Visual Studio.
3. Select `Debug` or `Release` / `Any CPU`.
4. Build the solution.
5. Output is generated under `bin\Debug\` or `bin\Release\`.

The project uses standard .NET Framework references and is suitable for offline development environments.

## Repository structure

```text
SOACS-GridWatch/
├── Assets/              Application logos and icon assets
├── Docs/                Operator guide, Help, and What's New documentation
├── Models/              Target and enum models
├── Properties/          Assembly metadata
├── Resources/           GridWatch graphics and resources
├── Services/            Monitoring, discovery, configuration, and command services
├── App.xaml             WPF application definition
├── MainWindow.xaml      Primary GridWatch UI
├── MainWindow.xaml.cs   Primary application logic
├── SplashWindow.xaml    Startup splash screen
├── SOACS.GridWatch.csproj
├── CHANGELOG.md
└── README.md
```

Historical `README_v*.txt` files are retained as development records from earlier internal GridWatch iterations. The GitHub production baseline is identified by the assembly and release metadata rather than those historical package names.

## Runtime data

Generated runtime data, logs, exports, temporary files, build outputs, Visual Studio workspace files, and packaged releases should not be committed to source control. These are excluded through `.gitignore`.

## Development workflow

```text
main
  Stable / production baseline

└── develop
      Integrated development work

      └── feature/<short-description>
            Larger or isolated changes
```

For routine development:

1. Start from `develop`.
2. Use a `feature/*` branch for larger isolated changes.
3. Build and test locally.
4. Merge completed work into `develop`.
5. Merge `develop` into `main` only after production testing/approval.
6. Tag production releases in GitHub.

See [CONTRIBUTING.md](CONTRIBUTING.md) for repository workflow details.

## Status

**SOACS GridWatch is live production software.**

This repository establishes the controlled source baseline for the fielded GridWatch application.

---

**SOACS GridWatch**
