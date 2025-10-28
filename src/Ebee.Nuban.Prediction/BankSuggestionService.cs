using System.Reflection;
using System.Text.Json;

namespace Ebee.Nuban.Prediction;

public static class BankSuggestionService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static List<Bank>? _cachedBanks;
    private static readonly object _lockObject = new();

    /// <summary>
    /// Suggests a list of possible banks that match the provided account number.
    /// </summary>
    /// <param name="accountNumber">The account number to evaluate.</param>
    /// <param name="options">Configuration options for bank prediction. If null, default options are used.</param>
    /// <returns>A list of banks that are potential matches for the provided account number.</returns>
    /// <exception cref="ArgumentException">Thrown if the account number is invalid or banks cannot be retrieved.</exception>
    public static List<Bank> SuggestPossibleBanks(string accountNumber, NubanPredictionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));
        }

        // Remove any spaces or hyphens
        accountNumber = accountNumber
            .Replace(" ", "")
            .Replace("-", "");

        // Validate account number is exactly 10 digits
        if (accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
        {
            throw new ArgumentException("Account number must be exactly 10 digits.", nameof(accountNumber));
        }

        options ??= BankPrioritizationOptions.DefaultOptions;
        var banks = GetBanks(null, options);

        if (banks is null || banks.Count == 0)
        {
            throw new ArgumentException("Unable to retrieve banks from the data source.", nameof(accountNumber));
        }

        // Check for phone number format FIRST
        if (BankPrioritizationOptions.IsPhoneNumberFormat(accountNumber, options))
        {
            // For phone numbers, ONLY suggest the fintech banks that use phone numbers
            var phoneNumberBanks = banks
                .Where(b => options.PhoneNumberBankCodes.Contains(b.Code))
                .ToList();

            if (phoneNumberBanks.Count != 0)
            {
                // Return ONLY fintech banks for phone number format
                return phoneNumberBanks;
            }

            // Fallback: if no phone-number banks found, continue with regular validation
        }

        var possibleBanks = banks
            .Where(bank => IsValidNubanForBank(accountNumber, bank.Code))
            .ToList();

        // Apply popularity-based filtering
        return BankPrioritizationOptions.ApplyBankPriorityFilter(possibleBanks, options);
    }

    /// <summary>
    /// Retrieves a list of banks, optionally filtered by a search term.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter banks.</param>
    /// <param name="options">Configuration options containing custom banks. If null, default options are used.</param>
    /// <returns>A list of Bank objects.</returns>
    public static List<Bank> GetBanks(string? searchTerm = null, NubanPredictionOptions? options = null)
    {
        options ??= BankPrioritizationOptions.DefaultOptions;

        List<Bank> banks;

        // Use custom banks if provided, otherwise use cached/embedded banks
        if (options.CustomBanks != null && options.CustomBanks.Count > 0)
        {
            banks = options.CustomBanks;
        }
        else
        {
            banks = GetDefaultBanks();
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            banks = [.. banks.Where(b =>
                    b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    b.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
        }

        return banks;
    }

    /// <summary>
    /// Gets the default banks from embedded resource with caching.
    /// </summary>
    /// <returns>List of default banks.</returns>
    private static List<Bank> GetDefaultBanks()
    {
        if (_cachedBanks is null)
        {
            lock (_lockObject)
            {
                _cachedBanks ??= LoadBanksFromResource();
            }
        }

        return _cachedBanks;
    }

    /// <summary>
    /// Loads a list of banks from an embedded JSON resource.
    /// </summary>
    /// <returns>A list of Bank objects deserialized from the embedded JSON resource.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the JSON resource is not found.</exception>
    /// <exception cref="JsonException">Thrown if the JSON content cannot be deserialized.</exception>
    private static List<Bank> LoadBanksFromResource()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Ebee.Nuban.Prediction.banks.json") ??
            throw new FileNotFoundException("Banks JSON resource not found.");

        using var reader = new StreamReader(stream);
        var jsonContent = reader.ReadToEnd();

        return JsonSerializer.Deserialize<List<Bank>>(jsonContent, _jsonOptions) ??
            throw new JsonException("Failed to deserialize banks from JSON resource.");
    }

    /// <summary>
    /// Validates if an account number is valid for a specific bank code using NUBAN check digit algorithm.
    /// </summary>
    /// <param name="accountNumber">The 10-digit account number</param>
    /// <param name="bankCode">The bank code (3 digits for DMB or 5 digits for OFI)</param>
    /// <returns>True if the account number is valid for the bank</returns>
    public static bool IsValidNubanForBank(string accountNumber, string bankCode)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(bankCode))
        {
            return false;
        }

        accountNumber = accountNumber
            .Replace(" ", "")
            .Replace("-", "");

        bankCode = bankCode
            .Replace(" ", "")
            .Replace("-", "");

        if (accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
        {
            return false;
        }

        // Bank code must be numeric and either 3 digits (DMB) or 5 digits (OFI)
        if (!bankCode.All(char.IsDigit) || (bankCode.Length != 3 && bankCode.Length != 5))
        {
            return false;
        }

        // Convert bank code to 6-digit format
        string sixDigitBankCode;

        if (bankCode.Length == 3)
        {
            // DMB: pad with 3 leading zeros
            sixDigitBankCode = bankCode.PadLeft(6, '0');
        }
        else if (bankCode.Length == 5)
        {
            // OFI: prefix with '9'
            sixDigitBankCode = "9" + bankCode;
        }
        else
        {
            return false;
        }

        string serialNumber = accountNumber[..9];
        int providedCheckDigit = accountNumber[9] - '0';
        int? calculatedCheckDigit = CalculateNubanCheckDigit(sixDigitBankCode, serialNumber);

        return calculatedCheckDigit.HasValue && providedCheckDigit == calculatedCheckDigit;
    }

    /// <summary>
    /// Calculates the NUBAN check digit using the algorithm specified in CBN standards.
    /// </summary>
    /// <param name="sixDigitBankCode">6-digit bank code</param>
    /// <param name="serialNumber">9-digit serial number</param>
    /// <returns>The calculated check digit (0-9)</returns>
    private static int? CalculateNubanCheckDigit(string sixDigitBankCode, string serialNumber)
    {
        // Combine bank code and serial number to form 15-digit string
        string combined = sixDigitBankCode + serialNumber;

        if (combined.Length != 15 || !combined.All(char.IsDigit))
        {
            return null;
        }

        // The multiplier pattern: 3,7,3,3,7,3,3,7,3,3,7,3,3,7,3
        int[] multipliers = [3, 7, 3, 3, 7, 3, 3, 7, 3, 3, 7, 3, 3, 7, 3];

        // Step 1: Calculate sum of (digit * multiplier)
        int sum = 0;
        for (int i = 0; i < 15; i++)
        {
            int digit = int.Parse(combined[i].ToString());
            sum += digit * multipliers[i];
        }

        // Step 2: Calculate Modulo 10
        int mod = sum % 10;

        // Step 3: Calculate check digit
        int checkDigit = (10 - mod) % 10;

        return checkDigit;
    }
}
