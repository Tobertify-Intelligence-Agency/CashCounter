using System.Globalization;
using CashCount.Shared.Services.Sync;

#if ANDROID || IOS || MACCATALYST
using Plugin.Firebase.Firestore;
#endif

namespace CashCount.Maui.Services.Sync;

/// <summary>
/// Firestore access for Android and iOS via Plugin.Firebase.
///
/// Layout: <c>users/{uid}/{collection}/{documentId}</c> — the same shape the
/// existing profile sync already uses, so both live side by side under one
/// document per user.
///
/// The stored fields are deliberately primitive (two strings and a bool). The
/// record itself travels as JSON inside <c>Payload</c>, for two reasons: Firestore
/// cannot store an array inside an array, and a signature is exactly that
/// (strokes of points); and a JSON payload keeps the document shape identical on
/// Android, iOS and the web, so a phone and a browser really do read each other's
/// data instead of two subtly different field mappings.
///
/// Timestamps are ISO-8601 strings rather than Firestore <c>Timestamp</c> values,
/// again so that every platform produces byte-identical documents.
/// </summary>
public class MauiCloudSyncStore : ICloudSyncStore
{
#if ANDROID || IOS || MACCATALYST
    private const string UsersCollection = "users";

    public bool IsAvailable => true;

    private static ICollectionReference Collection(string userId, string collection)
        => CrossFirebaseFirestore.Current
            .GetCollection(UsersCollection)
            .GetDocument(userId)
            .GetCollection(collection);

    public async Task<IReadOnlyList<SyncDocument>> GetAllAsync(string userId, string collection)
    {
        var snapshot = await Collection(userId, collection).GetDocumentsAsync<SyncDocumentDto>();

        var result = new List<SyncDocument>();
        foreach (var document in snapshot.Documents)
        {
            var mapped = Map(document.Data, document.Reference.Id);
            if (mapped != null)
                result.Add(mapped);
        }

        return result;
    }

    public async Task<SyncDocument?> GetAsync(string userId, string collection, string id)
    {
        var snapshot = await Collection(userId, collection)
            .GetDocument(id)
            .GetDocumentSnapshotAsync<SyncDocumentDto>();

        return Map(snapshot.Data, id);
    }

    public async Task UpsertAsync(string userId, string collection, IReadOnlyList<SyncDocument> documents)
    {
        if (documents.Count == 0)
            return;

        var reference = Collection(userId, collection);

        foreach (var document in documents)
        {
            if (string.IsNullOrEmpty(document.Id))
                continue;

            await reference.GetDocument(document.Id).SetDataAsync(ToDto(document));
        }
    }

    public async Task HardDeleteAsync(string userId, string collection, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
            return;

        var reference = Collection(userId, collection);

        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id))
                continue;

            await reference.GetDocument(id).DeleteDocumentAsync();
        }
    }

    private static SyncDocument? Map(SyncDocumentDto? dto, string id)
    {
        if (dto == null)
            return null;

        return new SyncDocument
        {
            Id = id,
            UpdatedAt = ParseTimestamp(dto.UpdatedAt),
            IsDeleted = dto.IsDeleted,
            Payload = dto.Payload ?? string.Empty
        };
    }

    private static SyncDocumentDto ToDto(SyncDocument document) => new()
    {
        UpdatedAt = document.UpdatedAt.ToString("O", CultureInfo.InvariantCulture),
        IsDeleted = document.IsDeleted,
        Payload = document.Payload
    };

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

    /// <summary>Plain POCO — Plugin.Firebase maps it onto Firestore fields by name.</summary>
    public class SyncDocumentDto
    {
        public string? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? Payload { get; set; }
    }

#else
    // Windows and macOS have no Firestore binding in Plugin.Firebase. Rather than
    // failing on every call, sync reports itself as unavailable and the app stays
    // purely local.

    public bool IsAvailable => false;

    public Task<IReadOnlyList<SyncDocument>> GetAllAsync(string userId, string collection)
        => Task.FromResult<IReadOnlyList<SyncDocument>>(Array.Empty<SyncDocument>());

    public Task<SyncDocument?> GetAsync(string userId, string collection, string id)
        => Task.FromResult<SyncDocument?>(null);

    public Task UpsertAsync(string userId, string collection, IReadOnlyList<SyncDocument> documents)
        => Task.CompletedTask;

    public Task HardDeleteAsync(string userId, string collection, IReadOnlyList<string> ids)
        => Task.CompletedTask;
#endif
}
