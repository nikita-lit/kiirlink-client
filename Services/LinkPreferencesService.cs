namespace KiirLink.Services;

public sealed class LinkPreferencesService
{
    private const string CategoryIdKey = "link_default_category_id";
    private const string CategoryNameKey = "link_default_category_name";
    private const string ExpirationDaysKey = "link_auto_expiration_days";
    private const string ExpirationDateKey = "link_auto_expiration_date";
    private const string IsPublicKey = "link_default_is_public";

    public int? DefaultCategoryId
    {
        get
        {
            var value = Preferences.Default.Get(CategoryIdKey, 0);
            return value > 0 ? value : null;
        }
    }

    public string DefaultCategoryName =>
        Preferences.Default.Get(CategoryNameKey, "None");

    public DateTime? AutoExpirationDate
    {
        get
        {
            var stored = Preferences.Default.Get(ExpirationDateKey, string.Empty);
            if (DateTime.TryParse(stored, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date))
                return date.Date;

            var legacyDays = Preferences.Default.Get(ExpirationDaysKey, 0);
            if (legacyDays <= 0)
                return null;

            var migrated = DateTime.Today.AddDays(legacyDays);
            SetAutoExpiration(migrated);
            return migrated;
        }
    }

    public bool IsPublic => Preferences.Default.Get(IsPublicKey, true);

    public string AutoExpirationDisplay =>
        AutoExpirationDate is DateTime date ? date.ToString("MMM d, yyyy") : "Never";

    public string PrivacyDisplay => IsPublic ? "Public" : "Private";

    public void SetDefaultCategory(int? categoryId, string? categoryName)
    {
        Preferences.Default.Set(CategoryIdKey, categoryId ?? 0);
        Preferences.Default.Set(CategoryNameKey,
            categoryId.HasValue && !string.IsNullOrWhiteSpace(categoryName) ? categoryName : "None");
    }

    public void SetAutoExpiration(DateTime? date)
    {
        Preferences.Default.Set(
            ExpirationDateKey,
            date?.Date.ToString("O") ?? string.Empty);
        Preferences.Default.Remove(ExpirationDaysKey);
    }

    public void SetPrivacy(bool isPublic)
    {
        Preferences.Default.Set(IsPublicKey, isPublic);
    }
}
