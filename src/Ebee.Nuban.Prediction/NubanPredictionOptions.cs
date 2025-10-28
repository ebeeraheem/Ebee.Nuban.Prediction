namespace Ebee.Nuban.Prediction;

/// <summary>
/// Configuration options for NUBAN prediction and bank prioritization.
/// </summary>
public class NubanPredictionOptions
{
    /// <summary>
    /// Custom list of banks. If null, the default embedded banks will be used.
    /// </summary>
    public List<Bank>? CustomBanks { get; set; }

    /// <summary>
    /// Bank codes for Tier 1 (highest priority) banks.
    /// </summary>
    public HashSet<string> Tier1BankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "011", // First Bank of Nigeria
        "044", // Access Bank  
        "50211", // Kuda Bank
        "057", // Zenith Bank
        "058", // GTBank
        "50515",  // Moniepoint
        "070", // Fidelity Bank
        "033", // United Bank for Africa
        "214", // FCMB
        "232", // Sterling Bank
        "035"  // Wema Bank
    };

    /// <summary>
    /// Bank codes for Tier 2 (medium priority) banks.
    /// </summary>
    public HashSet<string> Tier2BankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "050", // Ecobank
        "215", // Unity Bank
        "082", // Keystone Bank
        "032", // Union Bank
        "076", // Polaris Bank
        "221", // Stanbic IBTC
        "068", // Standard Chartered
        "023", // Citibank
        "301", // Jaiz Bank
        "101", // Providus Bank
        "100", // Suntrust Bank
        "302", // TAJ Bank
        "303"  // Lotus Bank
    };

    /// <summary>
    /// Bank codes that support phone number as account numbers.
    /// </summary>
    public HashSet<string> PhoneNumberBankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "999992", // Opay
        "50515",  // Moniepoint
        "999991", // Palmpay
        "214", // FCMB
        "232"  // Sterling Bank
    };

    /// <summary>
    /// Valid phone number prefixes for Nigerian mobile networks.
    /// </summary>
    public HashSet<string> ValidPhonePrefixes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        // MTN
        "703", "706", "803", "806", "810", "813", "814", "816", "903", "906", "913", "916",
        // Airtel
        "701", "708", "802", "808", "812", "901", "902", "904", "907", "911", "912",
        // Glo
        "705", "805", "807", "811", "815", "905", "915",
        // 9mobile
        "809", "817", "818", "908", "909"
    };

    /// <summary>
    /// Maximum number of Tier 1 banks to include in results.
    /// </summary>
    public int MaxTier1Results { get; set; } = 4;

    /// <summary>
    /// Maximum number of Tier 2 banks to include in results.
    /// </summary>
    public int MaxTier2Results { get; set; } = 2;

    /// <summary>
    /// Minimum number of total suggestions to return.
    /// </summary>
    public int MinimumSuggestions { get; set; } = 3;

    /// <summary>
    /// Maximum number of total suggestions to return.
    /// </summary>
    public int MaximumSuggestions { get; set; } = 6;
}