using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class BillingEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task FacturaEndpointsReturnListDetailAndClientLookup()
    {
        var token = await Fixture.LoginAdminAsync();

        using var listRequest = Fixture.CreateRequest(HttpMethod.Get, "/Factura", token);
        var listResponse = await Fixture.Client.SendAsync(listRequest);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(listResponse);
        listEnvelope.Data.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        using var detailRequest = Fixture.CreateRequest(HttpMethod.Get, $"/Factura/{Fixture.State.FacturaSalaId}", token);
        var detailResponse = await Fixture.Client.SendAsync(detailRequest);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(detailResponse);
        detailEnvelope.Data.GetProperty("numeroFactura").GetGuid().Should().Be(Fixture.State.FacturaSalaId);

        using var searchRequest = Fixture.CreateRequest(HttpMethod.Get, "/Factura/clientes/search?query=ana", token);
        var searchResponse = await Fixture.Client.SendAsync(searchRequest);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(searchResponse);
        searchEnvelope.Data.EnumerateArray().Any(item => item.GetProperty("idUsuarioCliente").GetGuid() == Fixture.State.VerifiedCustomerId)
            .Should().BeTrue();
    }

    [Fact]
    public async Task AdminCanCreateUpdateAssignChargeSendAndDeleteManualFactura()
    {
        var token = await Fixture.LoginAdminAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Factura", token);
        createRequest.Content = JsonContent.Create(new
        {
            precioTotal = 14.50,
            descuento = 0,
            estado = 0
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var facturaId = createEnvelope.Data.GetProperty("numeroFactura").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Factura/{facturaId}", token);
        updateRequest.Content = JsonContent.Create(new
        {
            tipoDescuento = 1,
            valorDescuento = 10,
            motivoDescuento = "Promocion pruebas"
        });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("motivoDescuento").GetString().Should().Be("Promocion pruebas");

        var assignRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Factura/{facturaId}/cliente", token);
        assignRequest.Content = JsonContent.Create(new
        {
            idUsuarioCliente = Fixture.State.VerifiedCustomerId,
            createCustomer = false,
            fiscalName = "Ana Morales Tests",
            dni = "11111111H",
            cif = string.Empty,
            billingStreet = "Calle Factura 7",
            billingCity = "Madrid",
            billingProvince = "Madrid",
            billingPostalCode = "28004",
            billingEmail = "ana.morales@cliente.gestaurante.com",
            billingPhone = "633333333",
            saveOnCustomer = false
        });
        var assignResponse = await Fixture.Client.SendAsync(assignRequest);

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(assignResponse);
        assignEnvelope.Data.GetProperty("clienteFactura").GetProperty("idUsuarioCliente").GetGuid().Should().Be(Fixture.State.VerifiedCustomerId);

        var chargeRequest = Fixture.CreateRequest(HttpMethod.Post, $"/Factura/{facturaId}/cobrar", token);
        chargeRequest.Content = JsonContent.Create(new
        {
            metodoPago = 0,
            importeEntregado = 20.00
        });
        var chargeResponse = await Fixture.Client.SendAsync(chargeRequest);

        chargeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var chargeEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(chargeResponse);
        chargeEnvelope.Data.GetProperty("estado").GetString().Should().Be("PAGADO");

        var sendEmailRequest = Fixture.CreateRequest(HttpMethod.Post, $"/Factura/{facturaId}/send-email", token);
        sendEmailRequest.Content = JsonContent.Create(new { email = string.Empty });
        var sendEmailResponse = await Fixture.Client.SendAsync(sendEmailRequest);

        sendEmailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.EmailService.Messages.Should().Contain(message => message.Subject.Contains(facturaId.ToString(), StringComparison.Ordinal));

        using var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Factura/{facturaId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }
}
