using CoreShift8.Core.Models;

namespace CoreShift8.Migration.Services;

public class MigrationEngine
{
    public Task<MigrationPlan> GenerateMigrationPlanAsync(ScanResult scanResult)
    {
        var plan = new MigrationPlan();
        
        foreach (var project in scanResult.LegacyProjects)
        {
            AddProjectMigrationSteps(plan, project);
        }
        
        // Calculate effort estimation
        plan.Effort = CalculateEffort(plan);
        
        // Add warnings
        AddWarnings(plan, scanResult);
        
        return Task.FromResult(plan);
    }
    
    private void AddProjectMigrationSteps(MigrationPlan plan, ProjectInfo project)
    {
        // Step 1: Update project file to SDK-style
        plan.Steps.Add(new MigrationStep
        {
            Title = $"Convert {project.Name} to SDK-style project",
            Description = $"Update {project.Name}.csproj to use modern SDK-style format",
            Type = MigrationStepType.UpdateProjectFile,
            EstimatedMinutes = 15,
            ProjectPath = project.Path,
            Requirements = new List<string>
            {
                "Backup original project file",
                "Convert to SDK-style format",
                "Update target framework to net8.0"
            }
        });
        
        // Step 2: Convert packages.config to PackageReference
        if (project.UsesPackagesConfig)
        {
            plan.Steps.Add(new MigrationStep
            {
                Title = $"Convert packages.config to PackageReference for {project.Name}",
                Description = "Migrate from packages.config to modern PackageReference format",
                Type = MigrationStepType.UpdatePackageReferences,
                EstimatedMinutes = 10,
                ProjectPath = project.Path,
                Requirements = new List<string>
                {
                    "Convert packages.config entries to PackageReference",
                    "Remove packages.config file",
                    "Update assembly references"
                }
            });
        }
        
        // Step 3: Migrate Entity Framework 6 to EF Core
        if (project.UsesEntityFramework6)
        {
            plan.Steps.Add(new MigrationStep
            {
                Title = $"Migrate Entity Framework 6 to EF Core for {project.Name}",
                Description = "Replace EF6 with EF Core and update related code",
                Type = MigrationStepType.UpdateCodeFiles,
                EstimatedMinutes = 120, // This is complex
                ProjectPath = project.Path,
                Requirements = new List<string>
                {
                    "Remove EntityFramework package",
                    "Add Microsoft.EntityFrameworkCore packages",
                    "Update DbContext class",
                    "Update connection string configuration",
                    "Update LINQ queries if needed",
                    "Review and update migrations"
                }
            });
        }
        
        // Step 4: Update configuration files
        if (project.ConfigFiles.Any())
        {
            plan.Steps.Add(new MigrationStep
            {
                Title = $"Update configuration for {project.Name}",
                Description = "Migrate app.config/web.config to modern configuration",
                Type = MigrationStepType.UpdateConfigFiles,
                EstimatedMinutes = 30,
                ProjectPath = project.Path,
                Requirements = new List<string>
                {
                    "Convert app.config to appsettings.json (if applicable)",
                    "Update connection strings format",
                    "Migrate configuration sections"
                }
            });
        }
        
        // Step 5: Update incompatible API usage
        plan.Steps.Add(new MigrationStep
        {
            Title = $"Review and update API usage for {project.Name}",
            Description = "Check for .NET Framework specific APIs and replace with .NET 8 equivalents",
            Type = MigrationStepType.Manual,
            EstimatedMinutes = 60,
            ProjectPath = project.Path,
            Requirements = new List<string>
            {
                "Review usage of System.Web (if any)",
                "Check for removed APIs",
                "Update deprecated method calls",
                "Test compilation"
            }
        });
    }
    
    private EstimatedEffort CalculateEffort(MigrationPlan plan)
    {
        var totalMinutes = plan.Steps.Sum(s => s.EstimatedMinutes);
        var totalHours = (int)Math.Ceiling(totalMinutes / 60.0);
        
        var complexity = totalHours switch
        {
            <= 2 => ComplexityLevel.Low,
            <= 8 => ComplexityLevel.Medium,
            <= 24 => ComplexityLevel.High,
            _ => ComplexityLevel.VeryHigh
        };
        
        var breakdown = plan.Steps
            .GroupBy(s => s.Type)
            .ToDictionary(
                g => g.Key.ToString(),
                g => (int)Math.Ceiling(g.Sum(s => s.EstimatedMinutes) / 60.0)
            );
        
        return new EstimatedEffort
        {
            TotalHours = totalHours,
            Complexity = complexity,
            BreakdownByCategory = breakdown
        };
    }
    
    private void AddWarnings(MigrationPlan plan, ScanResult scanResult)
    {
        // Add warnings based on scan results
        var ef6Projects = scanResult.LegacyProjects.Where(p => p.UsesEntityFramework6).Count();
        if (ef6Projects > 0)
        {
            plan.Warnings.Add($"Found {ef6Projects} project(s) using Entity Framework 6. Migration to EF Core may require code changes.");
        }
        
        var oldFrameworkProjects = scanResult.LegacyProjects.Where(p => 
            p.FrameworkVersion.StartsWith("v4.0") || p.FrameworkVersion.StartsWith("v4.5")).Count();
        if (oldFrameworkProjects > 0)
        {
            plan.Warnings.Add($"Found {oldFrameworkProjects} project(s) using older .NET Framework versions. Some APIs may not be available in .NET 8.");
        }
        
        var webProjects = scanResult.LegacyProjects.Where(p => 
            p.ConfigFiles.Any(c => c.ToLower().Contains("web.config"))).Count();
        if (webProjects > 0)
        {
            plan.Warnings.Add($"Found {webProjects} web project(s). Consider migrating to ASP.NET Core for full .NET 8 compatibility.");
        }
        
        if (scanResult.Issues.Any())
        {
            plan.Warnings.Add($"Encountered {scanResult.Issues.Count} issue(s) during scanning. Review scan results carefully.");
        }
    }
}
