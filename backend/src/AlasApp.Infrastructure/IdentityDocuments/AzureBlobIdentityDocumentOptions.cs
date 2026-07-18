namespace AlasApp.Infrastructure.IdentityDocuments;

public sealed class AzureBlobIdentityDocumentOptions
{
    public const string SectionName = "IdentityDocuments";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "competitor-identity-documents";
}
