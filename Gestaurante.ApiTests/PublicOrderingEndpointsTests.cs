using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class PublicOrderingEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task PublicMesaSessionSupportsOpenCreateOrderAndListOrders()
    {
        var openResponse = await Fixture.Client.PostAsJsonAsync($"/public/mesa/{Fixture.State.PublicMesaId}/session", new
        {
            sessionToken = (string?)null
        });

        openResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var openEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(openResponse);
        var sessionToken = openEnvelope.Data.GetProperty("sessionToken").GetString();
        sessionToken.Should().NotBeNullOrWhiteSpace();

        using var createPedidoRequest = new HttpRequestMessage(HttpMethod.Post, $"/public/mesa/{Fixture.State.PublicMesaId}/pedido");
        createPedidoRequest.Headers.Add("X-Mesa-Session", sessionToken);
        createPedidoRequest.Content = JsonContent.Create(new
        {
            detalles = new[]
            {
                new
                {
                    idPlato = Fixture.State.PlatoCapreseId,
                    cantidad = 1
                }
            }
        });
        var createPedidoResponse = await Fixture.Client.SendAsync(createPedidoRequest);

        createPedidoResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createPedidoEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createPedidoResponse);
        var pedidoId = createPedidoEnvelope.Data.GetProperty("idPedido").GetGuid();

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/public/mesa/{Fixture.State.PublicMesaId}/pedidos");
        listRequest.Headers.Add("X-Mesa-Session", sessionToken);
        var listResponse = await Fixture.Client.SendAsync(listRequest);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(listResponse);
        listEnvelope.Data.EnumerateArray().Select(item => item.GetProperty("idPedido").GetGuid()).Should().Contain(pedidoId);
    }

    [Fact]
    public async Task AuthenticatedCustomerCanCreateOnlineOrderAndReadOwnHistory()
    {
        var token = await Fixture.LoginCustomerAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/public/checkout/order", token);
        createRequest.Content = JsonContent.Create(new
        {
            tipoEntrega = 2,
            pagarOnline = true,
            idClienteDireccion = Fixture.State.VerifiedCustomerAddressId,
            detalles = new[]
            {
                new
                {
                    idPlato = Fixture.State.PlatoPizzaId,
                    cantidad = 1
                }
            },
            paymentMethod = new
            {
                idClienteMetodoPago = Fixture.State.VerifiedCustomerPaymentMethodId
            }
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var pedidoId = createEnvelope.Data.GetProperty("idPedido").GetGuid();
        var facturaId = createEnvelope.Data.GetProperty("idFactura").GetGuid();
        createEnvelope.Data.GetProperty("canalPedido").GetString().Should().Be("ONLINE");
        createEnvelope.Data.GetProperty("tipoEntrega").GetString().Should().Be("DOMICILIO");
        Fixture.EmailService.Messages.Should().ContainSingle();
        var facturaEmail = Fixture.EmailService.Messages.Single();
        facturaEmail.Subject.Should().Contain(facturaId.ToString());
        facturaEmail.IsHtml.Should().BeTrue();
        facturaEmail.Body.Should().Contain("Factura simplificada");
        facturaEmail.Body.Should().Contain("Pizza Margarita");
        facturaEmail.Body.Should().Contain("Puedes imprimir este correo");
        facturaEmail.Body.Should().Contain("contacta con nosotros desde el formulario");
        facturaEmail.Body.Should().NotContain($"Factura: {facturaId}. Total:");

        using var historyRequest = Fixture.CreateRequest(HttpMethod.Get, "/public/account/orders", token);
        var historyResponse = await Fixture.Client.SendAsync(historyRequest);

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(historyResponse);
        historyEnvelope.Data.EnumerateArray().Select(item => item.GetProperty("idPedido").GetGuid()).Should().Contain(pedidoId);

        using var detailRequest = Fixture.CreateRequest(HttpMethod.Get, $"/public/account/orders/{pedidoId}", token);
        var detailResponse = await Fixture.Client.SendAsync(detailRequest);

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(detailResponse);
        detailEnvelope.Data.GetProperty("idPedido").GetGuid().Should().Be(pedidoId);
        detailEnvelope.Data.GetProperty("estadoPago").GetString().Should().Be("PAGADO_ONLINE");
    }
}
