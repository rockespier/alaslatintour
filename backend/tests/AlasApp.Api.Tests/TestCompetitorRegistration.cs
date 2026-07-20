using System.Net.Http.Headers;

namespace AlasApp.Api.Tests;

internal static class TestCompetitorRegistration
{
    public static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string email,
        string password,
        string nombre = "Test",
        string apellido = "Competitor")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(email), "Email" },
            { new StringContent(password), "Password" },
            { new StringContent(nombre), "Nombre" },
            { new StringContent(apellido), "Apellido" },
            { new StringContent("competidor"), "Tipo" },
            { new StringContent("Perú"), "Pais" },
            { new StringContent("Español"), "IdiomaPreferido" },
            { new StringContent("false"), "Newsletter" },
            { new StringContent("true"), "Terminos" },
            { new StringContent("true"), "Reglamento" },
            { new StringContent("1998-04-22T00:00:00Z"), "FechaNacimiento" },
            { new StringContent("Femenino"), "Genero" },
            { new StringContent("+51 999 111 222"), "Telefono" },
            { new StringContent("ALAS Club"), "Club" },
            { new StringContent("Regular"), "Postura" },
            { new StringContent("M"), "TallaCamiseta" },
            { new StringContent("FENTA"), "Federacion" },
            { new StringContent("Marca Test"), "Patrocinadores" }
        };

        var document = new ByteArrayContent("test-identity-document"u8.ToArray());
        document.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(document, "IdentityDocument", "identity.pdf");

        return client.PostAsync("/v1/auth/register", content);
    }
}
