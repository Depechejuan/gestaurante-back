using Xunit;

namespace Gestaurante.ApiTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "gestaurante-api";
}
