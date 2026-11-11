using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.UnitTests;

/// <summary>
/// Prueba de verificación del proyecto de tests unitarios.
/// No prueba lógica de negocio: confirma que el proyecto se descubre,
/// se ejecuta con "dotnet test" y referencia correctamente las capas
/// Application y Domain que se van a testear.
/// </summary>
public class SetupVerificationTests
{
    [Fact]
    public void TestProject_IsDiscoveredAndRuns()
    {
        Assert.True(true);
    }

    [Fact]
    public void TestProject_ReferencesApplicationAndDomainLayers()
    {
        // Si las referencias de proyecto no estuvieran bien configuradas,
        // este test ni siquiera compilaría.
        Assert.Equal("Dsw2025Tpi.Application", typeof(ProductsManagmentService).Assembly.GetName().Name);
        Assert.Equal("Dsw2025Tpi.Domain", typeof(IRepository).Assembly.GetName().Name);
    }
}
