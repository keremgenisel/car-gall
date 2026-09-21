using System.Xml.Linq;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// Mimari testler kaynak ağacını doğrudan okur. Derlenmiş assembly'lere referans vermek,
/// test projesini her yeni modüle referans vermeye zorlardı; kaynak tarama ise modül
/// eklendiği anda kendiliğinden kapsar — kuralın unutulacak bir adımı kalmaz.
/// </summary>
internal static class SolutionLayout
{
    internal static string Root { get; } = FindRoot();

    internal static string SourceRoot => Path.Combine(Root, "src");

    internal static IReadOnlyList<string> ProjectFiles() =>
        Directory.Exists(SourceRoot)
            ? [.. Directory.EnumerateFiles(SourceRoot, "*.csproj", SearchOption.AllDirectories).Order()]
            : [];

    internal static IReadOnlyList<string> SourceFiles() =>
        Directory.Exists(SourceRoot)
            ? [.. Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsGenerated(path))
                .Order()]
            : [];

    internal static IReadOnlyList<string> ProjectReferences(string projectFile)
    {
        XDocument document = XDocument.Load(projectFile);
        string projectDirectory = Path.GetDirectoryName(projectFile)!;

        return
        [
            .. document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!.Replace('\\', '/'))))
        ];
    }

    internal static string Relative(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath).Replace('\\', '/');

    /// <summary>Modül projesi mi (<c>src/Modules/X/X</c>), yoksa sözleşme projesi mi (<c>X.Contracts</c>)?</summary>
    internal static bool IsContractsProject(string projectFile) =>
        Path.GetFileNameWithoutExtension(projectFile).EndsWith(".Contracts", StringComparison.Ordinal);

    internal static bool IsModuleProject(string projectFile) =>
        Relative(projectFile).StartsWith("src/Modules/", StringComparison.Ordinal);

    internal static string ModuleNameOf(string projectFile)
    {
        string[] segments = Relative(projectFile).Split('/');
        return segments.Length >= 3 ? segments[2] : string.Empty;
    }

    private static bool IsGenerated(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            // SDK 10 çözümü .slnx olarak üretir; .sln hâlâ geçerli bir biçimdir.
            if (Directory.EnumerateFiles(directory.FullName, "CarGallery.sln*").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("CarGallery.sln bulunamadı; mimari testler kaynak ağacını konumlandıramıyor.");
    }
}
