using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class EmployeeEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task EmployeeLoginAndProfileReturnAuthenticatedUser()
    {
        var token = await Fixture.LoginAdminAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/user/me");
        Fixture.SetBearerToken(request, token);

        var response = await Fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await Fixture.ReadEnvelopeAsync<JsonElement>(response);
        envelope.Data.GetProperty("email").GetString().Should().Be("admin@gestaurante.com");
        envelope.Data.GetProperty("tipo").GetString().Should().Be("Administrador");
    }

    [Fact]
    public async Task EmployeeListRequiresAuthentication()
    {
        var response = await Fixture.Client.PostAsync("/admin/getusers", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCanListAndUpdateEmployeeProfileWithMultipartPayload()
    {
        var adminToken = await Fixture.LoginAdminAsync();
        using var listRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/getusers");
        Fixture.SetBearerToken(listRequest, adminToken);

        var listResponse = await Fixture.Client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        var listEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(listResponse);
        var employee = listEnvelope.Data.EnumerateArray()
            .First(item => item.GetProperty("email").GetString() == "lucas.romero@gestaurante.com");
        var employeeId = employee.GetProperty("id").GetGuid();

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/admin/user/{employeeId}");
        Fixture.SetBearerToken(updateRequest, adminToken);

        var multipart = new MultipartFormDataContent
        {
            { new StringContent("Lucas Editado"), "Nombre" },
            { new StringContent("Romero"), "Apellido1" },
            { new StringContent("Pruebas"), "Apellido2" },
            { new StringContent("lucas.editado@gestaurante.com"), "Email" },
            { new StringContent("87654321-Z"), "DNI" },
            { new StringContent("28-12345678-5"), "NUSS" },
            { new StringContent("3"), "Tipo" },
            { new StringContent("false"), "Activo" }
        };
        updateRequest.Content = multipart;

        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("nombre").GetString().Should().Be("Lucas Editado");
        updateEnvelope.Data.GetProperty("apellido2").GetString().Should().Be("Pruebas");
        updateEnvelope.Data.GetProperty("email").GetString().Should().Be("lucas.editado@gestaurante.com");
        updateEnvelope.Data.GetProperty("dni").GetString().Should().Be("87654321-Z");
        updateEnvelope.Data.GetProperty("tipo").GetString().Should().Be("Repartidor");
        updateEnvelope.Data.GetProperty("activo").GetBoolean().Should().BeFalse();
    }
}
