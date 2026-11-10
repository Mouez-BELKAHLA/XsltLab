namespace XsltLab.Core;

public enum TemplateKind { Filing, Draft }
public enum TemplateStatus { Draft, Approved }

/// <summary>A template is a DB row: kind, country, validity period, status, and an uploaded file.
/// ExpressionsPath is the NEW optional file next to a draft template (author-written XSLT that derives draft-only values).</summary>
public sealed record TemplateRow(
    int Id,
    TemplateKind Kind,
    string Country,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    TemplateStatus Status,
    string FilePath,
    string? ExpressionsPath = null);

/// <summary>Stand-in for "company data for this period" (the database).</summary>
public sealed record FilingContext(string Country, string VatNumber, string Period, string Box81, bool IsCorrection);
