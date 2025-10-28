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
    /// <remarks>This method validates the format of the account number and uses bank-specific rules to
    /// determine potential matches. If the account number is in a phone number format, the method prioritizes fintech
    /// banks that support such formats. If no matches are found for phone number formats, the method falls back to
    /// standard validation rules.</remarks>
    /// <param name="accountNumber">The account number to evaluate. The account number must be exactly 10 digits long and cannot contain spaces or
    /// hyphens.</param>
    /// <returns>A list of <see cref="Bank"/> objects representing the banks that are potential matches for the provided account
    /// number. The list may be filtered based on prioritization rules, such as popularity or specific formats (e.g.,
    /// phone number-based accounts).</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="accountNumber"/> is null, empty, contains invalid characters, or is not exactly 10
    /// digits long. Also thrown if the list of banks cannot be retrieved from the data source.</exception>
    public static List<Bank> SuggestPossibleBanks(string accountNumber)
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

        var banks = GetBanks();

        if (banks is null || banks.Count == 0)
        {
            throw new ArgumentException("Unable to retrieve banks from the data source.", nameof(accountNumber));
        }

        // Check for phone number format FIRST
        if (BankPrioritizationOptions.IsPhoneNumberFormat(accountNumber))
        {
            // For phone numbers, ONLY suggest the fintech banks that use phone numbers
            var phoneNumberBanks = banks
                .Where(b => BankPrioritizationOptions.PhoneNumberBankCodes.Contains(b.Code))
                .ToList();

            if (phoneNumberBanks.Count != 0)
            {
                // Return ONLY fintech banks for phone number format
                return phoneNumberBanks;
            }

            // Fallback: if no phone-number banks found, continue with regular validation
            // (This handles edge cases where the phone number banks might not be in the list)
        }

        var possibleBanks = banks
            .Where(bank => IsValidNubanForBank(accountNumber, bank.Code))
            .ToList();

        // Apply popularity-based filtering
        return BankPrioritizationOptions.ApplyBankPriorityFilter(possibleBanks);
    }

    /// <summary>
    /// Retrieves a list of banks, optionally filtered by a search term.
    /// </summary>
    /// <remarks>The method uses a cached list of banks for performance. The cache is initialized on the first
    /// call and remains in memory for subsequent calls.</remarks>
    /// <param name="searchTerm">An optional string used to filter the banks by name or code. The search is case-insensitive. If <paramref
    /// name="searchTerm"/> is <see langword="null"/> or whitespace, all banks are returned.</param>
    /// <returns>A list of <see cref="Bank"/> objects. If no banks match the search term, an empty list is returned.</returns>
    public static List<Bank> GetBanks(string? searchTerm = null)
    {
        if (_cachedBanks is null)
        {
            lock (_lockObject)
            {
                _cachedBanks ??= LoadBanksFromResource();
            }
        }

        var banks = _cachedBanks;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            banks = [.. banks.Where(b =>
                    b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    b.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
        }

        return banks;
    }

    /// <summary>
    /// Loads a list of banks from an embedded JSON resource.
    /// </summary>
    /// <remarks>This method retrieves the JSON resource embedded in the assembly, deserializes its content
    /// into a list of <see cref="Bank"/> objects, and returns the result. The resource must be named
    /// "Ebee.Nuban.Prediction.banks.json" and must be accessible at runtime.</remarks>
    /// <returns>A list of <see cref="Bank"/> objects deserialized from the embedded JSON resource.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the JSON resource "Ebee.Nuban.Prediction.banks.json" is not found in the assembly.</exception>
    /// <exception cref="JsonException">Thrown if the JSON resource content cannot be deserialized into a list of <see cref="Bank"/> objects.</exception>
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
