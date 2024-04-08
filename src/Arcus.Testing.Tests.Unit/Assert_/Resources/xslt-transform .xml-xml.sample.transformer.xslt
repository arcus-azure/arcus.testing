<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl cs" version="1.0" xmlns:cs="urn:cs">
  <xsl:output omit-xml-declaration="yes" method="xml" version="1.0" />
  <xsl:template match="/">
    <xsl:apply-templates select="/Data" />
  </xsl:template>
  <xsl:template match="/Data">
    <Data>
      <ns0:Node1 xmlns:ns0="dummyNS">
        <xsl:value-of select="/Data/Field1" />
      </ns0:Node1>
    </Data>
  </xsl:template>
</xsl:stylesheet>