namespace Ebee.Nuban.Prediction;

public static class BankPrioritizationOptions
{
    /// <summary>
    /// Filters and prioritizes a list of banks based on predefined tiers, returning a maximum of six banks.
    /// </summary>
    /// <remarks>Banks are categorized into three groups: Tier 1, Tier 2, and others. The method ensures that
    /// Tier 1 banks are prioritized, followed by Tier 2 banks, and finally other banks if necessary to meet the minimum
    /// count of three suggestions. The total number of banks returned will not exceed six.</remarks>
    /// <param name="banks">The list of banks to filter and prioritize.</param>
    /// <returns>A list of up to six banks, prioritized by tier: Tier 1 banks are included first (up to four), followed by Tier 2
    /// banks (up to two), and then other banks if fewer than three total banks are selected from Tier 1 and Tier 2.</returns>
    public static List<Bank> ApplyBankPriorityFilter(List<Bank> banks)
    {
        var tier1Results = banks.Where(b => Tier1BankCodes.Contains(b.Code)).ToList();
        var tier2Results = banks.Where(b => Tier2BankCodes.Contains(b.Code)).ToList();
        var otherResults = banks.Where(b =>
                !Tier1BankCodes.Contains(b.Code) &&
                !Tier2BankCodes.Contains(b.Code))
            .ToList();

        // Return maximum 6 suggestions: Tier 1 first, then Tier 2, then others
        var result = new List<Bank>();
        result.AddRange(tier1Results.Take(4));
        result.AddRange(tier2Results.Take(2));

        // Only add others if we have less than 3 total suggestions
        if (result.Count < 3)
        {
            result.AddRange(otherResults.Take(3 - result.Count));
        }

        return result;
    }

    public static HashSet<string> PhoneNumberBankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "999992", // Opay
        "50515",  // Moniepoint
        "999991", // Palmpay
        "214", // FCMB
        "232"  // Sterling Bank
    };

    public static HashSet<string> Tier1BankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
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

    public static HashSet<string> Tier2BankCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
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

    public static HashSet<string> ValidPhonePrefixes { get; set; } =
    [
        // MTN
        "703", "706", "803", "806", "810", "813", "814", "816", "903", "906", "913", "916",
        // Airtel
        "701", "708", "802", "808", "812", "901", "902", "904", "907", "911", "912",
        // Glo
        "705", "805", "807", "811", "815", "905", "915",
        // 9mobile
        "809", "817", "818", "908", "909"
    ];

    public static bool IsPhoneNumberFormat(string accountNumber)
    {
        // Remove any spaces or hyphens
        accountNumber = accountNumber
            .Replace(" ", "")
            .Replace("-", "");

        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length != 10)
        {
            return false;
        }

        string prefix = accountNumber[..3];
        return ValidPhonePrefixes.Contains(prefix);
    }
}