using XsltLab.Core;

namespace XsltLab.Tests;

public class DraftTests
{
    private readonly ProviderResolver _resolver = Env.Resolver();

    private string RenderText(TemplateRow row, FilingContext ctx)
    {
        var path = Env.Tmp("out.docx");
        Env.Drafts(_resolver).Generate(row, ctx, path);
        return DocxRenderer.ReadText(path);
    }

    [Theory(DisplayName = "Q1 | Content-team rule IsBigCorrection (correction AND Box 81 above 1000)")]
    [InlineData("12500.00", true, "Yes")]
    [InlineData("500.00", true, "No")]
    [InlineData("12500.00", false, "No")]
    [InlineData("1000.00", true, "No")]   // boundary: strictly above 1000
    [InlineData("1000.01", true, "Yes")]
    public void IsBigCorrection_follows_the_rule_written_by_the_content_team(string box81, bool correction, string expected)
    {
        var values = Env.Drafts(_resolver).Generate(Env.MigratedDraft(), Env.Ctx(box81: box81, correction: correction), Env.Tmp("o.docx"));
        Assert.Equal(expected, values["IsBigCorrection"]);
    }

    [Fact(DisplayName = "Q1 | Draft shows a ticked box for a correction and an empty box otherwise")]
    public void Draft_shows_ticked_box_for_a_correction_and_empty_box_otherwise()
    {
        Assert.Contains("correction? \u2612 (Yes)", RenderText(Env.MigratedDraft(), Env.Ctx(correction: true)));
        Assert.Contains("correction? \u2610 (No)", RenderText(Env.MigratedDraft(), Env.Ctx(correction: false)));
    }

    [Fact(DisplayName = "Q1 | Derived value: VAT number without its first character")]
    public void VatNumberNoPrefix_drops_the_first_character()
    {
        Assert.Contains("without first letter: E123456789", RenderText(Env.MigratedDraft(), Env.Ctx()));
    }

    /// <summary>Q7: the evidence used to decide when a dedicated provider can be deleted.</summary>
    [Fact(DisplayName = "Q7 | Parity: the migrated draft renders exactly like the dedicated provider did (evidence to delete it)")]
    public void Migrated_draft_renders_exactly_like_the_dedicated_provider_did()
    {
        foreach (var (box, corr) in new[] { ("12500.00", true), ("500.00", true), ("12500.00", false) })
        {
            var ctx = Env.Ctx("DE", box, corr);
            Assert.Equal(RenderText(Env.LegacyDraft("DE"), ctx), RenderText(Env.MigratedDraft("DE"), ctx));
        }
    }

    [Fact(DisplayName = "Q5 | A placeholder with no value fails the render and names the placeholder")]
    public void Missing_placeholder_fails_the_render_and_names_it()
    {
        // IT has no dedicated provider and the template has no expressions => VatNumberNoPrefix has no value
        var ex = Assert.Throws<InvalidOperationException>(() => RenderText(Env.LegacyDraft("IT"), Env.Ctx("IT")));
        Assert.Contains("VatNumberNoPrefix", ex.Message);
    }
}
