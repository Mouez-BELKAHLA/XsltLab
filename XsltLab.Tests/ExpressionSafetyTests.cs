using XsltLab.Core;

namespace XsltLab.Tests;

/// <summary>Q5/Q6: what we refuse to let author-written XSLT do.</summary>
public class ExpressionSafetyTests
{
    private const string Header = "<?xml version=\"1.0\"?><xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"><xsl:template match=\"/values\"><values>";
    private const string Footer = "</values></xsl:template></xsl:stylesheet>";

    private static string Write(string inner)
    {
        var p = Env.Tmp("expr.xslt");
        File.WriteAllText(p, Header + inner + Footer);
        return p;
    }

    private static Dictionary<string, string> Values() => new GenericProvider().GetValues(Env.Ctx());

    [Fact(DisplayName = "Q5 | A derived name may not shadow an existing provider field")]
    public void A_derived_name_may_not_shadow_a_provider_field()
    {
        var path = Write("<value name=\"VatNumber\">X</value>");
        var ex = Assert.Throws<InvalidOperationException>(() => new DraftExpressionEvaluator().Merge(path, Values()));
        Assert.Contains("VatNumber", ex.Message);
    }

    [Fact(DisplayName = "Q6 | document() is disabled, so author XSLT cannot read files or URLs")]
    public void document_function_is_disabled()
    {
        var path = Write("<value name=\"Leak\"><xsl:value-of select=\"document('file:///etc/passwd')\"/></value>");
        Assert.ThrowsAny<Exception>(() => new DraftExpressionEvaluator().Merge(path, Values()));
    }

    [Fact(DisplayName = "Q6 | Embedded scripts are disabled, so author XSLT cannot run code")]
    public void Scripts_are_disabled()
    {
        var p = Env.Tmp("script.xslt");
        File.WriteAllText(p,
            "<?xml version=\"1.0\"?><xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" " +
            "xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:u=\"urn:u\" exclude-result-prefixes=\"msxsl u\">" +
            "<msxsl:script language=\"C#\" implements-prefix=\"u\">public string Hi(){return \"hi\";}</msxsl:script>" +
            "<xsl:template match=\"/values\"><values><value name=\"S\"><xsl:value-of select=\"u:Hi()\"/></value></values></xsl:template></xsl:stylesheet>");
        Assert.ThrowsAny<Exception>(() => new DraftExpressionEvaluator().Merge(p, Values()));
    }

    [Fact(DisplayName = "Q5 | The expressions output must be a <values> document")]
    public void Output_must_be_a_values_document()
    {
        var p = Env.Tmp("wrong-root.xslt");
        File.WriteAllText(p,
            "<?xml version=\"1.0\"?><xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">" +
            "<xsl:template match=\"/values\"><Return/></xsl:template></xsl:stylesheet>");
        Assert.Throws<InvalidOperationException>(() => new DraftExpressionEvaluator().Merge(p, Values()));
    }
}
