using System.Text.Json;
using HttpLib;
using HttpServerApp.Models;
using HttpServerApp.Repositories;

namespace HttpServerApp.Handlers;

public class CatsHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly CatRepository _repository;

    public CatsHandler(CatRepository repository)
    {
        _repository = repository;
    }

    public Task<HttpResponse> List(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        var json = JsonSerializer.Serialize(_repository.GetAll(), JsonOptions);
        return Task.FromResult(HttpResponse.Json(200, json));
    }

    public Task<HttpResponse> GetById(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        if (!TryGetId(parameters, out var id))
            return Task.FromResult(HttpResponse.Create(400, "Invalid id"));

        var cat = _repository.GetById(id);
        if (cat is null)
            return Task.FromResult(HttpResponse.Create(404, "Not Found"));

        return Task.FromResult(HttpResponse.Json(200, JsonSerializer.Serialize(cat, JsonOptions)));
    }

    public Task<HttpResponse> Create(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        if (!IsJsonContentType(request))
            return Task.FromResult(HttpResponse.Create(400, "Content-Type must be application/json"));

        Cat? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<Cat>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult(HttpResponse.Create(400, "Malformed JSON body"));
        }

        if (incoming is null || string.IsNullOrWhiteSpace(incoming.Name))
            return Task.FromResult(HttpResponse.Create(400, "Invalid cat payload"));

        var created = _repository.Add(new Cat
        {
            Name = incoming.Name,
            Breed = incoming.Breed ?? "",
            Age = incoming.Age
        });

        var response = HttpResponse.Json(201, JsonSerializer.Serialize(created, JsonOptions));
        response.Headers["Location"] = $"/cats/{created.Id}";
        return Task.FromResult(response);
    }

    public Task<HttpResponse> Update(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        if (!TryGetId(parameters, out var id))
            return Task.FromResult(HttpResponse.Create(400, "Invalid id"));

        if (!IsJsonContentType(request))
            return Task.FromResult(HttpResponse.Create(400, "Content-Type must be application/json"));

        Cat? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<Cat>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult(HttpResponse.Create(400, "Malformed JSON body"));
        }

        if (incoming is null || string.IsNullOrWhiteSpace(incoming.Name))
            return Task.FromResult(HttpResponse.Create(400, "Invalid cat payload"));

        var updated = _repository.Update(id, new Cat
        {
            Name = incoming.Name,
            Breed = incoming.Breed ?? "",
            Age = incoming.Age
        });

        if (updated is null)
            return Task.FromResult(HttpResponse.Create(404, "Not Found"));

        return Task.FromResult(HttpResponse.Json(200, JsonSerializer.Serialize(updated, JsonOptions)));
    }

    public Task<HttpResponse> Delete(HttpRequest request, IReadOnlyDictionary<string, string> parameters)
    {
        if (!TryGetId(parameters, out var id))
            return Task.FromResult(HttpResponse.Create(400, "Invalid id"));

        if (!_repository.Delete(id))
            return Task.FromResult(HttpResponse.Create(404, "Not Found"));

        return Task.FromResult(HttpResponse.Create(204));
    }

    private static bool TryGetId(IReadOnlyDictionary<string, string> parameters, out int id)
    {
        id = 0;
        return parameters.TryGetValue("id", out var raw) && int.TryParse(raw, out id);
    }

    private static bool IsJsonContentType(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Content-Type", out var contentType))
            return false;

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
