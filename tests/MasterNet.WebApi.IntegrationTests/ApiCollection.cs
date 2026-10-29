namespace MasterNet.WebApi.IntegrationTests;

// Un único contenedor SQL Server (y por tanto una única migración + seed) para
// todas las clases de test marcadas con [Collection(Name)] de esta colección.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<MasterNetApiFactory>
{
    public const string Name = "MasterNet API";
}
