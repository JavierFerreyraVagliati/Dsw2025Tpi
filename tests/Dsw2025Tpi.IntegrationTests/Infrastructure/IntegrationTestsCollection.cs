namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Todas las clases marcadas con [Collection(IntegrationTestsCollection.Name)]
/// comparten UNA sola instancia de CustomWebApplicationFactory (y por lo tanto la misma
/// API en memoria y la misma base SQLite). xUnit ejecuta las pruebas de una misma
/// colección de a una, así no chocan entre sí al leer o escribir la base.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}
