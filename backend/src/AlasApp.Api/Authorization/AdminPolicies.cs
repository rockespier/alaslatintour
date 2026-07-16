namespace AlasApp.Api.Authorization;

public static class AdminPolicies
{
    public const string DashboardRead = "Admin.Dashboard.Read";
    public const string UsersRead = "Admin.Users.Read";
    public const string UsersWrite = "Admin.Users.Write";
    public const string CircuitsRead = "Admin.Circuits.Read";
    public const string CircuitsWrite = "Admin.Circuits.Write";
    public const string EventsRead = "Admin.Events.Read";
    public const string EventsWrite = "Admin.Events.Write";
    public const string CategoriesRead = "Admin.Categories.Read";
    public const string CategoriesWrite = "Admin.Categories.Write";
    public const string InscriptionsRead = "Admin.Inscriptions.Read";
    public const string InscriptionsWrite = "Admin.Inscriptions.Write";
    public const string PaymentsRead = "Admin.Payments.Read";
    public const string PaymentsWrite = "Admin.Payments.Write";
    public const string TokensRead = "Admin.Tokens.Read";
    public const string TokensWrite = "Admin.Tokens.Write";
    public const string ConfigurationRead = "Admin.Configuration.Read";
    public const string ConfigurationWrite = "Admin.Configuration.Write";
}
