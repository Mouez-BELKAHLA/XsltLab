<?xml version="1.0" encoding="utf-8"?>
<!-- DRAFT EXPRESSIONS: written by the content team. Only for the draft PDF.
     Reads the same source XML, emits <values><value name="..">..</value></values> -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" encoding="utf-8"/>
  <xsl:template match="/values">
    <values>
      <value name="IsBigCorrection">
        <xsl:choose>
          <xsl:when test="IsCorrection = 'Yes' and number(Box81) &gt; 1000">Yes</xsl:when>
          <xsl:otherwise>No</xsl:otherwise>
        </xsl:choose>
      </value>
      <value name="VatNumberNoPrefix"><xsl:value-of select="substring(VatNumber, 2)"/></value>
      <value name="IsCorrectionBox">
        <xsl:choose>
          <xsl:when test="IsCorrection = 'Yes'">&#9746;</xsl:when>
          <xsl:otherwise>&#9744;</xsl:otherwise>
        </xsl:choose>
      </value>
    </values>
  </xsl:template>
</xsl:stylesheet>
