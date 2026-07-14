namespace AlasApp.AlasApi.Api.Controllers;

public partial class EventCategoryResponse
{
    [Newtonsoft.Json.JsonProperty("gender", Required = Newtonsoft.Json.Required.DisallowNull)]
    public CategoryGender Gender { get; set; }
}
