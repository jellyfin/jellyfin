<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="xsl msxsl"
>
  <xsl:param name="help"/>
  <xsl:output method="xml" indent="yes"/>


  <xsl:template match="/*">
    <xsl:if test="$help='true'">
      <xsl:message terminate="yes">
        Xml template for protobuf-net.
        
        This template writes the proto descriptor as xml.
        No options available.
      </xsl:message>
    </xsl:if>
    <xsl:call-template name="main"/>
  </xsl:template>
  
    <xsl:template match="@* | node()" name="main">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
