# CoreShift8

A powerful WinForms .NET 8 application designed to migrate legacy .NET Framework + Entity Framework 6 projects to .NET 8 + EF Core.

## 🚀 Features

- **Solution Scanner**: Automatically detects legacy .NET Framework projects in a solution
- **EF6 Detection**: Identifies projects using Entity Framework 6 for targeted migration
- **Package Analysis**: Detects packages.config usage and outdated package references
- **Migration Planning**: Generates detailed migration plans with step-by-step instructions
- **Effort Estimation**: Provides time estimates and complexity analysis
- **Interactive & CLI Modes**: Use via command line or interactive console interface
- **Report Generation**: Exports migration plans in Markdown and JSON formats
- **Sample Projects**: Includes realistic legacy project examples for testing

## 🏗️ Architecture

The solution is organized into focused, reusable components:

```
src/
├── CoreShift8.Core/         # Domain models and shared types
├── CoreShift8.Scanner/      # Solution scanning and analysis logic
├── CoreShift8.Migration/    # Migration planning and rule engine
└── CoreShift8.UI/          # Console application interface
```

## 📋 Requirements

- .NET 8 SDK
- Windows, macOS, or Linux

## 🛠️ Installation & Usage

### Build from Source

```bash
git clone https://github.com/ramcsamal/CoreShift8.git
cd CoreShift8
dotnet build
```

### Command Line Usage

```bash
# Scan a solution for legacy projects
dotnet run --project src/CoreShift8.UI/CoreShift8.UI scan path/to/your/solution.sln

# Generate a migration plan
dotnet run --project src/CoreShift8.UI/CoreShift8.UI plan path/to/your/solution.sln

# Show help
dotnet run --project src/CoreShift8.UI/CoreShift8.UI help
```

### Interactive Mode

```bash
dotnet run --project src/CoreShift8.UI/CoreShift8.UI
```

## 🧪 Testing with Sample Projects

The repository includes realistic legacy sample projects for testing:

```bash
# Test with the included legacy sample
dotnet run --project src/CoreShift8.UI/CoreShift8.UI scan samples/LegacySample/LegacySample.sln
```

The sample solution includes:
- **LegacyWebApp**: ASP.NET MVC 5 application with EF6
- **LegacyClassLibrary**: Class library with EF6 and packages.config
- **LegacyConsoleApp**: .NET Framework 4.6.1 console application

## 📊 What CoreShift8 Detects

### Legacy Indicators
- ✅ .NET Framework target frameworks (v4.x)
- ✅ Entity Framework 6 usage
- ✅ packages.config files
- ✅ Old-style project files (.NET Framework format)
- ✅ Web.config and App.config files
- ✅ Legacy assembly references

### Migration Plan Includes
- 🔧 Convert to SDK-style project files
- 📦 Migrate packages.config to PackageReference
- 🔄 EF6 to EF Core migration steps
- ⚙️ Configuration modernization
- 🔍 API compatibility review
- ⏱️ Time estimates and complexity analysis

## 📈 Example Output

### Scan Results
```
📊 SCAN RESULTS
================
Solution: samples/LegacySample/LegacySample.sln
Scan Time: 2025-01-15 10:30:00 UTC

🏗️  Legacy Projects Found (3):
  • LegacyWebApp
    Framework: v4.7.2
    Type: ClassLibrary
    EF6: Yes
    packages.config: Yes
    Config files: Web.config, packages.config

  • LegacyClassLibrary
    Framework: v4.7.2
    Type: ClassLibrary
    EF6: Yes
    packages.config: Yes
    Config files: App.config, packages.config

  • LegacyConsoleApp
    Framework: v4.6.1
    Type: ConsoleApp
    EF6: No
    packages.config: No
    Config files: App.config
```

### Migration Plan
```
=== MIGRATION PLAN ===
Generated: 2025-01-15 10:30:00 UTC
Estimated Time: 10 hours
Complexity: High

⚠️ WARNINGS:
  • Found 2 project(s) using Entity Framework 6. Migration to EF Core may require code changes.
  • Found 1 web project(s). Consider migrating to ASP.NET Core for full .NET 8 compatibility.

📋 MIGRATION STEPS:
01. Convert LegacyWebApp to SDK-style project
    Description: Update LegacyWebApp.csproj to use modern SDK-style format
    Type: UpdateProjectFile
    Estimated Time: 15 minutes
    ...
```

## 🔧 Extensibility

CoreShift8 is designed with extensibility in mind:

- **Rule Engine**: Add custom migration rules in `CoreShift8.Migration`
- **Scanner Plugins**: Extend `CoreShift8.Scanner` for additional project types
- **Report Formats**: Add new export formats in the reporting system
- **UI Modes**: Replace the console UI with WinForms, WPF, or web interface

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all builds pass
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔮 Future Enhancements

- **Backup System**: Automatic project backup before migration
- **Git Integration**: Create git checkpoints during migration
- **Dry Run Mode**: Preview changes without applying them
- **Visual Migration Wizard**: WinForms/WPF GUI for guided migration
- **Batch Processing**: Process multiple solutions simultaneously
- **CI/CD Integration**: GitHub Actions for automated migration analysis

## 📞 Support

For questions, issues, or contributions, please visit the [GitHub repository](https://github.com/ramcsamal/CoreShift8).

---

**CoreShift8** - Making .NET Framework to .NET 8 migration simple and predictable.