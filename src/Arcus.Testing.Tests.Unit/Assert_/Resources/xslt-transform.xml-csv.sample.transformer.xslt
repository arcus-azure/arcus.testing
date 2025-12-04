<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:math="http://www.w3.org/2005/xpath-functions/math"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	exclude-result-prefixes="xs math" version="3.0">
	<xsl:output method="text" indent="yes" omit-xml-declaration="yes" />

	<xsl:param name="separator" select="';'"/>

	<xsl:template match="/">
		<xsl:apply-templates select="Data"/>
	</xsl:template>

	<xsl:template match="Data">
		<xsl:value-of select="concat(Field1, $separator, Field2, $separator, Field3)"/>
	</xsl:template>

</xsl:stylesheet>