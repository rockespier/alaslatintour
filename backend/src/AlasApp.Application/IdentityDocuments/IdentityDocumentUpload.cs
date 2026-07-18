namespace AlasApp.Application.IdentityDocuments;

public sealed record IdentityDocumentUpload(
    string FileName,
    string ContentType,
    Stream Content,
    long Length);
