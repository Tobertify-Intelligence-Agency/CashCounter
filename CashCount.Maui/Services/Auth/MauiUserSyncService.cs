using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;

#if ANDROID || IOS
using Plugin.Firebase.Firestore;
#endif

namespace CashCount.Maui.Services.Auth;

/// <summary>
/// MAUI implementation of IUserSyncService using Plugin.Firebase.Firestore.
/// Supports Android and iOS. Windows/macOS fall back to stub implementation.
/// </summary>
public class MauiUserSyncService : IUserSyncService
{
#if ANDROID || IOS
    private const string UsersCollection = "users";
    private const string SavedCountsCollection = "savedCounts";

    private ICollectionReference UsersRef => CrossFirebaseFirestore.Current.GetCollection(UsersCollection);

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var document = await UsersRef.GetDocument(userId).GetDocumentSnapshotAsync<UserProfileDto>();
            var dto = document.Data;
            if (dto != null)
            {
                return MapToUserProfile(dto, userId);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserProfile error: {ex.Message}");
            return null;
        }
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        try
        {
            var dto = MapToDto(profile);
            await UsersRef.GetDocument(profile.UserId).SetDataAsync(dto);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveUserProfile error: {ex.Message}");
        }
    }

    public async Task UpdatePremiumStatusAsync(string userId, bool isPremium, DateTime? expiryDate)
    {
        try
        {
            var updates = new Dictionary<object, object>
            {
                { "isPremium", isPremium },
                { "premiumExpiryDate", expiryDate?.ToString("o") ?? "" }
            };
            await UsersRef.GetDocument(userId).UpdateDataAsync(updates);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdatePremiumStatus error: {ex.Message}");
        }
    }

    public async Task SyncSavedCountsAsync(string userId, List<SavedCount> counts)
    {
        try
        {
            var savedCountsRef = UsersRef.GetDocument(userId).GetCollection(SavedCountsCollection);

            // Delete existing counts first
            var existingDocs = await savedCountsRef.GetDocumentsAsync<SavedCountDto>();
            foreach (var doc in existingDocs.Documents)
            {
                await doc.Reference.DeleteDocumentAsync();
            }

            // Add new counts
            foreach (var count in counts)
            {
                var dto = MapToSavedCountDto(count);
                await savedCountsRef.GetDocument(count.Id).SetDataAsync(dto);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncSavedCounts error: {ex.Message}");
        }
    }

    public async Task<List<SavedCount>> GetSyncedCountsAsync(string userId)
    {
        try
        {
            var savedCountsRef = UsersRef.GetDocument(userId).GetCollection(SavedCountsCollection);
            var snapshot = await savedCountsRef.GetDocumentsAsync<SavedCountDto>();

            var result = new List<SavedCount>();
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Data != null)
                {
                    result.Add(MapToSavedCount(doc.Data, doc.Reference.Id));
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetSyncedCounts error: {ex.Message}");
            return new List<SavedCount>();
        }
    }

    public async Task DeleteUserDataAsync(string userId)
    {
        try
        {
            // Delete saved counts subcollection first
            var savedCountsRef = UsersRef.GetDocument(userId).GetCollection(SavedCountsCollection);
            var existingDocs = await savedCountsRef.GetDocumentsAsync<SavedCountDto>();
            foreach (var doc in existingDocs.Documents)
            {
                await doc.Reference.DeleteDocumentAsync();
            }

            // Delete user document
            await UsersRef.GetDocument(userId).DeleteDocumentAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteUserData error: {ex.Message}");
        }
    }

    #region Mapping Methods

    private static UserProfile MapToUserProfile(UserProfileDto? dto, string userId)
    {
        if (dto == null) return new UserProfile { UserId = userId };

        return new UserProfile
        {
            UserId = userId,
            Email = dto.Email ?? string.Empty,
            DisplayName = dto.DisplayName ?? string.Empty,
            PhotoUrl = dto.PhotoUrl,
            IsPremium = dto.IsPremium,
            PremiumExpiryDate = string.IsNullOrEmpty(dto.PremiumExpiryDate)
                ? null
                : DateTime.TryParse(dto.PremiumExpiryDate, out var date) ? date : null,
            CreatedAt = string.IsNullOrEmpty(dto.CreatedAt)
                ? DateTime.UtcNow
                : DateTime.TryParse(dto.CreatedAt, out var created) ? created : DateTime.UtcNow,
            LastLoginAt = string.IsNullOrEmpty(dto.LastLoginAt)
                ? DateTime.UtcNow
                : DateTime.TryParse(dto.LastLoginAt, out var login) ? login : DateTime.UtcNow,
            AuthProvider = dto.AuthProvider
        };
    }

    private static UserProfileDto MapToDto(UserProfile profile)
    {
        return new UserProfileDto
        {
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            PhotoUrl = profile.PhotoUrl,
            IsPremium = profile.IsPremium,
            PremiumExpiryDate = profile.PremiumExpiryDate?.ToString("o"),
            CreatedAt = profile.CreatedAt.ToString("o"),
            LastLoginAt = profile.LastLoginAt.ToString("o"),
            AuthProvider = profile.AuthProvider
        };
    }

    private static SavedCountDto MapToSavedCountDto(SavedCount count)
    {
        return new SavedCountDto
        {
            Name = count.Name,
            SavedAt = count.SavedAt.ToString("o"),
            CurrencyCode = count.CurrencyCode,
            CurrencySymbol = count.CurrencySymbol,
            TotalAmount = (double)count.TotalAmount,
            BanknotesTotal = (double)count.BanknotesTotal,
            CoinsTotal = (double)count.CoinsTotal,
            Signature = new SavedCountSignatureDto
            {
                SignerName = count.Signature.SignerName,
                TypedSignature = count.Signature.TypedSignature,                
                SignedAt = count.Signature.SignedAt?.ToString("o"),
                DrawnStrokes = count.Signature.DrawnStrokes.Select(stroke => new SignatureStrokeDto
                {
                    Points = stroke.Points.Select(point => new SignaturePointDto
                    {
                        X = point.X,
                        Y = point.Y
                    }).ToList()
                }).ToList()
            },
            Denominations = count.Denominations.Select(d => new DenominationCountDto
            {
                Value = (double)d.Value,
                DisplayName = d.DisplayName,
                Quantity = d.Quantity,
                IsCoin = d.IsCoin
            }).ToList()
        };
    }

    private static SavedCount MapToSavedCount(SavedCountDto dto, string id)
    {
        return new SavedCount
        {
            Id = id,
            Name = dto.Name ?? string.Empty,
            SavedAt = DateTime.TryParse(dto.SavedAt, out var date) ? date : DateTime.Now,
            CurrencyCode = dto.CurrencyCode ?? string.Empty,
            CurrencySymbol = dto.CurrencySymbol ?? string.Empty,
            TotalAmount = (decimal)dto.TotalAmount,
            BanknotesTotal = (decimal)dto.BanknotesTotal,
            CoinsTotal = (decimal)dto.CoinsTotal,
            Signature = dto.Signature == null
                ? new SavedCountSignature()
                : new SavedCountSignature
                {
                    SignerName = dto.Signature.SignerName ?? string.Empty,
                    TypedSignature = dto.Signature.TypedSignature ?? string.Empty,                    
                    SignedAt = string.IsNullOrWhiteSpace(dto.Signature.SignedAt)
                        ? null
                        : DateTime.TryParse(dto.Signature.SignedAt, out var signedAt) ? signedAt : null,
                    DrawnStrokes = dto.Signature.DrawnStrokes?.Select(stroke => new SignatureStroke
                    {
                        Points = stroke.Points?.Select(point => new SignaturePoint
                        {
                            X = point.X,
                            Y = point.Y
                        }).ToList() ?? new List<SignaturePoint>()
                    }).ToList() ?? new List<SignatureStroke>()
                },
            Denominations = dto.Denominations?.Select(d => new DenominationCount
            {
                Value = (decimal)d.Value,
                DisplayName = d.DisplayName ?? string.Empty,
                Quantity = d.Quantity,
                IsCoin = d.IsCoin
            }).ToList() ?? new List<DenominationCount>()
        };
    }

    #endregion

    #region DTOs for Firestore

    private class UserProfileDto
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsPremium { get; set; }
        public string? PremiumExpiryDate { get; set; }
        public string? CreatedAt { get; set; }
        public string? LastLoginAt { get; set; }
        public string? AuthProvider { get; set; }
    }

    private class SavedCountDto
    {
        public string? Name { get; set; }
        public string? SavedAt { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
        public double TotalAmount { get; set; }
        public double BanknotesTotal { get; set; }
        public double CoinsTotal { get; set; }
        public SavedCountSignatureDto? Signature { get; set; }
        public List<DenominationCountDto>? Denominations { get; set; }
    }

    private class SavedCountSignatureDto
    {
        public string? SignerName { get; set; }
        public string? TypedSignature { get; set; }
        public int Mode { get; set; }
        public string? SignedAt { get; set; }
        public List<SignatureStrokeDto>? DrawnStrokes { get; set; }
    }

    private class SignatureStrokeDto
    {
        public List<SignaturePointDto>? Points { get; set; }
    }

    private class SignaturePointDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    private class DenominationCountDto
    {
        public double Value { get; set; }
        public string? DisplayName { get; set; }
        public int Quantity { get; set; }
        public bool IsCoin { get; set; }
    }

    #endregion

#else
    // Windows/macOS stub implementation - Firebase Firestore not available

    public Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.FromResult<UserProfile?>(null);
    }

    public Task SaveUserProfileAsync(UserProfile profile)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.CompletedTask;
    }

    public Task UpdatePremiumStatusAsync(string userId, bool isPremium, DateTime? expiryDate)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.CompletedTask;
    }

    public Task SyncSavedCountsAsync(string userId, List<SavedCount> counts)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.CompletedTask;
    }

    public Task<List<SavedCount>> GetSyncedCountsAsync(string userId)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.FromResult(new List<SavedCount>());
    }

    public Task DeleteUserDataAsync(string userId)
    {
        System.Diagnostics.Debug.WriteLine("Firestore not available on this platform");
        return Task.CompletedTask;
    }
#endif
}
