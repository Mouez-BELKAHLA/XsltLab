namespace XsltLab.Core;

public interface IValueProvider
{
    /// <summary>Returns a NEW dictionary each call, so callers can never mutate shared state.</summary>
    Dictionary<string, string> GetValues(FilingContext ctx);
}

public interface ICountryProvider : IValueProvider
{
    string Country { get; }
}

/// <summary>Plain lookups only. Filings are ALWAYS fed by this one.</summary>
public sealed class GenericProvider : IValueProvider
{
    public Dictionary<string, string> GetValues(FilingContext ctx) => new()
    {
        ["VatNumber"] = ctx.VatNumber,
        ["Period"] = ctx.Period,
        ["Box81"] = ctx.Box81,
        ["IsCorrection"] = ctx.IsCorrection ? "Yes" : "No",
    };
}

/// <summary>TODAY's way: a decision lives as a line of C# (PR + release per change). One of ~25 such classes.</summary>
public sealed class DeProvider : ICountryProvider
{
    public string Country => "DE";

    public Dictionary<string, string> GetValues(FilingContext ctx)
    {
        var d = new GenericProvider().GetValues(ctx);
        d["VatNumberNoPrefix"] = ctx.VatNumber.Length > 1 ? ctx.VatNumber[1..] : "";
        d["IsBigCorrection"] = ctx.IsCorrection && decimal.Parse(ctx.Box81, System.Globalization.CultureInfo.InvariantCulture) > 1000m ? "Yes" : "No";
        d["IsCorrectionBox"] = ctx.IsCorrection ? "\u2612" : "\u2610";
        return d;
    }
}

/// <summary>A second dedicated provider with different logic, to show the pattern (the other ~23 look the same).</summary>
public sealed class FrProvider : ICountryProvider
{
    public string Country => "FR";

    public Dictionary<string, string> GetValues(FilingContext ctx)
    {
        var d = new GenericProvider().GetValues(ctx);
        d["VatNumberNoPrefix"] = ctx.VatNumber.Length > 2 ? ctx.VatNumber[2..] : "";
        return d;
    }
}
