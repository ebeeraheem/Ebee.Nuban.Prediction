namespace Ebee.Nuban.Prediction;

/// <summary>
/// Provides options and utilities for prioritizing and filtering banks based on predefined tiers, validating phone
/// number formats, and managing bank-related configurations.
/// </summary>
public static class BankPrioritizationOptions
{
    /// <summary>
    /// Default configuration options for NUBAN prediction.
    /// </summary>
    public static NubanPredictionOptions DefaultOptions { get; } = new();

    /// <summary>
    /// Filters and prioritizes a list of banks based on the provided options.
    /// </summary>
    /// <param name="banks">The list of banks to filter and prioritize.</param>
    /// <param name="options">Configuration options for prioritization. If null, default options are used.</param>
    /// <returns>A list of banks prioritized according to the specified options.</returns>
    public static List<Bank> ApplyBankPriorityFilter(List<Bank> banks, NubanPredictionOptions? options = null)
    {
        options ??= DefaultOptions;

        var tier1Results = banks.Where(b => options.Tier1BankCodes.Contains(b.Code)).ToList();
        var tier2Results = banks.Where(b => options.Tier2BankCodes.Contains(b.Code)).ToList();
        var otherResults = banks.Where(b =>
                !options.Tier1BankCodes.Contains(b.Code) &&
                !options.Tier2BankCodes.Contains(b.Code))
            .ToList();

        var result = new List<Bank>();
        result.AddRange(tier1Results.Take(options.MaxTier1Results));
        result.AddRange(tier2Results.Take(options.MaxTier2Results));

        // Only add others if we have less than minimum suggestions
        if (result.Count < options.MinimumSuggestions)
        {
            result.AddRange(otherResults.Take(options.MinimumSuggestions - result.Count));
        }

        // Ensure we don't exceed maximum suggestions
        return [.. result.Take(options.MaximumSuggestions)];
    }

    /// <summary>
    /// Determines if the account number is in phone number format.
    /// </summary>
    /// <param name="accountNumber">The account number to check.</param>
    /// <param name="options">Configuration options containing valid phone prefixes. If null, default options are used.</param>
    /// <returns>True if the account number matches a phone number format.</returns>
    public static bool IsPhoneNumberFormat(string accountNumber, NubanPredictionOptions? options = null)
    {
        options ??= DefaultOptions;

        // Remove any spaces or hyphens
        accountNumber = accountNumber
            .Replace(" ", "")
            .Replace("-", "");

        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length != 10)
        {
            return false;
        }

        string prefix = accountNumber[..3];
        return options.ValidPhonePrefixes.Contains(prefix);
    }
}
