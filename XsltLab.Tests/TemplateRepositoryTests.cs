using XsltLab.Core;

namespace XsltLab.Tests;

public class TemplateRepositoryTests
{
    private static TemplateRow Row(int id, TemplateStatus s, DateOnly from, DateOnly? to, string country = "DE", TemplateKind k = TemplateKind.Draft) =>
        new(id, k, country, from, to, s, "x");

    [Fact(DisplayName = "Template lookup | Only APPROVED templates are used (draft-status rows are ignored)")]
    public void Only_approved_rows_are_used()
    {
        var repo = new InMemoryTemplateRepository(new[] { Row(1, TemplateStatus.Draft, new(2026, 1, 1), null) });
        Assert.Null(repo.FindApproved(TemplateKind.Draft, "DE", Env.Today));
    }

    [Fact(DisplayName = "Template lookup | The row must be valid on the filing date")]
    public void Row_must_be_valid_on_the_date()
    {
        var repo = new InMemoryTemplateRepository(new[] { Row(1, TemplateStatus.Approved, new(2025, 1, 1), new(2025, 12, 31)) });
        Assert.Null(repo.FindApproved(TemplateKind.Draft, "DE", Env.Today));
        Assert.NotNull(repo.FindApproved(TemplateKind.Draft, "DE", new(2025, 6, 1)));
    }

    [Fact(DisplayName = "Template lookup | Newest valid-from wins; country and kind must match")]
    public void Newest_valid_from_wins_and_country_and_kind_must_match()
    {
        var repo = new InMemoryTemplateRepository(new[]
        {
            Row(1, TemplateStatus.Approved, new(2025, 1, 1), null),
            Row(2, TemplateStatus.Approved, new(2026, 1, 1), null),
            Row(3, TemplateStatus.Approved, new(2026, 2, 1), null, country: "FR"),
            Row(4, TemplateStatus.Approved, new(2026, 2, 1), null, k: TemplateKind.Filing),
        });
        Assert.Equal(2, repo.FindApproved(TemplateKind.Draft, "DE", Env.Today)!.Id);
    }
}
