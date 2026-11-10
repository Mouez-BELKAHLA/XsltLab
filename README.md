# XsltLab: template expressions for draft documents

A small, runnable prototype for the design task "Template expressions for draft documents".

**Idea:** the content team already writes XSLT. Let them write a second, small XSLT for the *draft*
(`expressions.xslt`). It derives draft-only values (`IsBigCorrection`, `VatNumberNoPrefix`, a tick box).
Its output is merged into a **copy** of the dictionary before the unchanged Word renderer runs.
The filing path never sees it, so the filed XML stays byte-for-byte the same.

```
Filing path (untouched):  generic provider -> dictionary -> source XML -> filing.xslt -> tax office XML

Draft path (* = new):     generic provider -> dictionary -> source XML
                                -> expressions.xslt *  (written by the content team)
                                -> merge into a COPY of the dictionary *
                                -> shared Word renderer (unchanged) -> draft (.docx)
```

## Projects (open `XsltLab.sln`)

| Project | What it is | How to run it |
|---|---|---|
| `XsltLab.Core` | The mechanism: providers, `ProviderResolver`, XSLT engine, expressions evaluator, `.docx` renderer. A library, it does not run on its own. | Used by the other two |
| `XsltLab.Tests` | xUnit tests. Each test name starts with the design question it supports (Q1..Q7). | **Visual Studio:** Test > Run All Tests (Test Explorer). **Terminal:** `dotnet test --logger "console;verbosity=detailed"` or `run-tests.cmd` |
| `XsltLab.Demo` | A console demo working on real files in `XsltLab.Demo/demo-files`. | **Visual Studio:** set `XsltLab.Demo` as startup project, press F5. **Terminal:** `dotnet run --project XsltLab.Demo` |

Requires the .NET 9 SDK. Only the Demo appears next to the green Start button because only a program can be started
there; tests always run from Test Explorer.

## Try the whole mechanism in 2 minutes (no C# changes)

1. Run the Demo once. It creates `demo-files/draft.docx` and prints three drafts
   (big correction, small correction, no correction), and saves a baseline hash of the filed XML.
2. Open `demo-files/draft.docx` in Word and add a line: `Very big correction (over 10000)? {{IsVeryBig}}`. Save, close Word, run again.
   Result: `DRAFT FAILED: No value for placeholder IsVeryBig`. This is today's problem: nobody can add a placeholder without an engineer.
3. Open `demo-files/expressions.xslt` and add this just above `</values>`:

   ```xml
   <value name="IsVeryBig">
     <xsl:choose>
       <xsl:when test="IsCorrection = 'Yes' and number(Box81) &gt; 10000">Yes</xsl:when>
       <xsl:otherwise>No</xsl:otherwise>
     </xsl:choose>
   </value>
   ```
   Run again. The draft shows Yes/No, and every scenario still reports `filing bytes unchanged: YES`.
4. Try to break it: add `<value name="Box81">1</value>` (shadowing is refused);
   edit `filing.xslt` (the baseline check says NO, so the proof works both ways).

Output files are written to `demo-files/out/`; the `.docx` files open in Word.

## What the tests prove

| Prefix | Question | Examples |
|---|---|---|
| Q1 | The feature works | big-correction rule incl. the 1000 boundary, tick box, VAT number without first character |
| Q2 | The filing must not change | identical bytes before/after drafts; independent of dedicated providers; only the 4 filed fields; provider dictionary not mutated |
| Q3 | Where the routing decision lives | one `ProviderResolver`: filing -> generic; draft with expressions -> generic; draft without -> dedicated if any |
| Q5 | Validation | placeholder with no value fails and names itself; shadowing refused; output must be `<values>` |
| Q6 | Limits on author-written code | `document()` and embedded scripts disabled |
| Q7 | Retiring a dedicated provider | the migrated draft renders exactly like the hardcoded provider did (`DeProvider`) |
| Template lookup | Which template row is used | approved only, valid on the date, newest wins |

`DeProvider` and `FrProvider` stand in for the ~25 dedicated country providers (the old way: a conditional is a line of C#).
They are used only by the tests, to show routing and migration parity. The Demo uses the generic provider only.

## Known limits of this prototype

- No golden file captured from real production output. The before/after comparison proves the feature does not change the filing;
  a captured production baseline would pin it against today's real output.
- No timeout or output-size limit on the XSLT run yet (design question 6: deadline well under the 3-minute queue timeout,
  permanent failure instead of retries, bounded stack and a watchdog).
- The renderer is a stand-in: it handles placeholders Word splits across runs, but not headers, footers or text boxes,
  and it produces `.docx`, not PDF.
- Flat string values only; no transaction lists in expressions.
