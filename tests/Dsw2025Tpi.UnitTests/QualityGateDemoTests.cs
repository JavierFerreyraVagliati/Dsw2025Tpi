namespace Dsw2025Tpi.UnitTests;

/// <summary>
/// DEMOSTRACIÓN DEL QUALITY GATE - ESTA RAMA NO SE FUSIONA.
/// La prueba falla a propósito para comprobar que, si una prueba no pasa,
/// el pipeline se detiene en "Ejecutar pruebas (Quality Gate)" y no se
/// construye la imagen Docker ni se despliega en Azure.
/// </summary>
public class QualityGateDemoTests
{
    [Fact]
    public void QualityGate_FailingTest_BlocksBuildAndDeploy()
    {
        Assert.True(false, "Prueba que falla a propósito para demostrar que el pipeline bloquea el deploy.");
    }
}
