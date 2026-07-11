using AlasApp.Application.Uploads.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlasApp.Infrastructure.WordPress;

public sealed class WordPressMediaService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UploadedMediaDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = fileName,
            FileNameStar = fileName
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = streamContent
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<WordPressMediaUploadResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("WordPress no devolvió metadata del archivo cargado.");

        return new UploadedMediaDto(
            payload.Id.ToString(),
            payload.SourceUrl ?? string.Empty,
            fileName,
            payload.MimeType ?? contentType,
            content.CanSeek ? content.Length : 0);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"WordPress media respondió {(int)response.StatusCode}: {detail}");
    }
}
