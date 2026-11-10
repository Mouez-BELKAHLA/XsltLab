using XsltLab.Core;

// Run from the XsltLab folder:   dotnet run --project XsltLab.Demo
// Edit demo-files/draft.docx (in Word) and demo-files/expressions.xslt, then run again.
var dir = args.Length > 0 ? args[0] : FindDir();
string P(string f) => Path.Combine(dir, f);
Directory.CreateDirectory(P("out"));

if (!File.Exists(P("draft.docx")))
{
    SampleDocx.Create(P("draft.docx"), new[]
    {
        "Draft VAT return",
        "VAT number: {{VatNumber}}",
        "VAT number without first letter: {{VatNumberNoPrefix}}",
        "Period: {{Period}}",
        "Box 81: {{Box81}}",
        "Is this a correction? {{IsCorrectionBox}} ({{IsCorrection}})",
        "Big correction (over 1000)? {{IsBigCorrection}}",
    });
    Console.WriteLine("Created demo-files/draft.docx - open it in Word, change it, run again.\n");
}

var scenarios = new (string Name, FilingContext Ctx)[]
{
    ("big-correction",   new("DE", "DE123456789", "2026-Q1", "12500.00", true)),
    ("small-correction", new("DE", "DE123456789", "2026-Q1", "500.00", true)),
    ("no-correction",    new("DE", "DE123456789", "2026-Q1", "12500.00", false)),
};

var resolver = new ProviderResolver(new GenericProvider(), Array.Empty<ICountryProvider>());
var filing = new FilingGenerator(resolver);
var drafts = new DraftGenerator(resolver, new DraftExpressionEvaluator(), new DocxRenderer());
var from = new DateOnly(2026, 1, 1);
var filingRow = new TemplateRow(1, TemplateKind.Filing, "DE", from, null, TemplateStatus.Approved, P("filing.xslt"));
var draftRow = new TemplateRow(2, TemplateKind.Draft, "DE", from, null, TemplateStatus.Approved, P("draft.docx"), P("expressions.xslt"));

// Baseline of the filed XML: written on the first run, compared on every later run.
var baselinePath = P("out/filing-baseline.txt");
var baseline = File.Exists(baselinePath)
    ? File.ReadAllLines(baselinePath).Select(l => l.Split(' ')).ToDictionary(a => a[0], a => a[1])
    : new Dictionary<string, string>();
var newBaseline = new List<string>();
var failures = 0;

foreach (var (name, ctx) in scenarios)
{
    Console.WriteLine($"=== {name} (Box81 {ctx.Box81}, correction: {ctx.IsCorrection}) ===");

    var filedBefore = filing.Generate(filingRow, ctx);
    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(filedBefore));

    try
    {
        var outPath = P($"out/draft-{name}.docx");
        drafts.Generate(draftRow, ctx, outPath);
        Console.WriteLine(DocxRenderer.ReadText(outPath));
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine("DRAFT FAILED: " + ex.Message);
    }

    var filedAfter = filing.Generate(filingRow, ctx);
    var same = filedBefore.SequenceEqual(filedAfter);
    Console.WriteLine($"filing bytes unchanged by the draft step: {(same ? "YES" : "NO !!!")}");
    if (!same) failures++;

    if (baseline.TryGetValue(name, out var known))
    {
        var ok = known == hash;
        Console.WriteLine($"filing matches the baseline from the first run: {(ok ? "YES" : "NO !!! (was filing.xslt edited?)")}");
        if (!ok) failures++;
    }
    newBaseline.Add($"{name} {hash}");
    Console.WriteLine();
}

if (baseline.Count == 0)
{
    File.WriteAllLines(baselinePath, newBaseline);
    Console.WriteLine("Baseline of the filed XML saved to demo-files/out/filing-baseline.txt");
}
Console.WriteLine("Draft files are in demo-files/out/ (open them in Word).");
Console.WriteLine(failures == 0 ? "OK" : $"{failures} PROBLEM(S)");
return failures;

static string FindDir()
{
    // Works from the command line AND from Visual Studio (where the working folder is bin/Debug/net9.0):
    // walk up from the exe folder and the current folder until demo-files is found.
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
            foreach (var rel in new[] { Path.Combine("XsltLab.Demo", "demo-files"), "demo-files" })
            {
                var candidate = Path.Combine(d.FullName, rel);
                if (Directory.Exists(candidate)) return candidate;
            }
    throw new DirectoryNotFoundException("Could not find the demo-files folder. Pass its path as an argument.");
}
