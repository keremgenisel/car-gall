namespace CarGallery.Architecture.Tests;

/// <summary>docs/architecture.md § 5 — Veri erişimi, tipler ve kod (1–11).</summary>
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void ReferansVerilmez_BaskaModulunIcProjesine()
    {
        List<string> violations = [];

        foreach (string project in SolutionLayout.ProjectFiles().Where(SolutionLayout.IsModuleProject))
        {
            string ownModule = SolutionLayout.ModuleNameOf(project);

            foreach (string reference in SolutionLayout.ProjectReferences(project))
            {
                if (!SolutionLayout.IsModuleProject(reference))
                {
                    continue;
                }

                string referencedModule = SolutionLayout.ModuleNameOf(reference);
                if (referencedModule == ownModule || SolutionLayout.IsContractsProject(reference))
                {
                    continue;
                }

                violations.Add($"{SolutionLayout.Relative(project)} → {SolutionLayout.Relative(reference)}");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Tek izinli referans X.Contracts'tır (Yasak Listesi 7):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void ReferansVermez_BuildingBlocks_HicbirModule()
    {
        string? buildingBlocks = SolutionLayout.ProjectFiles()
            .FirstOrDefault(project => Path.GetFileName(project) == "BuildingBlocks.csproj");

        Assert.NotNull(buildingBlocks);

        List<string> violations =
        [
            .. SolutionLayout.ProjectReferences(buildingBlocks)
                .Where(SolutionLayout.IsModuleProject)
                .Select(SolutionLayout.Relative)
        ];

        Assert.True(
            violations.Count == 0,
            $"BuildingBlocks bilinçli olarak fakirdir (Yasak Listesi 11):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void ReferansVermez_SozlesmeProjesi_BaskaModulunIcProjesine()
    {
        List<string> violations = [];

        foreach (string project in SolutionLayout.ProjectFiles().Where(SolutionLayout.IsContractsProject))
        {
            violations.AddRange(
                SolutionLayout.ProjectReferences(project)
                    .Where(reference => SolutionLayout.IsModuleProject(reference) && !SolutionLayout.IsContractsProject(reference))
                    .Select(reference => $"{SolutionLayout.Relative(project)} → {SolutionLayout.Relative(reference)}"));
        }

        Assert.True(
            violations.Count == 0,
            $"Sözleşme projesi iç koda bağlanamaz:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
