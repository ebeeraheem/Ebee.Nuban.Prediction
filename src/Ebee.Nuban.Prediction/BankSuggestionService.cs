using System.Text.Json;

namespace Ebee.Nuban.Prediction;

public static class BankSuggestionService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Suggests a list of possible banks that match the provided account number.
    /// </summary>
    /// <remarks>This method validates the account number format and uses it to filter banks based on their 
    /// compatibility with the provided account number. If the account number resembles a phone number,  the method
    /// prioritizes fintech banks that support phone number formats. The final list of banks  is further refined based
    /// on popularity-based filtering.</remarks>
    /// <param name="accountNumber">The account number to validate and use for suggesting banks. The account number must be exactly  10 digits long
    /// and cannot contain spaces or hyphens.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of banks  that are potential
    /// matches for the provided account number. If the account number is in a phone  number format, the result may
    /// prioritize fintech banks that support such formats.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="accountNumber"/> is null, empty, not exactly 10 digits, or if banks  cannot be
    /// retrieved from the data source.</exception>
    public static async Task<List<Bank>> SuggestPossibleBanksAsync(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));
        }

        // Remove any spaces or hyphens
        accountNumber = accountNumber.Replace(" ", "").Replace("-", "");

        // Validate account number is exactly 10 digits
        if (accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
        {
            throw new ArgumentException("Account number must be exactly 10 digits.", nameof(accountNumber));
        }

        var banks = await GetBanksAsync();

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

    public static async Task<List<Bank>> GetBanksAsync(string? searchTerm = null)
    {
        var resourcePath = Path.Combine(
            AppContext.BaseDirectory, "banks.json");

        if (!File.Exists(resourcePath))
        {
            throw new FileNotFoundException("Banks JSON file not found.", resourcePath);
        }

        var jsonContent = await File.ReadAllTextAsync(resourcePath);
        var banks = JsonSerializer.Deserialize<List<Bank>>(jsonContent, _jsonOptions) ??
            throw new JsonException("Failed to deserialize banks from JSON file.");
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            banks = [.. banks.Where(b =>
                    b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    b.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
        }

        return banks;
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

        // Extract the 9-digit serial number from the account number
        string serialNumber = accountNumber[..9];

        // Extract the check digit from the account number
        int providedCheckDigit = accountNumber[9] - '0';

        // Calculate the expected check digit
        int? calculatedCheckDigit = CalculateNubanCheckDigit(sixDigitBankCode, serialNumber);

        // Handle invalid check digit
        if (calculatedCheckDigit is null)
        {
            return false;
        }

        return providedCheckDigit == calculatedCheckDigit;
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
