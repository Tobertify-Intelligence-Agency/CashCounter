using System.Globalization;
using System.Text.Json;
using CashCount.Shared.Services.Sync;
using Microsoft.JSInterop;

namespace CashCount.Web.Services.Sync;

/// <summary>
/// Firestore access in the browser, through <c>wwwroot/js/firebase-sync.js</c>.
///
/// Deliberately free of try/catch: a JavaScript failure arrives here as a
/// <see cref="JSException"/> and is meant to travel on to the coordinator, which
/// turns it into a visible error. Swallowing it is what made the first attempt at
/// sync in this app impossible to debug.
/// </summary>
public class WebCloudSyncStore : ICloudSyncStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime;

    public WebCloudSyncStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsAvailable => true;

    public async Task<IReadOnlyList<SyncDocument>> GetAllAsync(string userId, string collection)
    {
        var json = await _jsRuntime.InvokeAsync<string?>("firebaseSync.getAll", userId, collection);

        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<SyncDocument>();

        var dtos = JsonSerializer.Deserialize<List<SyncDocumentDto>>(json, JsonOptions);
        if (dtos is null)
            return Array.Empty<SyncDocument>();

        return dtos.Select(Map).ToList();
    }

    public async Task<SyncDocument?> GetAsync(string userId, string collection, string id)
    {
        var json = await _jsRuntime.InvokeAsync<string?>("firebaseSync.get", userId, collection, id);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        var dto = JsonSerializer.Deserialize<SyncDocumentDto>(json, JsonOptions);
        return dto is null ? null : Map(dto);
    }

    public async Task UpsertAsync(string userId, string collection, IReadOnlyList<SyncDocument> documents)
    {
        if (documents.Count == 0)
            return;

        var payload = documents.Select(d => new SyncDocumentDto
        {
            Id = d.Id,
            UpdatedAt = d.UpdatedAt.ToString("O", CultureInfo.InvariantCulture),
            IsDeleted = d.IsDeleted,
            Payload = d.Payload
        }).ToList();

        var json = JsonSerializer.Serialize(payload);
        await _jsRuntime.InvokeVoidAsync("firebaseSync.upsert", userId, collection, json);
    }

    public async Task HardDeleteAsync(string userId, string collection, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
            return;

        var json = JsonSerializer.Serialize(ids);
        await _jsRuntime.InvokeVoidAsync("firebaseSync.hardDelete", userId, collection, json);
    }

    private static SyncDocument Map(SyncDocumentDto dto) => new()
    {
        Id = dto.Id ?? string.Empty,
        UpdatedAt = ParseTimestamp(dto.UpdatedAt),
        IsDeleted = dto.IsDeleted,
        Payload = dto.Payload ?? string.Empty
    };

    /// <summary>
    /// Timestamps travel as text, so a missing or malformed value degrades to
    /// <c>default</c> — "unknown, therefore older than anything" — instead of
    /// throwing and aborting the whole run.
    /// </summary>
    private static DateTime ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? SyncNormalization.AsUtc(parsed)
            : default;
    }

    private sealed class SyncDocumentDto
    {
        public string? Id { get; set; }
        public string? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? Payload { get; set; }
    }
}
