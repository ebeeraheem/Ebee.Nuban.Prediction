# Ebee.Nuban.Prediction

[![NuGet Version](https://img.shields.io/nuget/v/Ebee.Nuban.Prediction.svg)](https://www.nuget.org/packages/Ebee.Nuban.Prediction/)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/github/license/ebeeraheem/Ebee.Nuban.Prediction.svg)](LICENSE.txt)

A comprehensive .NET library for validating and predicting Nigerian bank account numbers using the **NUBAN (Nigeria Uniform Bank Account Number)** standard. This library provides intelligent bank suggestions based on account numbers and supports both traditional bank accounts and modern fintech solutions.

## Features

- ✅ **NUBAN Validation**: Validate Nigerian bank account numbers using the official CBN (Central Bank of Nigeria) algorithm
- 🏦 **Bank Prediction**: Suggest possible banks for a given account number
- 📱 **Phone Number Support**: Detect and handle phone number-based accounts for fintech banks
- 🎯 **Smart Prioritization**: Tier-based bank suggestions (Tier 1, Tier 2, and other banks)
- 🔧 **Configurable Options**: Customize bank prioritization and suggestion limits
- 🗃️ **Comprehensive Bank Database**: Built-in database of 200+ Nigerian banks
- ⚡ **High Performance**: Efficient algorithms with caching for optimal performance
- 🎨 **Easy Integration**: Simple, intuitive API design

## Installation

### Package Manager
```powershell
Install-Package Ebee.Nuban.Prediction
```

### .NET CLI
```bash
dotnet add package Ebee.Nuban.Prediction
```

### PackageReference
```xml
<PackageReference Include="Ebee.Nuban.Prediction" Version="1.0.1" />
```

## Quick Start

### Basic Usage

```csharp
using Ebee.Nuban.Prediction;

// Suggest possible banks for an account number
var suggestions = BankService.SuggestPossibleBanks("0123456789");

foreach (var bank in suggestions)
{
    Console.WriteLine($"Bank: {bank.Name} (Code: {bank.Code})");
}
```

### Validate Account Number for Specific Bank

```csharp
// Check if an account number is valid for a specific bank
bool isValid = BankService.IsValidNubanForBank("0123456789", "058"); // GTBank

if (isValid)
{
    Console.WriteLine("Valid account number for GTBank!");
}
```

### Get All Banks

```csharp
// Get all available banks
var allBanks = BankService.GetBanks();

// Search for specific banks
var searchResults = BankService.GetBanks("Access");
```

## Advanced Configuration

### Custom Options

```csharp
var options = new NubanPredictionOptions
{
    MaxTier1Results = 3,
    MaxTier2Results = 2,
    MinimumSuggestions = 2,
    MaximumSuggestions = 5
};

var suggestions = BankService.SuggestPossibleBanks("0123456789", options);
```

### Custom Bank Database

```csharp
var customBanks = new List<Bank>
{
    new("My Custom Bank", "999"),
    new("Another Bank", "888")
};

var options = new NubanPredictionOptions
{
    CustomBanks = customBanks
};

var suggestions = BankService.SuggestPossibleBanks("0123456789", options);
```

### Phone Number Detection

The library automatically detects phone number formats and suggests appropriate fintech banks:

```csharp
// Phone number format (Nigerian mobile number)
var phoneBasedSuggestions = BankService.SuggestPossibleBanks("0801234567");

// Returns fintech banks that support phone numbers as account numbers:
// - OPay, Moniepoint, PalmPay, FCMB, Sterling Bank
```

## Bank Tiers

The library categorizes banks into tiers for intelligent prioritization:

### Tier 1 Banks (Highest Priority)
- First Bank of Nigeria (011)
- Access Bank (044)
- Kuda Bank (50211)
- Zenith Bank (057)
- GTBank (058)
- Moniepoint (50515)
- Fidelity Bank (070)
- United Bank for Africa (033)
- FCMB (214)
- Sterling Bank (232)
- Wema Bank (035)

### Tier 2 Banks (Medium Priority)
- Ecobank (050)
- Unity Bank (215)
- Keystone Bank (082)
- Union Bank (032)
- Polaris Bank (076)
- Stanbic IBTC (221)
- Standard Chartered (068)
- Citibank (023)
- Jaiz Bank (301)
- Providus Bank (101)
- Suntrust Bank (100)
- TAJ Bank (302)
- Lotus Bank (303)

### Phone Number Supporting Banks
- OPay (999992)
- Moniepoint (50515)
- PalmPay (999991)

## Configuration Options

### NubanPredictionOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CustomBanks` | `List<Bank>?` | `null` | Custom bank list (overrides default banks) |
| `Tier1BankCodes` | `HashSet<string>` | Predefined | Bank codes for highest priority banks |
| `Tier2BankCodes` | `HashSet<string>` | Predefined | Bank codes for medium priority banks |
| `PhoneNumberBankCodes` | `HashSet<string>` | Predefined | Banks supporting phone numbers as accounts |
| `ValidPhonePrefixes` | `HashSet<string>` | Nigerian prefixes | Valid Nigerian phone number prefixes |
| `MaxTier1Results` | `int` | `4` | Maximum Tier 1 banks in results |
| `MaxTier2Results` | `int` | `2` | Maximum Tier 2 banks in results |
| `MinimumSuggestions` | `int` | `3` | Minimum total suggestions to return |
| `MaximumSuggestions` | `int` | `6` | Maximum total suggestions to return |

### Example: Custom Configuration

```csharp
var customOptions = new NubanPredictionOptions
{
    // Add custom Tier 1 banks
    Tier1BankCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "044", "058", "070" // Access, GTBank, Fidelity only
    },
    
    // Increase suggestion limits
    MaxTier1Results = 5,
    MaxTier2Results = 3,
    MaximumSuggestions = 8,
    
    // Custom phone prefixes
    ValidPhonePrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "803", "814", "703" // Custom phone prefixes
    }
};
```

## Error Handling

The library throws specific exceptions for different error scenarios:

```csharp
try
{
    var suggestions = BankService.SuggestPossibleBanks("invalid");
}
catch (ArgumentException ex)
{
    // Handle invalid input:
    // - Null or empty account number
    // - Invalid account number format
    // - Unable to retrieve banks
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Supported Account Number Formats

### Traditional Bank Accounts
- **Format**: 10 digits
- **Example**: `0123456789`
- **Validation**: NUBAN check digit algorithm

### Phone Number Accounts
- **Format**: 10 digits (Nigerian mobile format)
- **Example**: `0801234567`
- **Supported Networks**: MTN, Airtel, Glo, 9mobile
- **Validation**: Phone prefix validation

## NUBAN Algorithm

The library implements the official CBN NUBAN validation algorithm:

1. **Bank Code Conversion**: Convert bank codes to 6-digit format
2. **Check Digit Calculation**: Use multiplier pattern [3,7,3,3,7,3,3,7,3,3,7,3,3,7,3]
3. **Modulo Operation**: Calculate modulo 10 and derive check digit
4. **Validation**: Compare calculated vs. provided check digit

## Performance Considerations

- **Caching**: Bank data is cached in memory for optimal performance
- **Thread Safety**: All public methods are thread-safe
- **Memory Efficient**: Embedded JSON resource for bank data
- **Fast Lookup**: HashSet-based tier and prefix matching

## Examples

### Complete Example: Account Validation Service

```csharp
using Ebee.Nuban.Prediction;

public class AccountValidationService
{
    private readonly NubanPredictionOptions _options;

    public AccountValidationService()
    {
        _options = new NubanPredictionOptions
        {
            MaxTier1Results = 3,
            MaxTier2Results = 2,
            MaximumSuggestions = 5
        };
    }

    public async Task<ValidationResult> ValidateAccountAsync(string accountNumber, string? bankCode = null)
    {
        try
        {
            // Get bank suggestions
            var suggestions = BankService.SuggestPossibleBanks(accountNumber, _options);

            if (bankCode != null)
            {
                // Validate for specific bank
                var isValid = BankService.IsValidNubanForBank(accountNumber, bankCode);
                var bank = suggestions.FirstOrDefault(b => b.Code.Equals(bankCode, StringComparison.OrdinalIgnoreCase));

                return new ValidationResult
                {
                    IsValid = isValid,
                    Bank = bank,
                    AllSuggestions = suggestions
                };
            }

            return new ValidationResult
            {
                IsValid = suggestions.Any(),
                AllSuggestions = suggestions
            };
        }
        catch (ArgumentException ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public Bank? Bank { get; set; }
    public List<Bank> AllSuggestions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
```

### Phone Number Account Detection

```csharp
public bool IsPhoneNumberAccount(string accountNumber)
{
    return BankPrioritizationOptions.IsPhoneNumberFormat(accountNumber);
}

public List<Bank> GetFintechBanks(string phoneNumber)
{
    if (IsPhoneNumberAccount(phoneNumber))
    {
        return BankService.SuggestPossibleBanks(phoneNumber);
    }
    
    return new List<Bank>();
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository
2. Ensure you have .NET 8.0 SDK installed
3. Run `dotnet restore`
4. Run `dotnet build`
5. Run tests with `dotnet test`

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Acknowledgments

- Central Bank of Nigeria (CBN) for the NUBAN specification
- Nigerian banking industry for standardizing account number formats
- Open source community for inspiration and best practices

## Support

If you encounter any issues or have questions, please:

1. Check the [documentation](#features)
2. Search [existing issues](https://github.com/ebeeraheem/Ebee.Nuban.Prediction/issues)
3. Create a [new issue](https://github.com/ebeeraheem/Ebee.Nuban.Prediction/issues/new) if needed

## Changelog

### [1.0.1] - 2025-11-01
- Fixed bug in phone number prefix validation
- Removed duplicate Wema Bank entry
- Add new BAIGE MFB to the bank database

### [1.0.0] - 2025-10-28
- Initial release
- NUBAN validation algorithm implementation
- Bank suggestion service
- Phone number account support
- Configurable bank prioritization
- Comprehensive bank database (200+ banks)

---

**Made with ❤️ for the Nigerian fintech ecosystem**