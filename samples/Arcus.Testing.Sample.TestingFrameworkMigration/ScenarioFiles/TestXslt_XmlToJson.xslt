<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                exclude-result-prefixes="msxsl"
                version="1.0">

  <xsl:output omit-xml-declaration="yes" method="xml" version="1.0" />

  <xsl:template match="/PURCHASEORDER">
    {
    "id" : "<xsl:value-of select="PONUMMER"/>",
    "creationDate" : "<xsl:value-of select="AANMAAKDATUM"/>Z",
    "eta" : "<xsl:value-of select="Line/ETA"/>Z",
    "etd" : "<xsl:value-of select="Line/ETD"/>Z",
    "vendorNumber" : "<xsl:value-of select="LEVERANCIERSNUMMER"/>",
    "vendorName" : "<xsl:value-of select="LEVERANCIERSNAAM"/>",
    "lines": [
    <xsl:for-each select="Line">
      <xsl:if test="position() > 1">,</xsl:if>
      {
      "articleDescription" : "<xsl:value-of select="ARTIKELOMSCHRIJVING"/>",
      "articleNumber" : "<xsl:value-of select="ARTIKELNUMMER"/>",
      "ean" : "<xsl:value-of select="EANCODE"/>",
      "eta" : "<xsl:value-of select="ETA"/>Z",
      "etd" : "<xsl:value-of select="ETD"/>Z",
      "quantityOrdered" : <xsl:if test="STATUS = 7">0</xsl:if><xsl:if test="STATUS != 7"><xsl:value-of select="AANTAL"/></xsl:if>
      }

    </xsl:for-each>
    ]
    }
  </xsl:template>
</xsl:stylesheet>