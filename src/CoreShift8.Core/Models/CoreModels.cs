namespace CoreShift8.Core.Models;

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FrameworkVersion { get; set; } = string.Empty;
    public bool UsesEntityFramework6 { get; set; }
    public bool UsesPackagesConfig { get; set; }
    public List<string> References { get; set; } = new();
    public List<string> NuGetPackages { get; set; } = new();
    public List<string> ConfigFiles { get; set; } = new();
    public ProjectType Type { get; set; }
}

public enum ProjectType
{
    Unknown,
    ConsoleApp,
    ClassLibrary,
    WebApplication,
    WinForms,
    WPF,
    Service
}

public class ScanResult
{
    public string SolutionPath { get; set; } = string.Empty;
    public List<ProjectInfo> LegacyProjects { get; set; } = new();
    public List<ProjectInfo> ModernProjects { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
}

public class MigrationPlan
{
    public List<MigrationStep> Steps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public EstimatedEffort Effort { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== MIGRATION PLAN ===");
        sb.AppendLine($"Generated: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Estimated Time: {Effort.TotalHours} hours");
        sb.AppendLine($"Complexity: {Effort.Complexity}");
        sb.AppendLine();
        
        if (Warnings.Any())
        {
            sb.AppendLine("⚠️ WARNINGS:");
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"  • {warning}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("📋 MIGRATION STEPS:");
        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            sb.AppendLine($"{i + 1:D2}. {step.Title}");
            sb.AppendLine($"    Description: {step.Description}");
            sb.AppendLine($"    Type: {step.Type}");
            sb.AppendLine($"    Estimated Time: {step.EstimatedMinutes} minutes");
            if (step.Requirements.Any())
            {
                sb.AppendLine("    Requirements:");
                foreach (var req in step.Requirements)
                {
                    sb.AppendLine($"      - {req}");
                }
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}

public class MigrationStep
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MigrationStepType Type { get; set; }
    public int EstimatedMinutes { get; set; }
    public List<string> Requirements { get; set; } = new();
    public string ProjectPath { get; set; } = string.Empty;
}

public enum MigrationStepType
{
    UpdateProjectFile,
    UpdatePackageReferences,
    UpdateCodeFiles,
    UpdateConfigFiles,
    AddNewFiles,
    RemoveFiles,
    Manual
}

public class EstimatedEffort
{
    public int TotalHours { get; set; }
    public ComplexityLevel Complexity { get; set; }
    public Dictionary<string, int> BreakdownByCategory { get; set; } = new();
}

public enum ComplexityLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}
