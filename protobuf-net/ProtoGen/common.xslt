<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <!--
  <xsl:template name="capitalizeFirst">
    <xsl:param name="value"/>
    <xsl:value-of select="translate(substring($value,1,1),$alpha,$ALPHA)"/>
    <xsl:value-of select="substring($value,2)"/>
  </xsl:template>
  -->
  <xsl:template match="*">
    <xsl:message terminate="yes">
      Node not handled: <xsl:for-each select="ancestor-or-self::*">/<xsl:value-of select="name()"/></xsl:for-each>
      <xsl:for-each select="*">
        ; <xsl:value-of select="concat(name(),'=',.)"/>
      </xsl:for-each>
    </xsl:message>
  </xsl:template>
  <xsl:param name="fixCase"/>
  <xsl:variable name="optionFixCase" select="$fixCase='true'"/>
  
  <xsl:template name="escapeKeyword">
    <xsl:param name="value"/>
    <xsl:value-of select="$value"/>
  </xsl:template>
  
  <xsl:template name="toCamelCase">
    <xsl:param name="value"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:param name="keepDelimiter" select="false()"/>
    <xsl:variable name="segment" select="substring-before($value, $delimiter)"/>
    <xsl:choose>
      <xsl:when test="$segment != ''">
        <xsl:value-of select="$segment"/><xsl:if test="$keepDelimiter"><xsl:value-of select="$delimiter"/></xsl:if>
        <xsl:call-template name="toPascalCase">
          <xsl:with-param name="value" select="substring-after($value, $delimiter)"/>
          <xsl:with-param name="delimiter" select="$delimiter"/>
          <xsl:with-param name="keepDelimiter" select="$keepDelimiter"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$value"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:variable name="alpha" select="'abcdefghijklmnopqrstuvwxyz'"/>
  <xsl:variable name="ALPHA" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>

  <xsl:template name="toPascalCase">
    <xsl:param name="value"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:param name="keepDelimiter" select="false()"/>
    <xsl:if test="$value != ''">
      <xsl:variable name="segment" select="substring-before($value, $delimiter)"/>
      <xsl:choose>
        <xsl:when test="$segment != ''">
          <xsl:value-of select="translate(substring($segment,1,1),$alpha,$ALPHA)"/><xsl:value-of select="substring($segment,2)"/><xsl:if test="$keepDelimiter"><xsl:value-of select="$delimiter"/></xsl:if>
          <xsl:call-template name="toPascalCase">
            <xsl:with-param name="value" select="substring-after($value, $delimiter)"/>
            <xsl:with-param name="delimiter" select="$delimiter"/>
            <xsl:with-param name="keepDelimiter" select="$keepDelimiter"/>
          </xsl:call-template>    
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="translate(substring($value,1,1),$alpha,$ALPHA)"/><xsl:value-of select="substring($value,2)"/>
        </xsl:otherwise>
      </xsl:choose>      
    </xsl:if>
  </xsl:template>
    <xsl:template name="pascal">
    <xsl:param name="value" select="name"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:call-template name="escapeKeyword">
      <xsl:with-param name="value"><xsl:choose>
      <xsl:when test="$optionFixCase"><xsl:variable name="dotted"><xsl:call-template name="toPascalCase">
          <xsl:with-param name="value" select="$value"/>
          <xsl:with-param name="delimiter" select="'.'"/>
          <xsl:with-param name="keepDelimiter" select="true()"/>
        </xsl:call-template></xsl:variable><xsl:call-template name="toPascalCase">
          <xsl:with-param name="value" select="$dotted"/>
          <xsl:with-param name="delimiter" select="$delimiter"/>
        </xsl:call-template></xsl:when>
      <xsl:otherwise><xsl:value-of select="$value"/></xsl:otherwise>
    </xsl:choose></xsl:with-param></xsl:call-template>
  </xsl:template>
  
  <xsl:template name="PickNamespace"><xsl:param name="defaultNamespace"/><xsl:choose>
    <xsl:when test="package"><xsl:call-template name="pascal">
      <xsl:with-param name="value" select="package"/>
    </xsl:call-template></xsl:when>
    <xsl:when test="$defaultNamespace"><xsl:value-of select="$defaultNamespace"/></xsl:when>
    <xsl:otherwise><xsl:variable name="trimmedName"><xsl:choose>
      <xsl:when test="substring(name,string-length(name)-5,6)='.proto'"><xsl:value-of select="substring(name,1,string-length(name)-6)"/></xsl:when>
      <xsl:otherwise><xsl:value-of select="name"/></xsl:otherwise>  
    </xsl:choose></xsl:variable><xsl:call-template name="pascal">
      <xsl:with-param name="value" select="$trimmedName"/>
    </xsl:call-template></xsl:otherwise>    
  </xsl:choose></xsl:template>

  <xsl:template match="FieldDescriptorProto/options"/>
  <xsl:template match="FileDescriptorProto/options"/>
  <xsl:template match="DescriptorProto/options"/>
  <xsl:template match="EnumValueDescriptorProto/options"/>
  <xsl:template match="EnumDescriptorProto/options"/>
  <xsl:template match="ServiceDescriptorProto/options"/>
  <xsl:template match="MethodDescriptorProto/options"/>
</xsl:stylesheet>
