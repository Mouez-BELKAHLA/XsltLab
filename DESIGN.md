# Design notes: template expressions for draft documents

## Summary

The content team already writes XSLT, so they write a second, small XSLT for the draft. It derives draft-only values such as `IsBigCorrection`, `VatNumberNoPrefix` and a ticked box. Its output is merged into a copy of the dictionary before the unchanged Word renderer runs. The filing path never sees it, so the filed XML stays byte for byte the same.

```text
Filing path (untouched):
  generic provider -> dictionary -> source XML -> filing.xslt -> tax office XML

Draft path (* = new):
  generic provider -> dictionary -> source XML
      -> expressions.xslt *  (written by the content team)
      -> merge into a COPY of the dictionary *
      -> shared Word renderer (unchanged) -> draft PDF
```

## 1. What is reused, what is new, where each piece sits

- **Reused:** the .NET XSLT 1.0 engine, the dictionary-to-XML serialiser, the template row, the admin UI upload and draft-to-approved workflow, the "generate test file" action, and the Word renderer.
- **New:** an optional expressions file on the draft template row (versioned and approved together with the Word file), a small evaluator that runs it and merges the results, and one routing rule (section 3).
- **Field scan:** the existing scan for field names must also read the expressions file, otherwise an expensive field used only there would never be loaded. Names the expressions define are derived, so they are not requested from the provider.
- **Why XSLT:** the team already knows it, it needs no new language, and the shared renderer stays untouched. A mini-language inside Word, C# scripting or a formula table would each need new infrastructure.
- **Accepted limits in v1:** flat scalar values only, no transaction lists. XSLT 1.0 has weak string and number handling, so expressions must see raw invariant values like `12500.00`, not display-formatted ones such as "12 500,00" (which would make `Box81 > 1000` silently false). The renderer does the formatting.

## 2. What must not change, and how to prove it

**Must not change**

- The filing path: the generic provider's dictionary, the source XML, `filing.xslt` and the engine settings.
- The shared renderer's behaviour for the two text-based filing formats.

**Proof**

1. Before any change, capture the real filing XML for every country, template and period as bytes with a SHA-256. After the change, regenerate and compare byte for byte. This runs in CI on every PR.
2. The filing code has no reference to the expressions code, and expressions work on a copy and cannot shadow an existing name.
3. Before enabling the feature, regenerate recent filings in production and compare them with the stored filed hashes.

In the prototype, the filing bytes are identical before and after a draft with expressions is generated, and also identical whether or not a dedicated provider exists. A golden file captured from real production output would be the stronger version of this proof.

## 3. Where the provider decision lives

In one place: a `ProviderResolver` that takes the template row. The three background jobs call it, and their three copies of the rule go away.

- A filing gets the generic provider.
- A draft with an expressions file gets the generic provider.
- A draft without one gets the country's dedicated provider if it exists, otherwise the generic one.

The decision keys off data on the approved template row, not off a flag in job code. Migrating a country is therefore an approval, not a deploy, and a rollback is approving the old row. The rule is first extracted as a behaviour-preserving refactor with tests, then the new branch is added.

**Trade-off:** an explicit "provider" column on the row was rejected because it can drift out of sync with the actual files. The cost is that you cannot force a provider by hand for one country, which can be added if a real need appears.

## 4. Keeping the other two filing formats unaffected

The shared renderer is left alone. It stays "template + dictionary -> document" and knows nothing about expressions. The draft job composes the steps in front of it: provider, then expressions, then renderer. The two text formats keep calling the renderer with the provider's dictionary directly. Tests check that the renderer project has no reference to the evaluator and that the two text formats' output is unchanged.

## 5. Validation on save

**Validated**

- The file is well-formed and compiles with hardened settings: no `document()`, no scripts, no resolver.
- The output contract: the root is `<values>`, each `<value name>` is unique and valid, and none shadows a provider field.
- Every `{{placeholder}}` in the Word file resolves to a provider field or a derived name. This moves the "placeholder has no value" failure from generation time to save time.
- The input fields it reads exist. A typo like "Box 81" would otherwise silently give an empty value.
- A dry run on sample values, within the limits in section 6.

**Deliberately not validated**

- Whether the business rule is right. That is the author's job and the approval step's job, and a person checks the draft anyway.
- Static analysis of XPath or termination, which is undecidable, so runtime limits are used instead.
- Value types and formatting, or Word layout.

## 6. Limits on the workers

- A hard deadline of seconds, far below the 3-minute visibility timeout. Otherwise a long transform makes the message visible to another worker, and after five deliveries a bad expression dead-letters the job and wastes the pool.
- Expression errors are permanent failures: the draft fails with a clear message and the message is acknowledged, so it is not retried. After a timeout, that approved template version is quarantined so redeliveries stop early.
- Caps on input size, output size and number of derived values.
- No capabilities: no `document()`, scripts, extension objects, network or file access.
- Runaway recursion can crash a .NET process, so the transform runs on a dedicated thread with a bounded stack and a watchdog that recycles the worker. The save-time dry run happens in a separate process.
- Compile once per approved version and cache it.

**Trade-off:** a thread plus watchdog cannot cancel a running transform cleanly. A separate sandbox process is stronger but heavier, so it is a later step if needed.

## 7. When the dedicated provider is deleted

Migrating a template does not delete the class. The switch is the template row: approving the migrated template routes that country's draft to generic plus expressions, and approving the old row rolls it back with no deploy. The class is deleted later, once all of these hold:

1. **Parity:** for several periods, the dedicated dictionary and the generic-plus-expressions result match field by field, and the rendered drafts are identical.
2. **All fields covered:** each is either a plain lookup the generic provider can serve or is now expressed in XSLT. A field needing data the generic provider lacks needs a one-time code change, or the class stays in a smaller form.
3. **Old versions:** no approved template, current or historical, that can still be regenerated routes to the class.
4. **Measured:** a metric on the resolver shows zero hits to that class across a full filing cycle.

## Left out of v1

- Transaction lists in expressions
- Shared expression libraries across countries
- A UI editor for expressions
- XSLT 2.0
- An automatic migration tool

These are better built after the first migrations show what the content team actually needs.
