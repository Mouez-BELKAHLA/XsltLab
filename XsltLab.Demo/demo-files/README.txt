HOW TO TRY THE MECHANISM (run from the XsltLab folder)

1. dotnet run --project XsltLab.Demo
   First run creates draft.docx, prints three drafts, saves a baseline of the filed XML.

2. Open demo-files/draft.docx in Word. Add a line:   Very big correction: {{IsVeryBig}}
   Save, run again.  -> "DRAFT FAILED: No value for placeholder IsVeryBig"
   (same as today: the content team cannot invent a placeholder)

3. Open demo-files/expressions.xslt and add, before </values>:

      <value name="IsVeryBig">
        <xsl:choose>
          <xsl:when test="IsCorrection = 'Yes' and number(Box81) &gt; 10000">Yes</xsl:when>
          <xsl:otherwise>No</xsl:otherwise>
        </xsl:choose>
      </value>

   Run again.  -> the draft shows Yes/No. No C# changed, no build of the Core library.
   The filing lines still say YES (unchanged).

4. Try to break things:
   - add <value name="Box81">1</value>  -> shadowing is refused
   - edit filing.xslt                   -> "matches the baseline" says NO (proof works both ways)
   - delete demo-files/out/filing-baseline.txt to take a new baseline.
