using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class CatalogEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task PublicCatalogListsAvailableDishesAndReturnsEnvelopeForMissingDish()
    {
        var catalogResponse = await Fixture.Client.GetAsync("/public/catalogo");

        catalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalogEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(catalogResponse);
        catalogEnvelope.Data.GetArrayLength().Should().BeGreaterThan(1);

        var missingDishResponse = await Fixture.Client.GetAsync($"/public/catalogo/{Guid.NewGuid()}");

        missingDishResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var missingDishEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(missingDishResponse);
        missingDishEnvelope.Error.Should().Be("Plato no encontrado.");
    }

    [Fact]
    public async Task AdminCanCreateUpdateAndDeleteCategoria()
    {
        var token = await Fixture.LoginAdminAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Categoria", token);
        createRequest.Content = JsonContent.Create(new { descripcion = "Postres test" });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var categoriaId = createEnvelope.Data.GetProperty("idCategoria").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Categoria/{categoriaId}", token);
        updateRequest.Content = JsonContent.Create(new { descripcion = "Postres editados" });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("descripcion").GetString().Should().Be("Postres editados");

        var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Categoria/{categoriaId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AdminCanCreateUpdateAndDeleteIngrediente()
    {
        var token = await Fixture.LoginAdminAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Ingrediente", token);
        createRequest.Content = JsonContent.Create(new
        {
            nombre = "Anchoa test",
            alergenico = true,
            disponible = true,
            imagen = string.Empty
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var ingredienteId = createEnvelope.Data.GetProperty("idIngrediente").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Ingrediente/{ingredienteId}", token);
        updateRequest.Content = JsonContent.Create(new
        {
            nombre = "Anchoa editada",
            alergenico = true,
            disponible = false,
            imagen = string.Empty
        });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("nombre").GetString().Should().Be("Anchoa editada");
        updateEnvelope.Data.GetProperty("disponible").GetBoolean().Should().BeFalse();

        var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Ingrediente/{ingredienteId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AdminCanCreateUpdateToggleAndDeletePlato()
    {
        var token = await Fixture.LoginAdminAsync();
        var categoriaId = Fixture.State.CategoriaId;
        var tomateId = Fixture.State.IngredienteTomateId;

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Plato", token);
        createRequest.Content = JsonContent.Create(new
        {
            nombre = "Croquetas test",
            descripcion = "Croquetas cremosas de jamon.",
            imagen = string.Empty,
            disponible = true,
            precio = 7.80m,
            idCategoria = categoriaId,
            categoriaDescripcion = "Entrantes",
            ingredientes = new[]
            {
                new
                {
                    idIngrediente = tomateId,
                    nombre = "Tomate"
                }
            }
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var platoId = createEnvelope.Data.GetProperty("idPlato").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Plato/{platoId}", token);
        updateRequest.Content = JsonContent.Create(new
        {
            idPlato = platoId,
            nombre = "Croquetas test editadas",
            descripcion = "Croquetas actualizadas.",
            imagen = string.Empty,
            disponible = true,
            precio = 8.40m,
            idCategoria = categoriaId,
            categoriaDescripcion = "Entrantes",
            ingredientes = new[]
            {
                new
                {
                    idIngrediente = tomateId,
                    nombre = "Tomate"
                }
            }
        });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("nombre").GetString().Should().Be("Croquetas test editadas");

        var toggleRequest = Fixture.CreateRequest(HttpMethod.Patch, $"/Plato/{platoId}/disponibilidad", token);
        toggleRequest.Content = JsonContent.Create(new { disponible = false });
        var toggleResponse = await Fixture.Client.SendAsync(toggleRequest);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggleEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(toggleResponse);
        toggleEnvelope.Data.GetProperty("disponible").GetBoolean().Should().BeFalse();

        var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Plato/{platoId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }
}
