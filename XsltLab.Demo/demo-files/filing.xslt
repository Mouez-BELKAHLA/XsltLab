<?xml version="1.0" encoding="utf-8"?>
<!-- FILING template: written by the content team. Produces the XML sent to the tax office. -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" encoding="utf-8"/>
  <xsl:template match="/values">
    <Return>
      <Vat><xsl:value-of select="VatNumber"/></Vat>
      <Period><xsl:value-of select="Period"/></Period>
      <Kz81><xsl:value-of select="Box81"/></Kz81>
      <Kz10><xsl:value-of select="IsCorrection"/></Kz10>
    </Return>
  </xsl:template>
</xsl:stylesheet>
