namespace AlasApp.Application.Uploads.Models;

public sealed record UploadedMediaDto(
    string MediaId,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);
