using System.Xml.Linq;

namespace XsltLab.Core;

/// <summary>Runs the author-written expressions XSLT over the SAME source XML as the filing
/// and merges the derived values into a COPY of the dictionary.</summary>
public sealed class DraftExpressionEvaluator
{
    public Dictionary<string, string> Merge(string expressionsPath, IReadOnlyDictionary<string, string> values)
    {
        var bytes = XsltEngine.Transform(XsltEngine.Compile(expressionsPath), XsltEngine.ToSourceXml(values));
        using var ms = new MemoryStream(bytes);
        var root = XDocument.Load(ms).Root ?? throw new InvalidOperationException("Expressions produced no root element");
        if (root.Name.LocalName != "values")
            throw new InvalidOperationException("Expressions root must be <values>");

        var merged = new Dictionary<string, string>(values);
        foreach (var v in root.Elements("value"))
        {
            var name = (string?)v.Attribute("name") ?? throw new InvalidOperationException("<value> without name");
            if (merged.ContainsKey(name))
                throw new InvalidOperationException($"Derived name '{name}' shadows an existing field");
            merged[name] = v.Value.Trim();
        }
        return merged;
    }
}
