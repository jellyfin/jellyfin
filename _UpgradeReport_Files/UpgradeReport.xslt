<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl='urn:schemas-microsoft-com:xslt'>
  <xsl:output omit-xml-declaration="yes" /> 

  <!-- Keys -->
  <xsl:key name="ProjectKey" match="Event" use="@Project" />

  <!-- String split template -->
  <xsl:template name="SplitString">
    <xsl:param name="source"      select="''" />
    <xsl:param name="separator" select="','" />
    <xsl:if test="not($source = '' or $separator = '')">
      <xsl:variable name="head" select="substring-before(concat($source, $separator), $separator)" />
      <xsl:variable name="tail" select="substring-after($source, $separator)" />
      <part>
        <xsl:value-of select="$head"/>
      </part>
      <xsl:call-template name="SplitString">
        <xsl:with-param name="source" select="$tail" />
        <xsl:with-param name="separator" select="$separator" />
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <!-- Intermediate Templates -->
  <xsl:template match="UpgradeReport" mode="ProjectOverviewXML">
    <Projects>
      <xsl:for-each select="Event[generate-id(.) = generate-id(key('ProjectKey', @Project))]">
        <Project>
          <xsl:variable name="pNode" select="current()" />
          <xsl:variable name="errorCount" select="count(../Event[@Project = current()/@Project and @ErrorLevel=2])" />
          <xsl:variable name="warningCount" select="count(../Event[@Project = current()/@Project and @ErrorLevel=1])" />
          <xsl:variable name="messageCount" select="count(../Event[@Project = current()/@Project and @ErrorLevel=0])" />
          <xsl:variable name="pathSplitSeparator">
            <xsl:text>\</xsl:text>
          </xsl:variable>
          <xsl:variable name="projectName">
            <xsl:choose>
              <xsl:when test="@Project = ''">Solution</xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="@Project"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:attribute name="IsSolution">
            <xsl:value-of select="@Project = ''"/>
          </xsl:attribute>
          <xsl:attribute name="Project">
            <xsl:value-of select="$projectName"/>
          </xsl:attribute>
          <xsl:attribute name="ProjectDisplayName">
            <!-- Sometimes it is possible to have project name set to a path over a real project name,
                 we split the string on '\' and if we end up with >1 part in the resulting tokens set
                 we format the ProjectDisplayName as ..\prior\last -->
            <xsl:variable name="pathTokens">
              <xsl:call-template name="SplitString">
                <xsl:with-param name="source" select="$projectName" />
                <xsl:with-param name="separator" select="$pathSplitSeparator" />
              </xsl:call-template>
            </xsl:variable>

            <xsl:choose>
              <xsl:when test="count(msxsl:node-set($pathTokens)/part) > 1">
                <xsl:value-of select="concat('..', $pathSplitSeparator, msxsl:node-set($pathTokens)/part[last() - 1], $pathSplitSeparator, msxsl:node-set($pathTokens)/part[last()])"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="$projectName"/>
              </xsl:otherwise>
            </xsl:choose>

          </xsl:attribute>
          <xsl:attribute name="ProjectSafeName">
            <xsl:value-of select="translate($projectName, '\', '-')"/>
          </xsl:attribute>
          <xsl:attribute name="Solution">
            <xsl:value-of select="/UpgradeReport/Properties/Property[@Name='Solution']/@Value"/>
          </xsl:attribute>
          <xsl:attribute name="Source">
            <xsl:value-of select="@Source"/>
          </xsl:attribute>
          <xsl:attribute name="Status">
            <xsl:choose>
              <xsl:when test="$errorCount > 0">Error</xsl:when>
              <xsl:when test="$warningCount > 0">Warning</xsl:when>
              <xsl:otherwise>Success</xsl:otherwise>
            </xsl:choose>
          </xsl:attribute>
          <xsl:attribute name="ErrorCount">
            <xsl:value-of select="$errorCount" />
          </xsl:attribute>
          <xsl:attribute name="WarningCount">
            <xsl:value-of select="$warningCount" />
          </xsl:attribute>
          <xsl:attribute name="MessageCount">
            <xsl:value-of select="$messageCount" />
          </xsl:attribute>
          <xsl:attribute name="TotalCount">
            <xsl:value-of select="$errorCount + $warningCount + $messageCount"/>
          </xsl:attribute>
          <xsl:for-each select="../Event[@Project = $pNode/@Project and @ErrorLevel=3]">
            <ConversionStatus>
              <xsl:value-of select="@Description"/>
            </ConversionStatus>
          </xsl:for-each>
          <Messages>
            <xsl:for-each select="../Event[@Project = $pNode/@Project and @ErrorLevel&lt;3]">
              <Message>
                <xsl:attribute name="Level">
                  <xsl:value-of select="@ErrorLevel" />
                </xsl:attribute>
                <xsl:attribute name="Status">
                  <xsl:choose>
                    <xsl:when test="@ErrorLevel = 0">Message</xsl:when>
                    <xsl:when test="@ErrorLevel = 1">Warning</xsl:when>
                    <xsl:when test="@ErrorLevel = 2">Error</xsl:when>
                    <xsl:otherwise>Message</xsl:otherwise>
                  </xsl:choose>
                </xsl:attribute>
                <xsl:attribute name="Source">
                  <xsl:value-of select="@Source"/>
                </xsl:attribute>
                <xsl:attribute name="Message">
                  <xsl:value-of select="@Description"/>
                </xsl:attribute>
              </Message>
            </xsl:for-each>
          </Messages>
        </Project>
      </xsl:for-each>
    </Projects>
  </xsl:template>



  <!-- Project Overview template -->
  <xsl:template match="Projects" mode="ProjectOverview">

    <table>
      <tr>
        <th></th>
        <th _locID="ProjectTableHeader">Project</th>
        <th _locID="PathTableHeader">Path</th>
        <th _locID="ErrorsTableHeader">Errors</th>
        <th _locID="WarningsTableHeader">Warnings</th>
        <th _locID="MessagesTableHeader">Messages</th>
      </tr>

        <xsl:for-each select="Project">

          <xsl:sort select="@ErrorCount" order="descending" />
          <xsl:sort select="@WarningCount" order="descending" />
          <!-- Always make solution last within a group -->
          <xsl:sort select="@IsSolution" order="ascending" />
          <xsl:sort select="@ProjectSafeName" order="ascending" />

          <tr>
            <td>
              <img width="16" height="16">
                <xsl:attribute name="src">
                  <xsl:choose>
                    <xsl:when test="@Status = 'Error'">_UpgradeReport_Files\UpgradeReport_Error.png</xsl:when>
                    <xsl:when test="@Status = 'Warning'">_UpgradeReport_Files\UpgradeReport_Warning.png</xsl:when>
                    <xsl:when test="@Status = 'Success'">_UpgradeReport_Files\UpgradeReport_Success.png</xsl:when>
                  </xsl:choose>
                </xsl:attribute>
                <xsl:attribute name="alt">
                  <xsl:value-of select="@Status" />
                </xsl:attribute>
              </img>
            </td>
            <td>
              <strong>
                <a>
                  <xsl:attribute name="href">
                    <xsl:value-of select="concat('#', @ProjectSafeName)"/>
                  </xsl:attribute>
                  <xsl:value-of select="@ProjectDisplayName" />
                </a>
              </strong>
            </td>
            <td>
              <xsl:value-of select="@Source" />
            </td>
            <td class="textCentered">
              <a>
                <xsl:if test="@ErrorCount > 0">
                  <xsl:attribute name="href">
                    <xsl:value-of select="concat('#', @ProjectSafeName, 'Error')"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:value-of select="@ErrorCount" />
              </a>
            </td>
            <td class="textCentered">
              <a>
                <xsl:if test="@WarningCount > 0">
                  <xsl:attribute name="href">
                    <xsl:value-of select="concat('#', @ProjectSafeName, 'Warning')"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:value-of select="@WarningCount" />
              </a>
            </td>
            <td class="textCentered">
              <a href="#">
                <xsl:if test="@MessageCount > 0">
                  <xsl:attribute name="onclick">
                    <xsl:variable name="apos">
                      <xsl:text>'</xsl:text>
                    </xsl:variable>
                    <xsl:variable name="JS" select="concat('ScrollToFirstVisibleMessage(', $apos, @ProjectSafeName, $apos, ')')" />
                    <xsl:value-of select="concat($JS, '; return false;')"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:value-of select="@MessageCount" />
              </a>
            </td>
          </tr>
        </xsl:for-each>
    </table>
  </xsl:template>

  <!-- Show messages row -->
  <xsl:template match="Project" mode="ProjectShowMessages">
    <tr>
      <xsl:attribute name="name">
        <xsl:value-of select="concat('MessageRowHeaderShow', @ProjectSafeName)"/>
      </xsl:attribute>
      <td>
        <img width="16" height="16" src="_UpgradeReport_Files\UpgradeReport_Information.png" />
      </td>
      <td class="messageCell">
        <xsl:variable name="apos">
          <xsl:text>'</xsl:text>
        </xsl:variable>
        <xsl:variable name="toggleRowsJS" select="concat('ToggleMessageVisibility(', $apos, @ProjectSafeName, $apos, ')')" />

        <a _locID="ShowAdditionalMessages" href="#">
          <xsl:attribute name="name">
            <xsl:value-of select="concat(@ProjectSafeName, 'Message')"/>
          </xsl:attribute>
          <xsl:attribute name="onclick">
            <xsl:value-of select="concat($toggleRowsJS, '; return false;')"/>
          </xsl:attribute>
          Show <xsl:value-of select="@MessageCount" /> additional messages
        </a>
      </td>
    </tr>
  </xsl:template>

  <!-- Hide messages row -->
  <xsl:template match="Project" mode="ProjectHideMessages">
    <tr style="display: none">
      <xsl:attribute name="name">
        <xsl:value-of select="concat('MessageRowHeaderHide', @ProjectSafeName)"/>
      </xsl:attribute>
      <td>
        <img width="16" height="16" src="_UpgradeReport_Files\UpgradeReport_Information.png" />
      </td>
      <td class="messageCell">
        <xsl:variable name="apos">
          <xsl:text>'</xsl:text>
        </xsl:variable>
        <xsl:variable name="toggleRowsJS" select="concat('ToggleMessageVisibility(', $apos, @ProjectSafeName, $apos, ')')" />

        <a _locID="HideAdditionalMessages" href="#">
          <xsl:attribute name="name">
            <xsl:value-of select="concat(@ProjectSafeName, 'Message')"/>
          </xsl:attribute>
          <xsl:attribute name="onclick">
            <xsl:value-of select="concat($toggleRowsJS, '; return false;')"/>
          </xsl:attribute>
          Hide <xsl:value-of select="@MessageCount" /> additional messages
        </a>
      </td>
    </tr>
  </xsl:template>

  <!-- Message row templates -->
  <xsl:template match="Message">
    <tr>
      <xsl:attribute name="name">
        <xsl:value-of select="concat(@Status, 'RowClass', ../../@ProjectSafeName)"/>
      </xsl:attribute>

      <xsl:if test="@Level = 0">
        <xsl:attribute name="style">display: none</xsl:attribute>
      </xsl:if>
      <td>
        <a>
          <xsl:attribute name="name">
            <xsl:value-of select="concat(../../@ProjectSafeName, @Status)"/>
          </xsl:attribute>
        </a>
        <img width="16" height="16">
          <xsl:attribute name="src">
            <xsl:choose>
              <xsl:when test="@Status = 'Error'">_UpgradeReport_Files\UpgradeReport_Error.png</xsl:when>
              <xsl:when test="@Status = 'Warning'">_UpgradeReport_Files\UpgradeReport_Warning.png</xsl:when>
              <xsl:when test="@Status = 'Message'">_UpgradeReport_Files\UpgradeReport_Information.png</xsl:when>
            </xsl:choose>
          </xsl:attribute>
          <xsl:attribute name="alt">
            <xsl:value-of select="@Status" />
          </xsl:attribute>
        </img>
      </td>
      <td class="messageCell">
        <strong>
          <xsl:value-of select="@Source"/>:
        </strong>
        <span>
          <xsl:value-of select="@Message"/>
        </span>
      </td>
    </tr>
  </xsl:template>

  <!-- Project Details Template -->
  <xsl:template match="Projects" mode="ProjectDetails">

    <xsl:for-each select="Project">
      <xsl:sort select="@ErrorCount" order="descending" />
      <xsl:sort select="@WarningCount" order="descending" />
      <!-- Always make solution last within a group -->
      <xsl:sort select="@IsSolution" order="ascending" />
      <xsl:sort select="@ProjectSafeName" order="ascending" />

      <a>
        <xsl:attribute name="name">
          <xsl:value-of select="@ProjectSafeName"/>
        </xsl:attribute>
      </a>
      <h3>
        <xsl:value-of select="@ProjectDisplayName"/>
      </h3>

      <table>
        <tr>
          <xsl:attribute name="id">
            <xsl:value-of select="concat(@ProjectSafeName, 'HeaderRow')"/>
          </xsl:attribute>
          <th></th>
          <th class="messageCell" _locID="MessageTableHeader">Message</th>
        </tr>

        <!-- Errors and warnings -->
        <xsl:for-each select="Messages/Message[@Level &gt; 0]">
          <xsl:sort select="@Level" order="descending" />
          <xsl:apply-templates select="." />
        </xsl:for-each>

        <xsl:if test="@MessageCount > 0">
          <xsl:apply-templates select="." mode="ProjectShowMessages" />
        </xsl:if>

        <!-- Messages -->
        <xsl:for-each select="Messages/Message[@Level = 0]">
          <xsl:apply-templates select="." />
        </xsl:for-each>

        <xsl:choose>
          <!-- Additional row as a 'place holder' for 'Show/Hide' additional messages -->
          <xsl:when test="@MessageCount > 0">
            <xsl:apply-templates select="." mode="ProjectHideMessages" />
          </xsl:when>
          <!-- No messages at all, show blank row -->
          <xsl:when test="@TotalCount = 0">
            <tr>
              <td>
                <img width="16" height="16" src="_UpgradeReport_Files\UpgradeReport_Information.png" />
              </td>
              <td class="messageCell" _locID="NoMessagesRow">
                <xsl:value-of select="@ProjectDisplayName" /> logged no messages.
              </td>
            </tr>
          </xsl:when>
        </xsl:choose>
      </table>
    </xsl:for-each>
  </xsl:template>

  <!-- Document, matches "UpgradeReport" -->
  <xsl:template match="UpgradeReport">
    <!-- Output doc type the 'Mark of the web' which disabled prompting to run JavaScript from local HTML Files in IE --> 
    <!-- NOTE: The whitespace around the 'Mark of the web' is important it must be exact --> 
    <xsl:text disable-output-escaping="yes"><![CDATA[<!DOCTYPE html>
<!-- saved from url=(0014)about:internet -->
]]>
    </xsl:text>
    <html>
      <head>
        <meta content="en-us" http-equiv="Content-Language" />
        <meta content="text/html; charset=utf-16" http-equiv="Content-Type" />
        <link type="text/css" rel="stylesheet" href="_UpgradeReport_Files\UpgradeReport.css" />
        <title _locID="ConversionReport0">
          Migration Report
        </title>

        <script type="text/javascript" language="javascript">
        <xsl:text disable-output-escaping="yes">
          <![CDATA[
          
            // Startup 
            // Hook up the the loaded event for the document/window, to linkify the document content
            var startupFunction = function() { linkifyElement("messages"); };
            
            if(window.attachEvent)
            {
              window.attachEvent('onload', startupFunction);
            }
            else if (window.addEventListener) 
            {
              window.addEventListener('load', startupFunction, false);
            }
            else 
            {
              document.addEventListener('load', startupFunction, false);
            } 
            
            // Toggles the visibility of table rows with the specified name 
            function toggleTableRowsByName(name)
            {
               var allRows = document.getElementsByTagName('tr');
               for (i=0; i < allRows.length; i++)
               {
                  var currentName = allRows[i].getAttribute('name');
                  if(!!currentName && currentName.indexOf(name) == 0)
                  {
                      var isVisible = allRows[i].style.display == ''; 
                      isVisible ? allRows[i].style.display = 'none' : allRows[i].style.display = '';
                  }
               }
            }
            
            function scrollToFirstVisibleRow(name) 
            {
               var allRows = document.getElementsByTagName('tr');
               for (i=0; i < allRows.length; i++)
               {
                  var currentName = allRows[i].getAttribute('name');
                  var isVisible = allRows[i].style.display == ''; 
                  if(!!currentName && currentName.indexOf(name) == 0 && isVisible)
                  {
                     allRows[i].scrollIntoView(true); 
                     return true; 
                  }
               }
               
               return false;
            }
            
            // Linkifies the specified text content, replaces candidate links with html links 
            function linkify(text)
            {
                 if(!text || 0 === text.length)
                 {
                     return text; 
                 }

                 // Find {DriveLetter}:\Something or \\{uncshare}\something strings and replace them with file:/// links
                 // It expects that a path ends in .extension, and that that extension does not have a space within it,
                 // it does this as not to greedily match in the case of "Text C:\foo\file.txt some other text" 
                 var filePath = /([A-z]\:|\\{2}[A-z].+)\\([^<]+)\.([^<\s]+)/gi;
                 
                 // Find http, https and ftp links and replace them with hyper links 
                 var urlLink = /(http|https|ftp)\:\/\/[a-zA-Z0-9\-\.]+(:[a-zA-Z0-9]*)?\/?([a-zA-Z0-9\-\._\?\,\/\\\+&%\$#\=~;\{\}])*/gi;
                 
                 return text.replace(filePath, '<a class="localLink" href="file:///$&">$&</a>')
                            .replace(urlLink, '<a href="$&">$&</a>') ;
            }
            
            // Linkifies the specified element by ID
            function linkifyElement(id)
            {
                var element = document.getElementById(id);
                if(!!element)
                {
                  element.innerHTML = linkify(element.innerHTML); 
                }
            }
            
            function ToggleMessageVisibility(projectName)
            {
              if(!projectName || 0 === projectName.length)
              {
                return; 
              }
              
              toggleTableRowsByName("MessageRowClass" + projectName);
              toggleTableRowsByName('MessageRowHeaderShow' + projectName);
              toggleTableRowsByName('MessageRowHeaderHide' + projectName); 
            }
            
            function ScrollToFirstVisibleMessage(projectName)
            {
              if(!projectName || 0 === projectName.length)
              {
                return; 
              }
              
              // First try the 'Show messages' row
              if(!scrollToFirstVisibleRow('MessageRowHeaderShow' + projectName))
              {
                // Failed to find a visible row for 'Show messages', try an actual message row 
                scrollToFirstVisibleRow('MessageRowClass' + projectName); 
              }
            }
          ]]>
        </xsl:text>
        </script>
      </head>
      <body>
        <h1 _locID="ConversionReport">
          Migration Report - <xsl:value-of select="Properties/Property[@Name='Solution']/@Value"/>
        </h1>

        <div id="content">
          <h2 _locID="OverviewTitle">Overview</h2>
          <xsl:variable name="projectOverview">
            <xsl:apply-templates select="self::node()" mode="ProjectOverviewXML" />
          </xsl:variable>

          <div id="overview">
            <xsl:apply-templates select="msxsl:node-set($projectOverview)/*" mode="ProjectOverview" />
          </div>

          <h2 _locID="SolutionAndProjectsTitle">Solution and projects</h2>

          <div id="messages">
            <xsl:apply-templates select="msxsl:node-set($projectOverview)/*" mode="ProjectDetails" />
          </div>
        </div>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>