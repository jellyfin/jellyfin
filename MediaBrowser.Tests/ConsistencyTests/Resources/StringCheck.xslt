<?xml version="1.0" encoding="utf-8"?>
<!-- DWXMLSource="StringCheckSample.xml" -->
<!DOCTYPE xsl:stylesheet  [
	<!ENTITY nbsp   "&#160;">
	<!ENTITY copy   "&#169;">
	<!ENTITY reg    "&#174;">
	<!ENTITY trade  "&#8482;">
	<!ENTITY mdash  "&#8212;">
	<!ENTITY ldquo  "&#8220;">
	<!ENTITY rdquo  "&#8221;"> 
	<!ENTITY pound  "&#163;">
	<!ENTITY yen    "&#165;">
	<!ENTITY euro   "&#8364;">
]>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"/>
  <xsl:template match="/">
    <html xmlns="http://www.w3.org/1999/xhtml">
    <head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>
      <xsl:value-of select="StringUsages/@ReportTitle"/>
    </title>
    <style>
body {
	background: #F3F3F4;
	color: #1E1E1F;
	font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
	padding: 0;
	margin: 0;
}
h1 {
	padding: 10px 0px 10px 10px;
	font-size: 21pt;
	background-color: #E2E2E2;
	border-bottom: 1px #C1C1C2 solid;
	color: #201F20;
	margin: 0;
	font-weight: normal;
}
h2 {
	font-size: 18pt;
	font-weight: normal;
	padding: 15px 0 5px 0;
	margin: 0;
}
h3 {
	font-weight: normal;
	font-size: 15pt;
	margin: 0;
	padding: 15px 0 5px 0;
	background-color: transparent;
}
/* Color all hyperlinks one color */
a {
	color: #1382CE;
}
/* Table styles */ 
table {
	border-spacing: 0 0;
	border-collapse: collapse;
	font-size: 10pt;
}
table th {
	background: #E7E7E8;
	text-align: left;
	text-decoration: none;
	font-weight: normal;
	padding: 3px 6px 3px 6px;
	border: 1px solid #CBCBCB;
}
table td {
	vertical-align: top;
	padding: 3px 6px 5px 5px;
	margin: 0px;
	border: 1px solid #CBCBCB;
	background: #F7F7F8;
}
/* Local link is a style for hyperlinks that link to file:/// content, there are lots so color them as 'normal' text until the user mouse overs */
.localLink {
	color: #1E1E1F;
	background: #EEEEED;
	text-decoration: none;
}
.localLink:hover {
	color: #1382CE;
	background: #FFFF99;
	text-decoration: none;
}
.baseCell {
	width: 100%;
	color: #427A9F;
}
.stringCell {
	display: table;
}
.tokenCell {
	white-space: nowrap;
}
.occurrence {
	padding-left: 40px;
}
.block {
	display: table-cell;
}
/* Padding around the content after the h1 */ 
#content {
	padding: 0px 12px 12px 12px;
}
#messages table {
	width: 97%;
}
    </style>
    </head>
    <body>
    <h1>
      <xsl:value-of select="StringUsages/@ReportTitle"/>
    </h1>
    <div id="content">
      <h2>Strings</h2>
      <div id="messages">
        <table>
          <tbody>
            <xsl:for-each select="StringUsages/Dictionary">
              <tr>
                <th class="baseCell"> <div class="stringCell">
                    <div class="block tokenCell"><strong><xsl:value-of select="@Token"/></strong>: "</div>
                    <div class="block"><xsl:value-of select="@Text"/>"</div>
                  </div></th>
              </tr>
              <xsl:for-each select="Occurence">
                <xsl:variable name="hyperlink"><xsl:value-of select="@FullPath" /></xsl:variable>
                <tr>
                  <td class="baseCell occurrence"><a href="{@FullPath}"><xsl:value-of select="@FileName"/>:<xsl:value-of select="@LineNumber"/></a></td>
                </tr>
              </xsl:for-each>
            </xsl:for-each>
          </tbody>
        </table>
      </div>
    </div>
    </body>
    </html>
  </xsl:template>
</xsl:stylesheet>