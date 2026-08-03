namespace AlasApp.Application.IdentityDocuments;

public sealed record IdentityDocumentDownload(
    Stream Content,
    string ContentType,
    string FileName);
