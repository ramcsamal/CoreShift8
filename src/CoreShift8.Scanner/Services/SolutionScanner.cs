using CoreShift8.Core.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CoreShift8.Scanner.Services;

public class SolutionScanner
{
    public async Task<ScanResult> ScanSolutionAsync(string solutionPath)
    {
        var result = new ScanResult
        {
            SolutionPath = solutionPath,
            ScanTime = DateTime.UtcNow
        };

        try
        {
            var solutionDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;
            var projectPaths = await ExtractProjectPathsAsync(solutionPath);

            foreach (var projectPath in projectPaths)
            {
                var fullProjectPath = Path.Combine(solutionDir, projectPath);
                if (File.Exists(fullProjectPath))
                {
                    var projectInfo = await AnalyzeProjectAsync(fullProjectPath);
                    
                    if (IsLegacyProject(projectInfo))
                    {
                        result.LegacyProjects.Add(projectInfo);
                    }
                    else
                    {
                        result.ModernProjects.Add(projectInfo);
                    }
                }
                else
                {
                    result.Issues.Add($"Project file not found: {fullProjectPath}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add($"Error scanning solution: {ex.Message}");
        }

        return result;
    }

    private async Task<List<string>> ExtractProjectPathsAsync(string solutionPath)
    {
        var projectPaths = new List<string>();
        var solutionContent = await File.ReadAllTextAsync(solutionPath);
        
        // Regex to match project lines in .sln file
        var projectRegex = new Regex(@"Project\(""{[^}]+}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""", RegexOptions.IgnoreCase);
        var matches = projectRegex.Matches(solutionContent);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var projectPath = match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
                projectPaths.Add(projectPath);
            }
        }
        
        return projectPaths;
    }

    private async Task<ProjectInfo> AnalyzeProjectAsync(string projectPath)
    {
        var projectInfo = new ProjectInfo
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            Path = projectPath
        };

        try
        {
            var projectContent = await File.ReadAllTextAsync(projectPath);
            var doc = XDocument.Parse(projectContent);

            // Determine project format
            if (IsOldFormatProject(doc))
            {
                AnalyzeOldFormatProject(doc, projectInfo);
            }
            else
            {
                AnalyzeNewFormatProject(doc, projectInfo);
            }

            // Check for Entity Framework 6
            projectInfo.UsesEntityFramework6 = await CheckForEF6Async(projectInfo);
            
            // Find config files
            projectInfo.ConfigFiles = (await FindConfigFilesAsync(Path.GetDirectoryName(projectPath) ?? string.Empty));
        }
        catch (Exception)
        {
            // If we can't parse the project file, treat it as legacy
            projectInfo.FrameworkVersion = "Unknown";
        }

        return projectInfo;
    }

    private bool IsOldFormatProject(XDocument doc)
    {
        // Old format projects have ToolsVersion attribute and use different structure
        return doc.Root?.Attribute("ToolsVersion") != null ||
               doc.Root?.Elements().Any(e => e.Name.LocalName == "PropertyGroup" && 
                                           e.Elements().Any(p => p.Name.LocalName == "TargetFrameworkVersion")) == true;
    }

    private void AnalyzeOldFormatProject(XDocument doc, ProjectInfo projectInfo)
    {
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        
        // Get target framework
        var targetFramework = doc.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value;
        if (targetFramework != null)
        {
            projectInfo.FrameworkVersion = targetFramework;
        }
        
        // Get output type to determine project type
        var outputType = doc.Descendants(ns + "OutputType").FirstOrDefault()?.Value;
        projectInfo.Type = outputType?.ToLower() switch
        {
            "exe" => ProjectType.ConsoleApp,
            "winexe" => ProjectType.WinForms, // Could also be WPF
            "library" => ProjectType.ClassLibrary,
            _ => ProjectType.Unknown
        };
        
        // Check for packages.config usage
        projectInfo.UsesPackagesConfig = doc.Descendants(ns + "PackageReference").Any() == false &&
                                       File.Exists(Path.Combine(Path.GetDirectoryName(projectInfo.Path) ?? string.Empty, "packages.config"));

        // Get references
        projectInfo.References = doc.Descendants(ns + "Reference")
            .Select(r => r.Attribute("Include")?.Value ?? string.Empty)
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();
    }

    private void AnalyzeNewFormatProject(XDocument doc, ProjectInfo projectInfo)
    {
        // SDK-style project
        var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value ??
                            doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value?.Split(';').FirstOrDefault();
        
        if (targetFramework != null)
        {
            projectInfo.FrameworkVersion = targetFramework;
        }

        // Get output type
        var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
        projectInfo.Type = outputType?.ToLower() switch
        {
            "exe" => ProjectType.ConsoleApp,
            "winexe" => doc.Descendants("UseWindowsForms").Any() ? ProjectType.WinForms : 
                       doc.Descendants("UseWPF").Any() ? ProjectType.WPF : ProjectType.Unknown,
            _ => ProjectType.ClassLibrary
        };

        // Get package references
        projectInfo.NuGetPackages = doc.Descendants("PackageReference")
            .Select(p => p.Attribute("Include")?.Value ?? string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }

    private async Task<bool> CheckForEF6Async(ProjectInfo projectInfo)
    {
        // Check for EF6 in package references or references
        if (projectInfo.NuGetPackages.Any(p => p.StartsWith("EntityFramework") && !p.Contains("Core")))
            return true;
            
        if (projectInfo.References.Any(r => r.StartsWith("EntityFramework")))
            return true;

        // Check packages.config file
        var projectDir = Path.GetDirectoryName(projectInfo.Path);
        if (projectDir != null)
        {
            var packagesConfigPath = Path.Combine(projectDir, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                try
                {
                    var packagesContent = await File.ReadAllTextAsync(packagesConfigPath);
                    return packagesContent.Contains("EntityFramework") && !packagesContent.Contains("EntityFrameworkCore");
                }
                catch
                {
                    // Ignore file read errors
                }
            }
        }

        return false;
    }

    private Task<List<string>> FindConfigFilesAsync(string projectDir)
    {
        var configFiles = new List<string>();
        
        if (Directory.Exists(projectDir))
        {
            var patterns = new[] { "app.config", "web.config", "*.config" };
            
            foreach (var pattern in patterns)
            {
                try
                {
                    var files = Directory.GetFiles(projectDir, pattern, SearchOption.TopDirectoryOnly);
                    configFiles.AddRange(files.Select(Path.GetFileName).Where(f => f != null).Cast<string>());
                }
                catch
                {
                    // Ignore directory access errors
                }
            }
        }
        
        return Task.FromResult(configFiles.Distinct().ToList());
    }

    private bool IsLegacyProject(ProjectInfo projectInfo)
    {
        // Consider it legacy if:
        // 1. Uses .NET Framework
        // 2. Uses packages.config
        // 3. Uses Entity Framework 6
        
        return projectInfo.FrameworkVersion.StartsWith("v4.") ||
               projectInfo.FrameworkVersion.StartsWith("net4") ||
               projectInfo.UsesPackagesConfig ||
               projectInfo.UsesEntityFramework6;
    }
}
