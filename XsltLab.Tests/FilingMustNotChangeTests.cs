using System.Xml.Linq;
using XsltLab.Core;

namespace XsltLab.Tests;

/// <summary>Q2: proof that the filed XML does not change.</summary>
public class FilingMustNotChangeTests
{
    [Fact(DisplayName = "Q2 | Filed XML is byte-for-byte identical before and after a draft with expressions is generated")]
    public void Filing_bytes_identical_before_and_after_a_draft_with_expressions_is_generated()
    {
        var resolver = Env.Resolver();
        var filing = new FilingGenerator(resolver);
        var ctx = Env.Ctx();

        var before = filing.Generate(Env.Filing(), ctx);
        Env.Drafts(resolver).Generate(Env.MigratedDraft(), ctx, Env.Tmp("out.docx"));
        var after = filing.Generate(Env.Filing(), ctx);

        Assert.Equal(Env.Sha(before), Env.Sha(after));
        Assert.Equal(before, after);
    }

    [Fact(DisplayName = "Q2 | Filed XML does not depend on whether a dedicated country provider exists")]
    public void Filing_bytes_do_not_depend_on_whether_a_dedicated_provider_exists()
    {
        var ctx = Env.Ctx("DE");
        var withDedicated = new FilingGenerator(Env.Resolver(withDedicated: true)).Generate(Env.Filing("DE"), ctx);
        var withoutDedicated = new FilingGenerator(Env.Resolver(withDedicated: false)).Generate(Env.Filing("DE"), ctx);
        Assert.Equal(withDedicated, withoutDedicated);
    }

    [Fact(DisplayName = "Q2 | Filed XML has only the 4 filed fields, and Kz10 stays Yes/No (the tick box is draft-only)")]
    public void Filing_contains_only_the_four_filed_fields_and_none_of_the_draft_only_ones()
    {
        var bytes = new FilingGenerator(Env.Resolver()).Generate(Env.Filing(), Env.Ctx());
        using var ms = new MemoryStream(bytes);
        var ret = XDocument.Load(ms).Root!;

        Assert.Equal("Return", ret.Name.LocalName);
        Assert.Equal("DE123456789", (string)ret.Element("Vat")!);
        Assert.Equal("2026-Q1", (string)ret.Element("Period")!);
        Assert.Equal("12500.00", (string)ret.Element("Kz81")!);
        Assert.Equal("Yes", (string)ret.Element("Kz10")!);   // stays Yes/No: the tick box exists only in the draft
        Assert.Equal(4, ret.Elements().Count());
    }

    [Fact(DisplayName = "Q2 | Expressions work on a copy: the provider dictionary is never changed")]
    public void Provider_dictionary_is_never_mutated_by_expressions()
    {
        var provider = new GenericProvider();
        var original = provider.GetValues(Env.Ctx());
        var snapshot = new Dictionary<string, string>(original);

        new DraftExpressionEvaluator().Merge(Env.Data("expressions.xslt"), original);

        Assert.Equal(snapshot, original);
    }
}
