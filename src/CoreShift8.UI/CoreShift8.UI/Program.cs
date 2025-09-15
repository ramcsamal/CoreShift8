using CoreShift8.Core.Models;
using CoreShift8.Scanner.Services;
using CoreShift8.Migration.Services;

namespace CoreShift8.UI;

public class Program
{
    private static readonly SolutionScanner _scanner = new();
    private static readonly MigrationEngine _migrationEngine = new();
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                        CoreShift8                           ║");
        Console.WriteLine("║       .NET Framework to .NET 8 Migration Tool             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        if (args.Length > 0)
        {
            // Command line mode
            await ProcessCommandLineAsync(args);
        }
        else
        {
            // Interactive mode
            await RunInteractiveModeAsync();
        }
    }
    
    private static async Task ProcessCommandLineAsync(string[] args)
    {
        var command = args[0].ToLower();
        
        switch (command)
        {
            case "scan":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: CoreShift8.UI scan <solution-path>");
                    return;
                }
                await ScanSolutionAsync(args[1]);
                break;
                
            case "plan":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: CoreShift8.UI plan <solution-path>");
                    return;
                }
                await GenerateMigrationPlanAsync(args[1]);
                break;
                
            case "help":
            case "--help":
            case "-h":
                ShowHelp();
                break;
                
            default:
                Console.WriteLine($"Unknown command: {command}");
                ShowHelp();
                break;
        }
    }
    
    private static async Task RunInteractiveModeAsync()
    {
        while (true)
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. Scan Solution");
            Console.WriteLine("2. Generate Migration Plan");
            Console.WriteLine("3. Help");
            Console.WriteLine("4. Exit");
            Console.Write("\nEnter your choice (1-4): ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await ScanSolutionInteractiveAsync();
                    break;
                case "2":
                    await GenerateMigrationPlanInteractiveAsync();
                    break;
                case "3":
                    ShowHelp();
                    break;
                case "4":
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }
    
    private static async Task ScanSolutionInteractiveAsync()
    {
        Console.Write("\nEnter the path to your solution file (.sln): ");
        var solutionPath = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            Console.WriteLine("Invalid path.");
            return;
        }
        
        await ScanSolutionAsync(solutionPath);
    }
    
    private static async Task GenerateMigrationPlanInteractiveAsync()
    {
        Console.Write("\nEnter the path to your solution file (.sln): ");
        var solutionPath = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            Console.WriteLine("Invalid path.");
            return;
        }
        
        await GenerateMigrationPlanAsync(solutionPath);
    }
    
    private static async Task ScanSolutionAsync(string solutionPath)
    {
        try
        {
            Console.WriteLine($"\n🔍 Scanning solution: {solutionPath}");
            Console.WriteLine("Please wait...\n");
            
            var scanResult = await _scanner.ScanSolutionAsync(solutionPath);
            
            Console.WriteLine("📊 SCAN RESULTS");
            Console.WriteLine("================");
            Console.WriteLine($"Solution: {scanResult.SolutionPath}");
            Console.WriteLine($"Scan Time: {scanResult.ScanTime:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();
            
            if (scanResult.LegacyProjects.Any())
            {
                Console.WriteLine($"🏗️  Legacy Projects Found ({scanResult.LegacyProjects.Count}):");
                foreach (var project in scanResult.LegacyProjects)
                {
                    Console.WriteLine($"  • {project.Name}");
                    Console.WriteLine($"    Framework: {project.FrameworkVersion}");
                    Console.WriteLine($"    Type: {project.Type}");
                    Console.WriteLine($"    EF6: {(project.UsesEntityFramework6 ? "Yes" : "No")}");
                    Console.WriteLine($"    packages.config: {(project.UsesPackagesConfig ? "Yes" : "No")}");
                    if (project.ConfigFiles.Any())
                    {
                        Console.WriteLine($"    Config files: {string.Join(", ", project.ConfigFiles)}");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("✅ No legacy projects found.");
            }
            
            if (scanResult.ModernProjects.Any())
            {
                Console.WriteLine($"✨ Modern Projects ({scanResult.ModernProjects.Count}):");
                foreach (var project in scanResult.ModernProjects)
                {
                    Console.WriteLine($"  • {project.Name} ({project.FrameworkVersion})");
                }
                Console.WriteLine();
            }
            
            if (scanResult.Issues.Any())
            {
                Console.WriteLine($"⚠️  Issues Found ({scanResult.Issues.Count}):");
                foreach (var issue in scanResult.Issues)
                {
                    Console.WriteLine($"  • {issue}");
                }
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error scanning solution: {ex.Message}");
        }
    }
    
    private static async Task GenerateMigrationPlanAsync(string solutionPath)
    {
        try
        {
            Console.WriteLine($"\n🔍 Scanning solution: {solutionPath}");
            var scanResult = await _scanner.ScanSolutionAsync(solutionPath);
            
            if (!scanResult.LegacyProjects.Any())
            {
                Console.WriteLine("✅ No legacy projects found. No migration needed.");
                return;
            }
            
            Console.WriteLine($"\n📋 Generating migration plan...");
            var migrationPlan = await _migrationEngine.GenerateMigrationPlanAsync(scanResult);
            
            Console.WriteLine("\n" + migrationPlan.ToString());
            
            // Offer to save the plan
            Console.Write("\nSave migration plan to file? (y/n): ");
            var save = Console.ReadLine()?.ToLower();
            
            if (save == "y" || save == "yes")
            {
                var fileName = $"migration-plan-{DateTime.Now:yyyyMMdd-HHmmss}.md";
                await File.WriteAllTextAsync(fileName, migrationPlan.ToString());
                Console.WriteLine($"✅ Migration plan saved to: {fileName}");
                
                // Also save as JSON
                var jsonFileName = $"migration-plan-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                var json = System.Text.Json.JsonSerializer.Serialize(migrationPlan, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(jsonFileName, json);
                Console.WriteLine($"✅ Migration plan (JSON) saved to: {jsonFileName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating migration plan: {ex.Message}");
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("\n📖 HELP");
        Console.WriteLine("=========");
        Console.WriteLine("CoreShift8 is a tool for migrating legacy .NET Framework + EF6 projects to .NET 8 + EF Core.");
        Console.WriteLine();
        Console.WriteLine("Command Line Usage:");
        Console.WriteLine("  CoreShift8.UI scan <solution-path>    - Scan a solution for legacy projects");
        Console.WriteLine("  CoreShift8.UI plan <solution-path>    - Generate a migration plan");
        Console.WriteLine("  CoreShift8.UI help                    - Show this help");
        Console.WriteLine();
        Console.WriteLine("Interactive Mode:");
        Console.WriteLine("  Run without arguments to enter interactive mode");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  • Detects .NET Framework projects");
        Console.WriteLine("  • Identifies Entity Framework 6 usage");
        Console.WriteLine("  • Finds packages.config files");
        Console.WriteLine("  • Generates detailed migration plans");
        Console.WriteLine("  • Provides effort estimation");
        Console.WriteLine("  • Exports plans in Markdown and JSON");
        Console.WriteLine();
    }
}
