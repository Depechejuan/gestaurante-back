using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class OperationsEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task AdminCanCreateUpdateAndDeleteMesa()
    {
        var token = await Fixture.LoginAdminAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Mesa", token);
        createRequest.Content = JsonContent.Create(new
        {
            capacidad = 6,
            estado = true,
            ubicacion = "Terraza T9"
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var mesaId = createEnvelope.Data.GetProperty("idMesa").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Mesa/{mesaId}", token);
        updateRequest.Content = JsonContent.Create(new
        {
            capacidad = 8,
            ubicacion = "Terraza T10"
        });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("capacidad").GetInt32().Should().Be(8);
        updateEnvelope.Data.GetProperty("ubicacion").GetString().Should().Be("Terraza T10");

        using var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Mesa/{mesaId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task StaffCanCreateManageAndDeletePedidoWithDetailLifecycle()
    {
        var token = await Fixture.LoginAdminAsync();

        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/Pedido", token);
        createRequest.Content = JsonContent.Create(new
        {
            idMesa = Fixture.State.PublicMesaId,
            estado = 0,
            canalPedido = 0,
            tipoEntrega = 0,
            estadoPago = 0,
            notas = "Pedido de pruebas operativas.",
            detalles = new[]
            {
                new
                {
                    idPlato = Fixture.State.PlatoCapreseId,
                    cantidad = 2
                }
            }
        });
        var createResponse = await Fixture.Client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);
        var pedidoId = createEnvelope.Data.GetProperty("idPedido").GetGuid();
        var detalleId = createEnvelope.Data.GetProperty("detalles")[0].GetProperty("idDetallePedido").GetGuid();

        using var detailRequest = Fixture.CreateRequest(HttpMethod.Get, $"/Pedido/{pedidoId}/linea/{detalleId}", token);
        var detailResponse = await Fixture.Client.SendAsync(detailRequest);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatePedidoRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Pedido/{pedidoId}", token);
        updatePedidoRequest.Content = JsonContent.Create(new
        {
            estado = 1
        });
        var updatePedidoResponse = await Fixture.Client.SendAsync(updatePedidoRequest);

        updatePedidoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatePedidoEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updatePedidoResponse);
        updatePedidoEnvelope.Data.GetProperty("estado").GetString().Should().Be("CONFIRMADO");

        var updateDetalleRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Pedido/{pedidoId}/linea/{detalleId}", token);
        updateDetalleRequest.Content = JsonContent.Create(new
        {
            estado = 2
        });
        var updateDetalleResponse = await Fixture.Client.SendAsync(updateDetalleRequest);

        updateDetalleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateDetalleEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateDetalleResponse);
        updateDetalleEnvelope.Data.GetProperty("estado").GetString().Should().Be("EN_COCINA");

        var cancelDetalleRequest = Fixture.CreateRequest(HttpMethod.Post, $"/Pedido/{pedidoId}/linea/{detalleId}/cancelar", token);
        cancelDetalleRequest.Content = JsonContent.Create(new
        {
            motivo = "Cliente cambia de idea"
        });
        var cancelDetalleResponse = await Fixture.Client.SendAsync(cancelDetalleRequest);

        cancelDetalleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelDetalleEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(cancelDetalleResponse);
        cancelDetalleEnvelope.Data.GetProperty("estado").GetString().Should().Be("CANCELADA");

        using var deleteRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/Pedido/{pedidoId}", token);
        var deleteResponse = await Fixture.Client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(deleteResponse);
        deleteEnvelope.Data.GetProperty("deleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task RepartidorOnlySeesOnlineDeliveryOrders()
    {
        var token = await Fixture.LoginRepartidorAsync();

        using var listRequest = Fixture.CreateRequest(HttpMethod.Get, "/Pedido", token);
        var listResponse = await Fixture.Client.SendAsync(listRequest);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(listResponse);
        var pedidos = listEnvelope.Data.EnumerateArray().ToList();

        pedidos.Should().NotBeEmpty();
        pedidos.Should().OnlyContain(pedido =>
            pedido.GetProperty("canalPedido").GetString() == "ONLINE"
            && pedido.GetProperty("tipoEntrega").GetString() == "DOMICILIO");
        pedidos.Should().Contain(pedido => pedido.GetProperty("idPedido").GetGuid() == Fixture.State.OnlinePedidoId);
    }

    [Fact]
    public async Task OnlineDeliveryBecomesPendingDeliveryAndCanBeCompletedByRepartidor()
    {
        var camareroToken = await Fixture.LoginCamareroAsync();
        var cocineroToken = await Fixture.LoginCocineroAsync();
        var repartidorToken = await Fixture.LoginRepartidorAsync();
        var (pedidoId, detalleId) = await CreateOnlineOrderAsync(tipoEntrega: 2, pagarOnline: true);

        var initialListRequest = Fixture.CreateRequest(HttpMethod.Get, "/Pedido", repartidorToken);
        var initialListResponse = await Fixture.Client.SendAsync(initialListRequest);
        initialListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialListEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(initialListResponse);
        initialListEnvelope.Data.EnumerateArray()
            .Select(pedido => pedido.GetProperty("idPedido").GetGuid())
            .Should().NotContain(pedidoId);

        await UpdateDetalleEstadoAsync(camareroToken, pedidoId, detalleId, estado: 2);
        await UpdateDetalleEstadoAsync(cocineroToken, pedidoId, detalleId, estado: 3);
        await UpdateDetalleEstadoAsync(camareroToken, pedidoId, detalleId, estado: 4);

        var pendingDelivery = await GetPedidoAsync(camareroToken, pedidoId);
        pendingDelivery.GetProperty("estado").GetString().Should().Be("PENDIENTE_ENTREGA");

        var readyListRequest = Fixture.CreateRequest(HttpMethod.Get, "/Pedido", repartidorToken);
        var readyListResponse = await Fixture.Client.SendAsync(readyListRequest);
        readyListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readyListEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(readyListResponse);
        readyListEnvelope.Data.EnumerateArray()
            .Select(pedido => pedido.GetProperty("idPedido").GetGuid())
            .Should().Contain(pedidoId);

        var enCamino = await UpdatePedidoEstadoAsync(repartidorToken, pedidoId, estado: 6);
        enCamino.GetProperty("estado").GetString().Should().Be("EN_CAMINO");

        var entregado = await UpdatePedidoEstadoAsync(repartidorToken, pedidoId, estado: 4);
        entregado.GetProperty("estado").GetString().Should().Be("ENTREGADO");

        var finalListRequest = Fixture.CreateRequest(HttpMethod.Get, "/Pedido", repartidorToken);
        var finalListResponse = await Fixture.Client.SendAsync(finalListRequest);
        finalListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalListEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(finalListResponse);
        finalListEnvelope.Data.EnumerateArray()
            .Select(pedido => pedido.GetProperty("idPedido").GetGuid())
            .Should().NotContain(pedidoId);
    }

    [Fact]
    public async Task OnlinePickupWaitsForCustomerAndCamareroDeliveryCreatesLocalFactura()
    {
        var camareroToken = await Fixture.LoginCamareroAsync();
        var cocineroToken = await Fixture.LoginCocineroAsync();
        var (pedidoId, detalleId) = await CreateOnlineOrderAsync(tipoEntrega: 1, pagarOnline: false);

        await UpdateDetalleEstadoAsync(camareroToken, pedidoId, detalleId, estado: 2);
        await UpdateDetalleEstadoAsync(cocineroToken, pedidoId, detalleId, estado: 3);
        await UpdateDetalleEstadoAsync(camareroToken, pedidoId, detalleId, estado: 4);

        var waitingPickup = await GetPedidoAsync(camareroToken, pedidoId);
        waitingPickup.GetProperty("estado").GetString().Should().Be("EN_ESPERA");
        waitingPickup.GetProperty("estadoPago").GetString().Should().Be("PENDIENTE_LOCAL");

        var entregado = await UpdatePedidoEstadoAsync(camareroToken, pedidoId, estado: 4);
        entregado.GetProperty("estado").GetString().Should().Be("ENTREGADO");
        entregado.GetProperty("estadoPago").GetString().Should().Be("PAGADO_LOCAL");
        entregado.GetProperty("estaFacturado").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CamareroCanCloseMesaWithPendingOrdersAndGenerateFactura()
    {
        var token = await Fixture.LoginCamareroAsync();

        var createPedidoRequest = Fixture.CreateRequest(HttpMethod.Post, "/Pedido", token);
        createPedidoRequest.Content = JsonContent.Create(new
        {
            idMesa = Fixture.State.PublicMesaId,
            estado = 0,
            canalPedido = 0,
            tipoEntrega = 0,
            estadoPago = 0,
            detalles = new[]
            {
                new
                {
                    idPlato = Fixture.State.PlatoPizzaId,
                    cantidad = 1
                }
            }
        });
        var createPedidoResponse = await Fixture.Client.SendAsync(createPedidoRequest);
        createPedidoResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedidoEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createPedidoResponse);
        var pedidoId = pedidoEnvelope.Data.GetProperty("idPedido").GetGuid();

        var closeRequest = Fixture.CreateRequest(HttpMethod.Post, $"/Mesa/{Fixture.State.PublicMesaId}/cerrar", token);
        closeRequest.Content = JsonContent.Create(new
        {
            descuento = 0,
            estadoFactura = 0
        });
        var closeResponse = await Fixture.Client.SendAsync(closeRequest);

        closeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var closeEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(closeResponse);
        closeEnvelope.Data.GetProperty("idMesa").GetGuid().Should().Be(Fixture.State.PublicMesaId);
        closeEnvelope.Data.GetProperty("pedidoIds").EnumerateArray().Select(item => item.GetGuid()).Should().Contain(pedidoId);
        closeEnvelope.Data.GetProperty("lineas").GetArrayLength().Should().BeGreaterThan(0);
    }

    private async Task<(Guid PedidoId, Guid DetalleId)> CreateOnlineOrderAsync(int tipoEntrega, bool pagarOnline)
    {
        var token = await Fixture.LoginCustomerAsync();
        var createRequest = Fixture.CreateRequest(HttpMethod.Post, "/public/checkout/order", token);
        createRequest.Content = tipoEntrega == 2
            ? JsonContent.Create(new
            {
                tipoEntrega,
                pagarOnline,
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
            })
            : JsonContent.Create(new
            {
                tipoEntrega,
                pagarOnline,
                detalles = new[]
                {
                    new
                    {
                        idPlato = Fixture.State.PlatoCapreseId,
                        cantidad = 1
                    }
                }
            });

        var createResponse = await Fixture.Client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createResponse);

        return (
            createEnvelope.Data.GetProperty("idPedido").GetGuid(),
            createEnvelope.Data.GetProperty("detalles")[0].GetProperty("idDetallePedido").GetGuid());
    }

    private async Task<JsonElement> GetPedidoAsync(string token, Guid pedidoId)
    {
        using var request = Fixture.CreateRequest(HttpMethod.Get, $"/Pedido/{pedidoId}", token);
        var response = await Fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await Fixture.ReadEnvelopeAsync<JsonElement>(response);
        return envelope.Data;
    }

    private async Task UpdateDetalleEstadoAsync(string token, Guid pedidoId, Guid detalleId, int estado)
    {
        var request = Fixture.CreateRequest(HttpMethod.Put, $"/Pedido/{pedidoId}/linea/{detalleId}", token);
        request.Content = JsonContent.Create(new
        {
            estado
        });

        var response = await Fixture.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<JsonElement> UpdatePedidoEstadoAsync(string token, Guid pedidoId, int estado)
    {
        var request = Fixture.CreateRequest(HttpMethod.Put, $"/Pedido/{pedidoId}", token);
        request.Content = JsonContent.Create(new
        {
            estado
        });

        var response = await Fixture.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await Fixture.ReadEnvelopeAsync<JsonElement>(response);
        return envelope.Data;
    }
}
