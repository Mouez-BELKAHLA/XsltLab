using XsltLab.Core;

namespace XsltLab.Tests;

/// <summary>Q3: one decision point for "generic vs dedicated provider".</summary>
public class RoutingTests
{
    private readonly ProviderResolver _r = Env.Resolver();

    [Fact(DisplayName = "Q3 | A filing always uses the generic provider, even if the country has a dedicated one")] public void Filing_always_uses_generic_even_when_country_has_a_dedicated_provider() =>
        Assert.IsType<GenericProvider>(_r.Resolve(Env.Filing("DE")));

    [Fact(DisplayName = "Q3 | A legacy draft (no expressions file) uses the country's dedicated provider")] public void Legacy_draft_uses_the_dedicated_provider() =>
        Assert.IsType<DeProvider>(_r.Resolve(Env.LegacyDraft("DE")));

    [Fact(DisplayName = "Q3 | A legacy draft in a country with no dedicated provider uses the generic one")] public void Legacy_draft_without_dedicated_provider_uses_generic() =>
        Assert.IsType<GenericProvider>(_r.Resolve(Env.LegacyDraft("IT")));

    [Fact(DisplayName = "Q3/Q7 | A migrated draft (has expressions) uses the generic provider even if a dedicated one still exists")] public void Migrated_draft_uses_generic_even_when_a_dedicated_provider_exists() =>
        Assert.IsType<GenericProvider>(_r.Resolve(Env.MigratedDraft("DE")));
}
