using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Shared.Application;
using TaskManager.Tasks.Application.Features.Tasks;

namespace TaskManager.Api.IntegrationTests.Fixtures;

/// <summary>Helpers HTTP com as mesmas opções JSON da Api (enums como string).</summary>
public static class TaskApi
{
    public const string BasePath = "/api/v1/tasks";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<HttpResponseMessage> PostTaskAsync(this HttpClient client, object body) =>
        client.PostAsJsonAsync(BasePath, body, Json);

    public static async Task<TaskResponse> CreateTaskAsync(this HttpClient client, object body)
    {
        var response = await client.PostTaskAsync(body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, object body, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body, options: Json) };
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static Task<PagedResponse<TaskResponse>> ListAsync(this HttpClient client, string query = "") =>
        client.GetFromJsonAsync<PagedResponse<TaskResponse>>($"{BasePath}{query}", Json)!;
}
