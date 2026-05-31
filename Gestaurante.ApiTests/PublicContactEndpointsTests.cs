using System.Net;
using System.Net.Http.Json;
using Gestaurante.ApiTests.Infrastructure;

namespace Gestaurante.ApiTests;

[Collection(ApiCollection.Name)]
public sealed class PublicContactEndpointsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task PublicContactSendsMessageToRestaurantInbox()
    {
        var response = await Fixture.Client.PostAsJsonAsync("/public/contact", new
        {
            name = "Laura Contacto",
            email = "laura.contacto@example.com",
            phone = "600123123",
            subject = "Reserva privada",
            message = "Queria consultar disponibilidad para una reserva de grupo."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        Fixture.EmailService.Messages.Should().ContainSingle(message =>
            message.ToEmail == "contact@gestaurante.local"
            && message.ReplyToEmail == "laura.contacto@example.com"
            && message.Subject.Contains("Reserva privada", StringComparison.Ordinal)
            && message.Body.Contains("Laura Contacto", StringComparison.Ordinal)
            && message.Body.Contains("600123123", StringComparison.Ordinal));
    }
}
