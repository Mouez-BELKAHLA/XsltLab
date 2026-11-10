namespace XsltLab.Core;

public sealed class DraftGenerator(ProviderResolver resolver, DraftExpressionEvaluator evaluator, DocxRenderer renderer)
{
    public Dictionary<string, string> Generate(TemplateRow draftTemplate, FilingContext ctx, string outPath)
    {
        var values = resolver.Resolve(draftTemplate).GetValues(ctx);
        if (draftTemplate.ExpressionsPath is not null)
            values = evaluator.Merge(draftTemplate.ExpressionsPath, values);
        renderer.Render(draftTemplate.FilePath, outPath, values);
        return values;
    }
}
