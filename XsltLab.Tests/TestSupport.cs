using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using XsltLab.Core;

namespace XsltLab.Tests;

internal static class Env
{
    public static string Data(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    public static string Tmp(string name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "xsltlab");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, Guid.NewGuid().ToString("N") + "-" + name);
    }

    public static readonly DateOnly Today = new(2026, 3, 15);

    public static FilingContext Ctx(string country = "DE", string box81 = "12500.00", bool correction = true) =>
        new(country, country + "123456789", "2026-Q1", box81, correction);

    public static ProviderResolver Resolver(bool withDedicated = true) =>
        new(new GenericProvider(), withDedicated ? new ICountryProvider[] { new DeProvider(), new FrProvider() } : Array.Empty<ICountryProvider>());

    public static DraftGenerator Drafts(ProviderResolver r) => new(r, new DraftExpressionEvaluator(), new DocxRenderer());

    public static TemplateRow Filing(string country = "DE", TemplateStatus s = TemplateStatus.Approved) =>
        new(1, TemplateKind.Filing, country, new DateOnly(2026, 1, 1), null, s, Data("filing.xslt"));

    /// <summary>Legacy draft: no expressions, values come from the dedicated provider (or generic).</summary>
    public static TemplateRow LegacyDraft(string country = "DE") =>
        new(2, TemplateKind.Draft, country, new DateOnly(2026, 1, 1), null, TemplateStatus.Approved, DraftDocx());

    /// <summary>Migrated draft: author-written expressions, generic provider.</summary>
    public static TemplateRow MigratedDraft(string country = "DE", string? expressions = null) =>
        new(3, TemplateKind.Draft, country, new DateOnly(2026, 1, 1), null, TemplateStatus.Approved, DraftDocx(),
            expressions ?? Data("expressions.xslt"));

    public static string Sha(byte[] b) => Convert.ToHexString(SHA256.HashData(b));

    private static readonly object Gate = new();
    private static string? _docx;

    /// <summary>Minimal real .docx (zip of XML) with the draft placeholders, built once.</summary>
    public static string DraftDocx()
    {
        lock (Gate)
        {
        if (_docx is not null && File.Exists(_docx)) return _docx;
        string[] lines =
        {
            "Draft VAT return",
            "VAT number: {{VatNumber}}",
            "VAT number without first letter: {{VatNumberNoPrefix}}",
            "Period: {{Period}}",
            "Box 81: {{Box81}}",
            "Is this a correction? {{IsCorrectionBox}} ({{IsCorrection}})",
            "Big correction (over 1000)? {{IsBigCorrection}}",
        };
        var body = string.Concat(lines.Select(l =>
            $"<w:p><w:r><w:t xml:space=\"preserve\">{System.Security.SecurityElement.Escape(l)}</w:t></w:r></w:p>"));
        var document = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + body + "</w:body></w:document>";

        var path = Tmp("draft-template.docx");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            void Add(string name, string content)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                w.Write(content);
            }
            Add("[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Add("_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Add("word/document.xml", document);
        }
        _docx = path;
        return path;
        }
    }
}
