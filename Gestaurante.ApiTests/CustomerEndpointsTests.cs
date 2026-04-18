using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class CustomerEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task StaffCanListClientsAndAdminCanUpdateAndToggleThem()
    {
        var staffToken = await Fixture.LoginCamareroAsync();
        var adminToken = await Fixture.LoginAdminAsync();

        using var listRequest = Fixture.CreateRequest(HttpMethod.Get, "/Cliente", staffToken);
        var listResponse = await Fixture.Client.SendAsync(listRequest);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(listResponse);
        var cliente = listEnvelope.Data.EnumerateArray()
            .First(item => item.GetProperty("email").GetString() == "ana.morales@cliente.gestaurante.com");
        var clienteId = cliente.GetProperty("idUsuarioCliente").GetGuid();

        var updateRequest = Fixture.CreateRequest(HttpMethod.Put, $"/Cliente/{clienteId}", adminToken);
        updateRequest.Content = JsonContent.Create(new
        {
            email = "ana.morales@cliente.gestaurante.com",
            fiscalName = "Ana Morales Tests",
            firstName = "Ana",
            lastName = "Morales Vega",
            phone = "611111111",
            dni = "11111111H",
            cif = string.Empty,
            billingStreet = "Calle Test 99",
            billingCity = "Madrid",
            billingProvince = "Madrid",
            billingPostalCode = "28002",
            activo = true,
            emailVerificado = true
        });
        var updateResponse = await Fixture.Client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(updateResponse);
        updateEnvelope.Data.GetProperty("billingStreet").GetString().Should().Be("Calle Test 99");

        var toggleRequest = Fixture.CreateRequest(HttpMethod.Patch, $"/Cliente/{clienteId}/estado", adminToken);
        toggleRequest.Content = JsonContent.Create(new { activo = false });
        var toggleResponse = await Fixture.Client.SendAsync(toggleRequest);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggleEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(toggleResponse);
        toggleEnvelope.Data.GetProperty("activo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PublicAccountSupportsRegisterResendVerifyLoginAndProfileRead()
    {
        var email = $"cliente.test.{Guid.NewGuid():N}@gestaurante.local";

        var registerResponse = await Fixture.Client.PostAsJsonAsync("/public/account/register", new
        {
            email,
            password = "Client3."
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Fixture.EmailService.Messages.Should().ContainSingle(message => message.ToEmail == email);

        var resendResponse = await Fixture.Client.PostAsJsonAsync("/public/account/resend-code", new
        {
            email
        });

        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.EmailService.Messages.Count(message => message.ToEmail == email).Should().Be(2);
        var verificationCode = ApiTestFixture.ExtractVerificationCode(Fixture.EmailService.Messages.Last(message => message.ToEmail == email));

        var verifyResponse = await Fixture.Client.PostAsJsonAsync("/public/account/verify-email", new
        {
            email,
            code = verificationCode
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await Fixture.Client.PostAsJsonAsync("/public/account/login", new
        {
            email,
            password = "Client3."
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var loginEnvelope = await Fixture.ReadEnvelopeAsync<ApiTestFixture.CustomerTokenEnvelope>(loginResponse);

        using var meRequest = Fixture.CreateRequest(HttpMethod.Get, "/public/account/me", loginEnvelope.Data!.Token);
        var meResponse = await Fixture.Client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(meResponse);
        meEnvelope.Data.GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task CustomerCanUpdateProfileAndManageAddressesAndPaymentMethods()
    {
        var token = await Fixture.LoginCustomerAsync();

        var profileRequest = Fixture.CreateRequest(HttpMethod.Put, "/public/account/profile", token);
        profileRequest.Content = JsonContent.Create(new
        {
            firstName = "Ana",
            lastName = "Morales Vega",
            phone = "622222222",
            fiscalName = "Ana Morales Actualizada",
            dni = "11111111H",
            cif = string.Empty,
            billingStreet = "Calle Renovada 7",
            billingCity = "Madrid",
            billingProvince = "Madrid",
            billingPostalCode = "28003"
        });
        var profileResponse = await Fixture.Client.SendAsync(profileRequest);

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(profileResponse);
        profileEnvelope.Data.GetProperty("billingStreet").GetString().Should().Be("Calle Renovada 7");

        var createAddressRequest = Fixture.CreateRequest(HttpMethod.Post, "/public/account/addresses", token);
        createAddressRequest.Content = JsonContent.Create(new
        {
            alias = "Oficina",
            street = "Gran Via 10",
            city = "Madrid",
            province = "Madrid",
            postalCode = "28013",
            notes = "Porteria",
            isDefault = false
        });
        var createAddressResponse = await Fixture.Client.SendAsync(createAddressRequest);

        createAddressResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var addressEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createAddressResponse);
        var addressId = addressEnvelope.Data.GetProperty("idClienteDireccion").GetGuid();

        var updateAddressRequest = Fixture.CreateRequest(HttpMethod.Put, $"/public/account/addresses/{addressId}", token);
        updateAddressRequest.Content = JsonContent.Create(new
        {
            alias = "Oficina central",
            street = "Gran Via 11",
            city = "Madrid",
            province = "Madrid",
            postalCode = "28013",
            notes = string.Empty,
            isDefault = true
        });
        var updateAddressResponse = await Fixture.Client.SendAsync(updateAddressRequest);

        updateAddressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createPaymentRequest = Fixture.CreateRequest(HttpMethod.Post, "/public/account/payment-methods", token);
        createPaymentRequest.Content = JsonContent.Create(new
        {
            cardNumber = "4111111111111111",
            holderName = "Ana Morales",
            expMonth = 10,
            expYear = 2031,
            isDefault = false
        });
        var createPaymentResponse = await Fixture.Client.SendAsync(createPaymentRequest);

        createPaymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var paymentEnvelope = await Fixture.ReadEnvelopeAsync<JsonElement>(createPaymentResponse);
        var paymentId = paymentEnvelope.Data.GetProperty("idClienteMetodoPago").GetGuid();

        using var deletePaymentRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/public/account/payment-methods/{paymentId}", token);
        var deletePaymentResponse = await Fixture.Client.SendAsync(deletePaymentRequest);
        deletePaymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var deleteAddressRequest = Fixture.CreateRequest(HttpMethod.Delete, $"/public/account/addresses/{addressId}", token);
        var deleteAddressResponse = await Fixture.Client.SendAsync(deleteAddressRequest);
        deleteAddressResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
