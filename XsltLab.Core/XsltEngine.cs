using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XsltLab.Core;

/// <summary>.NET's built-in XSLT 1.0 engine, hardened: no document(), no scripts, no resolver, no DTDs.</summary>
public static class XsltEngine
{
    public static string ToSourceXml(IReadOnlyDictionary<string, string> d) =>
        new XElement("values", d.Select(kv => new XElement(kv.Key, kv.Value))).ToString(SaveOptions.DisableFormatting);

    public static XslCompiledTransform Compile(string path)
    {
        var xslt = new XslCompiledTransform();
        var settings = new XsltSettings(enableDocumentFunction: false, enableScript: false);
        xslt.Load(path, settings, stylesheetResolver: null);
        return xslt;
    }

    /// <summary>Returns the raw output BYTES, so "byte for byte" comparisons are real.</summary>
    public static byte[] Transform(XslCompiledTransform xslt, string sourceXml)
    {
        using var reader = XmlReader.Create(new StringReader(sourceXml),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, xslt.OutputSettings))
            xslt.Transform(reader, writer);
        return ms.ToArray();
    }
}
