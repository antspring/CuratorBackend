using System.Reflection;

namespace UnitTests.Architecture;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotDependOnOtherLayers()
    {
        var references = GetReferencedAssemblyNames("Domain");

        Assert.DoesNotContain("Application", references);
        Assert.DoesNotContain("Infrastructure", references);
        Assert.DoesNotContain("API", references);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var references = GetReferencedAssemblyNames("Application");

        Assert.DoesNotContain("Infrastructure", references);
        Assert.DoesNotContain("API", references);
    }

    private static string?[] GetReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();
}
