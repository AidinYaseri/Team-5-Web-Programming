using System.Xml.Linq;

namespace Animal.UnitTests.Architecture
{
    // Enforces the one-way dependency rules from the project's .csproj files, so a wrong reference fails CI
    // instead of slipping through review: Client -(HTTP only)-> Api -> Data -> Domain.
    public class LayerDependencyTests
    {
        private static readonly string SrcFolder = Path.Combine(FindRepoRoot(), "src");

        [Fact]
        public void Domain_HasNoProjectOrPackageReferences()
        {
            Assert.Empty(ProjectReferences("Animal.Domain"));
            Assert.Empty(PackageReferences("Animal.Domain"));
        }

        [Fact]
        public void Data_ReferencesOnlyDomain()
        {
            Assert.Equal(["Animal.Domain"], ProjectReferences("Animal.Data"));
        }

        [Fact]
        public void Data_DoesNotUseAspNetCore()
        {
            Assert.DoesNotContain(PackageReferences("Animal.Data"), p => p.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Microsoft.NET.Sdk", Sdk("Animal.Data"));
        }

        [Fact]
        public void Api_ReferencesDataAndDomain_ButNotTheClient()
        {
            Assert.Equal(["Animal.Data", "Animal.Domain"], ProjectReferences("Animal.API"));
        }

        // The client may only use the API's HTTP endpoints, never its assemblies or the database.
        [Fact]
        public void Client_HasNoProjectReferences()
        {
            Assert.Empty(ProjectReferences("Animal.Client"));
            Assert.DoesNotContain(PackageReferences("Animal.Client"), p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        }

        private static XDocument LoadProject(string name) =>
            XDocument.Load(Path.Combine(SrcFolder, name, name + ".csproj"));

        private static string? Sdk(string name) => LoadProject(name).Root?.Attribute("Sdk")?.Value;

        private static List<string> ProjectReferences(string name) =>
            LoadProject(name).Descendants("ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
                .Order()
                .ToList();

        private static List<string> PackageReferences(string name) =>
            LoadProject(name).Descendants("PackageReference")
                .Select(r => r.Attribute("Include")!.Value)
                .ToList();

        // Walks up from the test's bin folder until it finds the folder that holds src/ and the solution.
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Team-5-Web-Programming.slnx")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository root from " + AppContext.BaseDirectory);
        }
    }
}
