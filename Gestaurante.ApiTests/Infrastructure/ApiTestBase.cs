using Xunit;

namespace Gestaurante.ApiTests.Infrastructure;

public abstract class ApiTestBase(ApiTestFixture fixture) : IAsyncLifetime
{
    protected ApiTestFixture Fixture { get; } = fixture;

    public virtual Task InitializeAsync()
    {
        return Fixture.ResetAsync();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
