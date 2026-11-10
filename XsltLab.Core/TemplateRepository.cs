namespace XsltLab.Core;

public sealed class InMemoryTemplateRepository(IEnumerable<TemplateRow> rows)
{
    /// <summary>Generation uses the APPROVED row that is valid on the given date; newest ValidFrom wins.</summary>
    public TemplateRow? FindApproved(TemplateKind kind, string country, DateOnly on) =>
        rows.Where(r => r.Kind == kind
                        && r.Country == country
                        && r.Status == TemplateStatus.Approved
                        && r.ValidFrom <= on
                        && (r.ValidTo is null || on <= r.ValidTo))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();
}
