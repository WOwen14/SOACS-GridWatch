SOACS GridWatch Professional v2.7
Configuration & Deployment Framework

Changes:
- Save Config now always acts as Save As.
- Save Config opens in %LOCALAPPDATA%\SOACS\GridWatch\Config.
- Load Config opens in the same Config folder.
- Open Config Folder opens the same Config folder.
- First launch creates the unified application data tree:
  %LOCALAPPDATA%\SOACS\GridWatch\Config
  %LOCALAPPDATA%\SOACS\GridWatch\Docs
  %LOCALAPPDATA%\SOACS\GridWatch\Logs
  %LOCALAPPDATA%\SOACS\GridWatch\Logs\Archive
  %LOCALAPPDATA%\SOACS\GridWatch\Exports
  %LOCALAPPDATA%\SOACS\GridWatch\Profiles
- Documentation is copied to the Docs folder on startup.
- Runtime log writes to Logs\GridWatch.log.
