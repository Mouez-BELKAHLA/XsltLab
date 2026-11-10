namespace XsltLab.Core;

/// <summary>THE single place that decides which provider feeds a template (Q3).
/// The three background jobs call this instead of each keeping its own copy of the rule.
/// Rule:
///   Filing                      -> generic, always
///   Draft with expressions file -> generic (expressions derive the rest)   [migrated template]
///   Draft without               -> dedicated provider of the country if any, else generic [legacy]
/// The switch for a country is therefore DATA (the approved template row), not a deploy.</summary>
public sealed class ProviderResolver(GenericProvider generic, IEnumerable<ICountryProvider> dedicated)
{
    private readonly Dictionary<string, ICountryProvider> _dedicated = dedicated.ToDictionary(p => p.Country);

    public IValueProvider Resolve(TemplateRow template)
    {
        if (template.Kind == TemplateKind.Filing) return generic;
        if (template.ExpressionsPath is not null) return generic;
        return _dedicated.TryGetValue(template.Country, out var p) ? p : generic;
    }
}
