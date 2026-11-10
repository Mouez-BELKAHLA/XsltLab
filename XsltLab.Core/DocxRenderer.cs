using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XsltLab.Core;

/// <summary>Stand-in for the shared renderer: replaces {{Placeholders}} in the body of a .docx.
/// - Word often splits a typed placeholder across several runs, so text is joined per paragraph first
///   (formatting INSIDE a paragraph that contains a placeholder collapses to the first run: fine for a demo).
/// - A placeholder without a value fails the render and reports its name (like the real one).
/// - Headers, footers and text boxes are not handled here.</summary>
public sealed class DocxRenderer
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly Regex Placeholder = new(@"\{\{\s*(\w+)\s*\}\}");

    public void Render(string templatePath, string outPath, IReadOnlyDictionary<string, string> values)
    {
        File.Copy(templatePath, outPath, overwrite: true);
        using var zip = ZipFile.Open(outPath, ZipArchiveMode.Update);
        var entry = zip.GetEntry("word/document.xml") ?? throw new InvalidOperationException("Not a .docx");
        XDocument doc;
        using (var r = entry.Open()) doc = XDocument.Load(r);

        foreach (var p in doc.Descendants(W + "p"))
        {
            var texts = p.Descendants(W + "t").ToList();
            if (texts.Count == 0) continue;
            var full = string.Concat(texts.Select(t => t.Value));
            if (!full.Contains("{{")) continue;

            var replaced = Placeholder.Replace(full, m =>
                values.TryGetValue(m.Groups[1].Value, out var val)
                    ? val
                    : throw new InvalidOperationException($"No value for placeholder {m.Groups[1].Value}"));

            texts[0].Value = replaced;
            texts[0].SetAttributeValue(XNamespace.Xml + "space", "preserve");
            foreach (var t in texts.Skip(1)) t.Value = "";
        }

        entry.Delete();
        using var w = new StreamWriter(zip.CreateEntry("word/document.xml").Open(), new UTF8Encoding(false));
        doc.Save(w, SaveOptions.DisableFormatting);
    }

    public static string ReadText(string docxPath)
    {
        using var zip = ZipFile.OpenRead(docxPath);
        using var r = zip.GetEntry("word/document.xml")!.Open();
        var doc = XDocument.Load(r);
        return string.Join("\n", doc.Descendants(W + "p").Select(p => string.Concat(p.Descendants(W + "t").Select(t => t.Value))));
    }
}
