using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemOne.Ollama;

public sealed class DecisionClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<SystemOneResponse> AskAsync<TState>(
        SystemOneRequest<TState> request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "v1/systemone",
            request,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama returned {(int)response.StatusCode} ({response.ReasonPhrase}): {error}",
                inner: null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<SystemOneResponse>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new JsonException("Ollama returned an empty System One response.");
    }
}