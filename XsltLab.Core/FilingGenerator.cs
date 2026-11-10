namespace XsltLab.Core;

/// <summary>Filing path. Knows NOTHING about draft expressions.</summary>
public sealed class FilingGenerator(ProviderResolver resolver)
{
    public byte[] Generate(TemplateRow filingTemplate, FilingContext ctx)
    {
        var values = resolver.Resolve(filingTemplate).GetValues(ctx);
        return XsltEngine.Transform(XsltEngine.Compile(filingTemplate.FilePath), XsltEngine.ToSourceXml(values));
    }
}
