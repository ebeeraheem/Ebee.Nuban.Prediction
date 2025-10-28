using System.Text.Json.Serialization;

namespace Ebee.Nuban.Prediction;

public class Bank
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("long_code")]
    public string LongCode { get; set; } = string.Empty;
}