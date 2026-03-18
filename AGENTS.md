# RunCat365 AGENTS.md

**Generated:** 2026-03-12
**Project:** RunCat 365 - Windows system monitoring tray application

## Build Commands

```bash
# Build the solution (x64 Debug)
dotnet build RunCat365.sln -p:Platform=x64 -p:Configuration=Debug

# Build for Release
dotnet build RunCat365.sln -p:Platform=x64 -p:Configuration=Release

# Build for specific platforms
dotnet build RunCat365.sln -p:Platform=x86
dotnet build RunCat365.sln -p:Platform=ARM64

# Run the application
dotnet run --project RunCat365/RunCat365.csproj

# Create MSIX package (requires Visual Studio)
# Right-click WapForStore project > Publish > Create App Packages
```

## Linting & Code Analysis

**No external linting tools configured.** Code style is enforced manually via:
- Allman indentation style (brace on new line)
- `var` keyword usage when type is obvious
- No comments in source code (use clear naming instead)
- No abbreviations (use `image` not `img`, `count` not `cnt`)

## Testing

**No unit tests exist in this repository.**
- Manual testing via Visual Studio debugger
- Test by running the application and verifying system tray behavior
- Test endless game by clicking "Endless Game" in context menu

## Code Style Guidelines

### Formatting
- **Indentation**: Allman style (braces on new line)
- **Line length**: Follow existing patterns (no hard limit)
- **Spacing**: Consistent with existing code

### Naming Conventions
- **PascalCase** for: Classes, Methods, Properties, Enums
- **camelCase** for: local variables, parameters
- **UPPER_SNAKE_CASE** for: Constants
- **Abbreviations**: Use all lowercase or all uppercase (e.g., `url`, `ID`, `UI`)
- **No abbreviations**: Use full words (`image` not `img`, `count` not `cnt`)

### Access Modifiers
- Use `internal` for types and members not exposed publicly
- Use `private` for implementation details
- Use `protected` only when inheritance is expected

### Language Features
- **Use `var`** when type is obvious from assignment
- **Pattern matching**: Use `switch` expressions where appropriate
- **Nullable reference types**: Enabled in project (`<Nullable>enable</Nullable>`)
- **Unsafe code**: Allowed (used in `BitmapExtension.cs` for image manipulation)

### Error Handling
- **Try-catch blocks**: Use sparingly, only when recovery is possible
- **Resource disposal**: Use `using` statements for `IDisposable` types
- **Exception messages**: Console output for errors (see `OpenRepository()` in `Program.cs`)

### Localization
- **Resource files**: All user-facing text in `Properties/Strings.resx`
- **Add new strings**: Must update all 7 language files simultaneously:
  - `Strings.resx` (English)
  - `Strings.zh-CN.resx` (Chinese Simplified)
  - `Strings.zh-TW.resx` (Chinese Traditional)
  - `Strings.fr.resx` (French)
  - `Strings.de.resx` (German)
  - `Strings.ja.resx` (Japanese)
  - `Strings.es.resx` (Spanish)
- **Fonts**: Language-specific fonts in localization notes (CLAUDE.md)

## Architecture Patterns

### Entry Point
- **Program.cs**: `Main()` method creates `RunCat365ApplicationContext`
- **Application lifecycle**: Managed by `ApplicationContext` for system tray apps

### Repository Pattern
- **CPURepository**: CPU usage via PerformanceCounter
- **GPURepository**: GPU usage monitoring
- **MemoryRepository**: Memory usage
- **StorageRepository**: Disk usage
- **NetworkRepository**: Network statistics
- Each repository has `Update()`, `Get()`, and `Close()` methods

### UI Management
- **ContextMenuManager**: Handles system tray icon, context menu, animations
- **Icon updates**: Thread-safe using `Lock` object
- **Form classes**: `EndlessGameForm` for mini-game

### Animation Flow
1. `fetchTimer` (1s) updates system info into `*Info` structs
2. `animateTimer` advances frames based on selected `SpeedSource`
3. `BitmapExtension` handles theme-aware icon recoloring

## Project Structure

```
RunCat365.sln
├── RunCat365/              # Main application project
│   ├── Program.cs          # Entry point
│   ├── *Repository.cs      # System info repositories
│   ├── ContextMenuManager.cs
│   ├── EndlessGameForm.cs  # Mini-game
│   └── Properties/
│       ├── Strings.resx    # Localization (7 languages)
│       ├── Resources.resx  # Embedded images
│       └── UserSettings.settings
└── WapForStore/            # MSIX packaging for Microsoft Store
```

## Important Notes

### Version Numbers
Update in **two places** when releasing:
1. `RunCat365/RunCat365.csproj`: `<Version>X.Y.Z</Version>` (3-digit)
2. `WapForStore/Package.appxmanifest`: `Version="X.Y.Z.0"` (4-digit)

### Platform Support
- Targets: x64, x86, ARM64
- Minimum OS: Windows 10 version 19041.0
- Framework: .NET 9.0 (Windows 10.0.26100.0)

### Store Distribution
- Microsoft Store package via WapForStore project
- Uses MSIX format
- Requires Partner Center submission

### Git Workflow
- Branch from `main`
- Single change per pull request
- English-only commits and PRs
- Follow existing commit message style
