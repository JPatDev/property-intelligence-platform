using System.Text.RegularExpressions;

namespace PropertyIntelligence.Modules.Properties.Domain;

public sealed partial class Address
{
    private Address()
    {
    }

    private Address(string street, string city, string state, string postalCode)
    {
        Street = NormalizeRequired(street, "street", 200);
        City = NormalizeRequired(city, "city", 100);
        State = NormalizeState(state);
        PostalCode = NormalizePostalCode(postalCode);
    }

    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;

    public static Address Create(string street, string city, string state, string postalCode) =>
        new(street, city, state, postalCode);

    internal string ToIdentityKey() =>
        string.Join('|', Street, City, State, PostalCode)
            .ToUpperInvariant();

    private static string NormalizeRequired(string value, string name, int maximumLength)
    {
        var normalized = Whitespace().Replace(value?.Trim() ?? string.Empty, " ");
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException($"A property {name} is required.", name);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The property {name} cannot exceed {maximumLength} characters.",
                name);
        }

        return normalized;
    }

    private static string NormalizeState(string value)
    {
        var normalized = NormalizeRequired(value, "state", 2).ToUpperInvariant();
        if (normalized.Length != 2 || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("The property state must be a two-letter code.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizePostalCode(string value)
    {
        var normalized = NormalizeRequired(value, "postal code", 10).ToUpperInvariant();
        if (!UsPostalCode().IsMatch(normalized))
        {
            throw new ArgumentException(
                "The property postal code must be a five-digit ZIP or ZIP+4 code.",
                nameof(value));
        }

        return normalized;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^\d{5}(?:-\d{4})?$")]
    private static partial Regex UsPostalCode();
}
