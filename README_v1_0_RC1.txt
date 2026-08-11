SOACS GridWatch v1.0 RC1

Release Candidate Hardening Build

Changes:
- Visible app name changed to SOACS GridWatch.
- Subtitle changed to Network Operations Monitor.
- CheckNode converted to async Task.
- Check All now awaits manual checks and uses throttling.
- Added per-target overlap prevention for checks.
- Scheduler heartbeat changed to 1 second.
- Documentation/config folder Process.Start calls now use UseShellExecute.
- ComboBox hit target cleanup.
- Release metadata added.
- Profile timing retained: Critical 2 sec, High 5 sec, Normal 10 sec, Low 30 sec.
