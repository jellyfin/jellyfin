<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="xsl msxsl"
>
  <xsl:import href="common.xslt"/>
  <xsl:param name="help"/>
  <xsl:param name="xml"/>
  <xsl:param name="datacontract"/>
  <xsl:param name="binary"/>
  <xsl:param name="protoRpc"/>
  <xsl:param name="observable"/>
  <xsl:param name="preObservable"/>
  <xsl:param name="partialMethods"/>
  <xsl:param name="detectMissing"/>
  <xsl:param name="lightFramework"/>
  <xsl:param name="asynchronous"/>
  <xsl:param name="clientProxy"/>
  <xsl:param name="defaultNamespace"/>
  
  
  <xsl:key name="fieldNames" match="//FieldDescriptorProto" use="name"/>
  
  <xsl:output method="text" indent="no" omit-xml-declaration="yes"/>
  <xsl:variable name="types" select="//EnumDescriptorProto | //DescriptorProto"/>
  <xsl:variable name="optionXml" select="$xml='true'"/>
  <xsl:variable name="optionDataContract" select="$datacontract='true'"/>
  <xsl:variable name="optionBinary" select="$binary='true'"/>
  <xsl:variable name="optionProtoRpc" select="$protoRpc='true'"/>
  <xsl:variable name="optionObservable" select="$observable='true'"/>
  <xsl:variable name="optionPreObservable" select="$preObservable='true'"/>
  <xsl:variable name="optionPartialMethods" select="$partialMethods='true'"/>
  <xsl:variable name="optionDetectMissing" select="$detectMissing='true'"/>
  <xsl:variable name="optionFullFramework" select="not($lightFramework='true')"/>
  <xsl:variable name="optionAsynchronous" select="$asynchronous='true'"/>
  <xsl:variable name="optionClientProxy" select="$clientProxy='true'"/>
  <xsl:variable name="optionFixCase" select="$fixCase='true'"/>  
  
  <xsl:template match="FileDescriptorSet">
    <xsl:if test="$help='true'">
      <xsl:message terminate="yes">
        VisualBasic template for protobuf-net.
        Options:
        General:
          "help" - this page
        Additional serializer support:
          "xml" - enable explicit xml support (XmlSerializer)
          "datacontract" - enable data-contract support (DataContractSerializer; requires .NET 3.0)
          "binary" - enable binary support (BinaryFormatter; not supported on Silverlight)
        Other:
          "protoRpc" - enable proto-rpc client
          "observable" - change notification (observer pattern) support
          "preObservable" - pre-change notification (observer pattern) support (requires .NET 3.5)
          "partialMethods" - provide partial methods for changes (requires C# 3.0)
          "detectMissing" - provide *Specified properties to indicate whether fields are present
          "lightFramework" - omit additional attributes not included in CF/Silverlight
          "asynchronous" - emit asynchronous methods for use with WCF
          "clientProxy" - emit asynchronous client proxy class
      </xsl:message>
    </xsl:if>

    <xsl:if test="$optionXml and $optionDataContract">
      <xsl:message terminate="yes">
        Invalid options: xml and data-contract serialization are mutually exclusive.
      </xsl:message>
    </xsl:if>
      ' Generated from <xsl:value-of select="name"/>
    <xsl:if test="$optionXml">
      ' Option: xml serialization ([XmlType]/[XmlElement]) enabled
    </xsl:if><xsl:if test="$optionDataContract">
      ' Option: data-contract serialization ([DataContract]/[DataMember]) enabled
    </xsl:if><xsl:if test="$optionBinary">
      ' Option: binary serialization (ISerializable) enabled
    </xsl:if><xsl:if test="$optionObservable">
      ' Option: observable (OnPropertyChanged) enabled
    </xsl:if><xsl:if test="$optionPreObservable">
      ' Option: pre-observable (OnPropertyChanging) enabled
    </xsl:if><xsl:if test="$partialMethods">
      ' Option: partial methods (On*Changing/On*Changed) enabled
    </xsl:if><xsl:if test="$detectMissing">
      ' Option: missing-value detection (*Specified/ShouldSerialize*/Reset*) enabled
    </xsl:if><xsl:if test="not($optionFullFramework)">
      ' Option: light framework (CF/Silverlight) enabled
    </xsl:if><xsl:if test="$optionProtoRpc">
      ' Option: proto-rpc enabled
  </xsl:if>
    <xsl:apply-templates select="file/FileDescriptorProto"/>
  </xsl:template>

  <xsl:template match="FileDescriptorProto">
' Generated from: <xsl:value-of select="name"/>
    <xsl:apply-templates select="dependency/string[.!='']"/>
    <xsl:variable name="namespace"><xsl:call-template name="PickNamespace">
      <xsl:with-param name="defaultNamespace" select="$defaultNamespace"/>
        </xsl:call-template>
      </xsl:variable>
    <xsl:if test="string($namespace) != ''">
Namespace <xsl:value-of select="translate($namespace,':-/\','__..')"/>
</xsl:if>
    <xsl:apply-templates select="message_type | enum_type | service"/>
    <xsl:if test="string($namespace) != ''">
End Namespace</xsl:if></xsl:template>
  
<xsl:template match="FileDescriptorProto/dependency/string">
' Note: requires additional types generated from: <xsl:value-of select="."/></xsl:template>

<xsl:template match="DescriptorProto">
<xsl:choose>
<xsl:when test="$optionDataContract">
&lt;Global.System.Serializable, Global.ProtoBuf.ProtoContract(Name:="<xsl:value-of select="name"/>")&gt; _
&lt;Global.System.Runtime.Serialization.DataContract(Name:="<xsl:value-of select="name"/>")&gt; _
</xsl:when>
<xsl:when test="$optionXml">
&lt;Global.System.Serializable, Global.ProtoBuf.ProtoContract(Name:="<xsl:value-of select="name"/>")&gt; _
&lt;Global.System.Xml.Serialization.XmlType(TypeName:="<xsl:value-of select="name"/>")&gt; _
</xsl:when>
<xsl:otherwise>
&lt;Global.System.Serializable, Global.ProtoBuf.ProtoContract(Name:="<xsl:value-of select="name"/>")&gt; _
</xsl:otherwise>
</xsl:choose><!--
-->Public Partial Class <xsl:call-template name="pascal"/>
    implements Global.ProtoBuf.IExtensible<!--
    --><xsl:if test="$optionBinary">, Global.System.Runtime.Serialization.ISerializable</xsl:if><!--
    --><xsl:if test="$optionObservable">, Global.System.ComponentModel.INotifyPropertyChanged</xsl:if><!--
    --><xsl:if test="$optionPreObservable">, Global.System.ComponentModel.INotifyPropertyChanging</xsl:if>
	
	Public Sub New
	End Sub
    <xsl:apply-templates select="*"/><xsl:if test="$optionBinary">
    Protected Sub New(ByVal info As Global.System.Runtime.Serialization.SerializationInfo, ByVal context As Global.System.Runtime.Serialization.StreamingContext)
          MyBase.New()
          Global.ProtoBuf.Serializer.Merge(info, Me)
    End Sub
	  
	Sub GetObjectData(ByVal info As Global.System.Runtime.Serialization.SerializationInfo, ByVal context As Global.System.Runtime.Serialization.StreamingContext) implements Global.System.Runtime.Serialization.ISerializable.GetObjectData
		Global.ProtoBuf.Serializer.Serialize(info, Me)
	End Sub
	
      </xsl:if><xsl:if test="$optionObservable">
    Public Event PropertyChanged As Global.System.ComponentModel.PropertyChangedEventHandler Implements Global.System.ComponentModel.INotifyPropertyChanged.PropertyChanged
    Protected Overridable Sub OnPropertyChanged(ByVal propertyName As String)
        RaiseEvent PropertyChanged(Me, New Global.System.ComponentModel.PropertyChangedEventArgs(propertyName))
    End Sub
    </xsl:if><xsl:if test="$optionPreObservable">
	Public Event PropertyChanging As Global.System.ComponentModel.PropertyChangingEventHandler Implements Global.System.ComponentModel.INotifyPropertyChanging.PropertyChanging
	Protected Overridable Sub OnPropertyChanging(ByVal propertyName As String)
		RaiseEvent PropertyChanging(Me, New Global.System.ComponentModel.PropertyChangingEventArgs(propertyName))
	End Sub
	</xsl:if>
    Private extensionObject As Global.ProtoBuf.IExtension
		Function GetExtensionObject(createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
			Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
		End Function
End Class
  </xsl:template>

  
  
  <xsl:template match="DescriptorProto/name | DescriptorProto/extension_range | DescriptorProto/extension"/>
  
  <xsl:template match="
                FileDescriptorProto/message_type | FileDescriptorProto/enum_type | FileDescriptorProto/service
                | DescriptorProto/field | DescriptorProto/enum_type | DescriptorProto/message_type
                | DescriptorProto/nested_type | EnumDescriptorProto/value | ServiceDescriptorProto/method">
    <xsl:apply-templates select="*"/>
  </xsl:template>

  <xsl:template match="EnumDescriptorProto">
    Public Enum <xsl:call-template name="pascal"/>
      <xsl:apply-templates select="value"/>
    End Enum
  </xsl:template>

  <xsl:template match="EnumValueDescriptorProto">
	  	<xsl:text> 
		</xsl:text>
		<xsl:value-of select="name"/>
		<xsl:text xml:space="preserve"> = </xsl:text><xsl:choose>
	      <xsl:when test="number"><xsl:value-of select="number"/></xsl:when>
	      <xsl:otherwise>0</xsl:otherwise>
	    </xsl:choose><xsl:if test="position()!=last()">
	    </xsl:if>
  </xsl:template>

  <xsl:template match="FieldDescriptorProto" mode="field">
    <xsl:choose>
      <xsl:when test="not(key('fieldNames',concat('_',name)))"><xsl:value-of select="concat('_',name)"/></xsl:when>
      <xsl:when test="not(key('fieldNames',concat(name,'Field')))"><xsl:value-of select="concat(name,'Field')"/></xsl:when>
      <xsl:otherwise><xsl:value-of select="concat('_',generate-id())"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto" mode="format">
    <xsl:choose>
      <xsl:when test="type='TYPE_DOUBLE' or type='TYPE_FLOAT'
                or type='TYPE_FIXED32' or type='TYPE_FIXED64'
                or type='TYPE_SFIXED32' or type='TYPE_SFIXED64'">FixedSize</xsl:when>
      <xsl:when test="type='TYPE_GROUP'">Group</xsl:when>
      <xsl:when test="not(type) or type='TYPE_INT32' or type='TYPE_INT64'
                or type='TYPE_UINT32' or type='TYPE_UINT64'
                or type='TYPE_ENUM'">TwosComplement</xsl:when>
      <xsl:when test="type='TYPE_SINT32' or type='TYPE_SINT64'">ZigZag</xsl:when>
      <xsl:otherwise>Default</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="FieldDescriptorProto" mode="primitiveType">
    <xsl:choose>
      <xsl:when test="not(type)">struct</xsl:when>
      <xsl:when test="type='TYPE_DOUBLE'">struct</xsl:when>
      <xsl:when test="type='TYPE_FLOAT'">struct</xsl:when>
      <xsl:when test="type='TYPE_INT64'">struct</xsl:when>
      <xsl:when test="type='TYPE_UINT64'">struct</xsl:when>
      <xsl:when test="type='TYPE_INT32'">struct</xsl:when>
      <xsl:when test="type='TYPE_FIXED64'">struct</xsl:when>
      <xsl:when test="type='TYPE_FIXED32'">struct</xsl:when>
      <xsl:when test="type='TYPE_BOOL'">struct</xsl:when>
      <xsl:when test="type='TYPE_STRING'">class</xsl:when>
      <xsl:when test="type='TYPE_BYTES'">class</xsl:when>
      <xsl:when test="type='TYPE_UINT32'">struct</xsl:when>
      <xsl:when test="type='TYPE_SFIXED32'">struct</xsl:when>
      <xsl:when test="type='TYPE_SFIXED64'">struct</xsl:when>
      <xsl:when test="type='TYPE_SINT32'">struct</xsl:when>
      <xsl:when test="type='TYPE_SINT64'">struct</xsl:when>
      <xsl:when test="type='TYPE_ENUM'">struct</xsl:when>
      <xsl:when test="type='TYPE_GROUP' or type='TYPE_MESSAGE'">none</xsl:when>
      <xsl:otherwise>
        <xsl:message terminate="yes">
          Field type not implemented: <xsl:value-of select="type"/> (<xsl:value-of select="../../name"/>.<xsl:value-of select="name"/>)
        </xsl:message>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="FieldDescriptorProto" mode="type">
    <xsl:choose>
      <xsl:when test="not(type)">double</xsl:when>
      <xsl:when test="type='TYPE_DOUBLE'">Double</xsl:when>
      <xsl:when test="type='TYPE_FLOAT'">Single</xsl:when>
      <xsl:when test="type='TYPE_INT64'">Long</xsl:when>
      <xsl:when test="type='TYPE_UINT64'">ULong</xsl:when>
      <xsl:when test="type='TYPE_INT32'">Integer</xsl:when>
      <xsl:when test="type='TYPE_FIXED64'">ULong</xsl:when>
      <xsl:when test="type='TYPE_FIXED32'">UInteger</xsl:when>
      <xsl:when test="type='TYPE_BOOL'">Boolean</xsl:when>
      <xsl:when test="type='TYPE_STRING'">String</xsl:when>
      <xsl:when test="type='TYPE_BYTES'">Byte()</xsl:when>
      <xsl:when test="type='TYPE_UINT32'">UInteger</xsl:when>
      <xsl:when test="type='TYPE_SFIXED32'">Integer</xsl:when>
      <xsl:when test="type='TYPE_SFIXED64'">Long</xsl:when>
      <xsl:when test="type='TYPE_SINT32'">Integer</xsl:when>
      <xsl:when test="type='TYPE_SINT64'">Long</xsl:when>
      <xsl:when test="type='TYPE_GROUP' or type='TYPE_MESSAGE' or type='TYPE_ENUM'"><xsl:call-template name="pascal">
          <xsl:with-param name="value" select="substring-after(type_name,'.')"/>
        </xsl:call-template></xsl:when>
      <xsl:otherwise>
        <xsl:message terminate="yes">
          Field type not implemented: <xsl:value-of select="type"/> (<xsl:value-of select="../../name"/>.<xsl:value-of select="name"/>)
        </xsl:message>
      </xsl:otherwise>
    </xsl:choose>
    
  </xsl:template>

  <xsl:template match="FieldDescriptorProto[default_value]" mode="defaultValue">
    <xsl:choose>
      <xsl:when test="type='TYPE_STRING'">"<xsl:value-of select="default_value"/>"</xsl:when>
      <xsl:when test="type='TYPE_ENUM'"><xsl:apply-templates select="." mode="type"/>.<xsl:value-of select="default_value"/></xsl:when>
      <xsl:when test="type='TYPE_BYTES'"> ' <xsl:value-of select="default_value"/></xsl:when>
      <xsl:otherwise>CType(<xsl:value-of select="default_value"/>, <xsl:apply-templates select="." mode="type"/>)</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!--
    We need to find the first enum value given .foo.bar.SomeEnum - but the enum itself
    only knows about SomeEnum; we need to look at all parent DescriptorProto nodes, and
    the FileDescriptorProto for the namespace.
    
    This does an annoying up/down recursion... a bit expensive, but *generally* OK.
    Could perhaps index the last part of the enum name to reduce overhead?
  -->
  <xsl:template name="GetFirstEnumValue">
    <xsl:variable name="hunt" select="type_name"/>
    <xsl:for-each select="//EnumDescriptorProto">
      <xsl:variable name="fullName">
        <xsl:for-each select="ancestor::FileDescriptorProto">.<xsl:value-of select="package"/></xsl:for-each>
        <xsl:for-each select="ancestor::DescriptorProto">.<xsl:value-of select="name"/></xsl:for-each>
        <xsl:value-of select="concat('.',name)"/>
      </xsl:variable>
      <xsl:if test="$fullName=$hunt"><xsl:value-of select="(value/EnumValueDescriptorProto)[1]/name"/></xsl:if>
    </xsl:for-each>
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[not(default_value)]" mode="defaultValue">
    <xsl:choose>
      <xsl:when test="type='TYPE_DOUBLE'">0.0</xsl:when>
      <xsl:when test="type='TYPE_FLOAT'">0.0F</xsl:when>
      <xsl:when test="type='TYPE_INT64'">0L</xsl:when>
      <xsl:when test="type='TYPE_UINT64'">0L</xsl:when>
      <xsl:when test="type='TYPE_INT32'">0</xsl:when>
      <xsl:when test="type='TYPE_FIXED64'">0L</xsl:when>
      <xsl:when test="type='TYPE_FIXED32'">0</xsl:when>
      <xsl:when test="type='TYPE_BOOL'">False</xsl:when>
      <xsl:when test="type='TYPE_STRING'">""</xsl:when>
      <xsl:when test="type='TYPE_BYTES'">Nothing</xsl:when>
      <xsl:when test="type='TYPE_UINT32'">0</xsl:when>
      <xsl:when test="type='TYPE_SFIXED32'">0</xsl:when>
      <xsl:when test="type='TYPE_SFIXED64'">0L</xsl:when>
      <xsl:when test="type='TYPE_SINT32'">0</xsl:when>
      <xsl:when test="type='TYPE_SINT64'">0L</xsl:when>
      <xsl:when test="type='TYPE_MESSAGE'">Nothing</xsl:when>
      <xsl:when test="type='TYPE_ENUM'"><xsl:apply-templates select="." mode="type"/>.<xsl:call-template name="GetFirstEnumValue"/></xsl:when>
      <xsl:otherwise>Nothing</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="FieldDescriptorProto[label='LABEL_OPTIONAL' or not(label)]">
    <xsl:variable name="propType"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    <xsl:variable name="format"><xsl:apply-templates select="." mode="format"/></xsl:variable>
    <xsl:variable name="primitiveType"><xsl:apply-templates select="." mode="primitiveType"/></xsl:variable>
    <xsl:variable name="defaultValue"><xsl:apply-templates select="." mode="defaultValue"/></xsl:variable>
    <xsl:variable name="field"><xsl:apply-templates select="." mode="field"/></xsl:variable>
	<xsl:variable name="specified" select="$optionDetectMissing and ($primitiveType='struct' or $primitiveType='class')"/>
    <xsl:variable name="fieldType"><xsl:if test="$specified and $primitiveType='struct'">Nullable(Of </xsl:if><xsl:value-of select="$propType"/><xsl:if test="$specified and $primitiveType='struct'">)</xsl:if></xsl:variable>

    <xsl:choose>
	  <xsl:when test="substring-after($fieldType, 'google.protobuf.')">
    Private <xsl:value-of select="concat($field,' As ',substring-after($fieldType, 'google.protobuf.'))"/><xsl:if test="not($specified)"> =<xsl:value-of select="$defaultValue"/></xsl:if>
	  </xsl:when>
	  <xsl:otherwise>
    Private <xsl:value-of select="concat($field,' As ',$fieldType)"/><xsl:if test="not($specified)"> =<xsl:value-of select="$defaultValue"/></xsl:if>
	  </xsl:otherwise>
	</xsl:choose>
	<xsl:choose>
		<xsl:when test="not($specified) and $optionXml">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
	<xsl:choose>
		<xsl:when test="substring-after($fieldType, 'google.protobuf.')">
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="substring-after($fieldType, 'google.protobuf.')"/>))&gt; _
		</xsl:when>
		<xsl:otherwise>
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="$fieldType"/>))&gt; _
		</xsl:otherwise>
	</xsl:choose>
    &lt;Global.System.Xml.Serialization.XmlElement("<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>)&gt; _ <!--
		--></xsl:when>
		<xsl:when test="not($specified) and $optionDataContract">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
    <xsl:choose>
		<xsl:when test="substring-after($fieldType, 'google.protobuf.')">
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="substring-after($fieldType, 'google.protobuf.')"/>))&gt; _
		</xsl:when>
		<xsl:otherwise>
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="$fieldType"/>))&gt; _
		</xsl:otherwise>
	</xsl:choose>
    &lt;Global.System.Runtime.Serialization.DataMember(Name:="<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>, IsRequired:=False)&gt; _ <!--
		--></xsl:when>
		<xsl:when test="not($specified)">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _ <!--
    --><xsl:choose>
		<xsl:when test="substring-after($fieldType, 'google.protobuf.')">
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="substring-after($fieldType, 'google.protobuf.')"/>))&gt; _ <!--
		--></xsl:when>
		<xsl:otherwise>
    &lt;Global.System.ComponentModel.DefaultValue(CType(<xsl:value-of select="$defaultValue"/>, <xsl:value-of select="$fieldType"/>))&gt; _ <!--
		--></xsl:otherwise>
	</xsl:choose><!--
		--></xsl:when>
		<xsl:when test="$optionDataContract">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
    &lt;Global.System.Runtime.Serialization.DataMember(Name:="<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>, IsRequired:=False)&gt; _ <!--
		--></xsl:when>
		<xsl:when test="$optionXml">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
    &lt;Global.System.Xml.Serialization.XmlElement("<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>)&gt; _ <!--
		--></xsl:when>
		<xsl:otherwise>
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=False, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _ <!--
		--></xsl:otherwise>
	</xsl:choose><!--
	--><xsl:call-template name="WriteGetSet">
      <xsl:with-param name="fieldType" select="$fieldType"/>
      <xsl:with-param name="propType" select="$propType"/>
      <xsl:with-param name="name"><xsl:call-template name="pascalPropName"/></xsl:with-param>
      <xsl:with-param name="field" select="$field"/>
      <xsl:with-param name="defaultValue" select="$defaultValue"/>
      <xsl:with-param name="specified" select="$specified"/>
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="pascalPropName">
    <xsl:param name="value" select="name"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:variable name="valueUC" select="translate($value,$alpha,$ALPHA)"/>
    <xsl:variable name="finalName">
      <xsl:choose>
        <xsl:when test="$types[translate(name,$alpha,$ALPHA)=$valueUC]"><xsl:value-of select="concat($value,$delimiter,'Property')"/></xsl:when>
        <xsl:otherwise><xsl:value-of select="$value"/></xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:call-template name="pascal">
      <xsl:with-param name="value" select="$finalName"/>
      <xsl:with-param name="delimiter" select="$delimiter"/>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[label='LABEL_REQUIRED']">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    <xsl:variable name="format"><xsl:apply-templates select="." mode="format"/></xsl:variable>
    <xsl:variable name="field"><xsl:apply-templates select="." mode="field"/></xsl:variable>
    Private <xsl:value-of select="concat($field, ' As ', $type)"/>
	<xsl:choose>
		<xsl:when test="$optionDataContract">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=True, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
    &lt;Global.System.Runtime.Serialization.DataMember(Name:="<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>, IsRequired:=True)&gt; _ <!--
		--></xsl:when>
		<xsl:when test="$optionXml">
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=True, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
    &lt;Global.System.Xml.Serialization.XmlElement("<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>)&gt; _ <!--
		--></xsl:when>
		<xsl:otherwise>
    &lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, IsRequired:=True, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _ <!--
		--></xsl:otherwise>
	</xsl:choose><!--
    --><xsl:call-template name="WriteGetSet">
      <xsl:with-param name="fieldType" select="$type"/>
      <xsl:with-param name="propType" select="$type"/>
      <xsl:with-param name="name" select="name"/>
      <xsl:with-param name="field" select="$field"/>
    </xsl:call-template>    
  </xsl:template>

  <xsl:template name="WriteGetSet">
    <xsl:param name="fieldType"/>
    <xsl:param name="propType"/>
    <xsl:param name="name"/>
    <xsl:param name="field"/>
    <xsl:param name="specified" select="false()"/>
    <xsl:param name="defaultValue"/>
	<xsl:variable name="primitiveType"><xsl:apply-templates select="." mode="primitiveType"/></xsl:variable>
	<xsl:choose>
		<xsl:when test="substring-after($fieldType, 'google.protobuf.')">
    Public Property <xsl:value-of select="concat($name,' As ',substring-after($fieldType, 'google.protobuf.'))"/>
		</xsl:when>
		<xsl:otherwise>
    Public Property <xsl:value-of select="concat($name,' As ',$fieldType)"/>
		</xsl:otherwise>
	</xsl:choose>
		Get 
			<xsl:choose>
				<xsl:when test="$specified and $primitiveType='struct'"><!--
					-->Return <xsl:value-of select="$field"/><!--
					--></xsl:when>
				<xsl:when test="$specified">
			If Not <xsl:value-of select="$field"/> Is Nothing Then
				Return <xsl:value-of select="$field"/>
			Else
				Return <xsl:value-of select="$defaultValue"/>
			End If<!--
				--></xsl:when>
				<xsl:otherwise><!--
			-->Return <xsl:value-of select="$field"/><!--
				--></xsl:otherwise>
			</xsl:choose>
		End Get
	<xsl:choose>
		<xsl:when test="substring-after($fieldType, 'google.protobuf.')">
		Set(<xsl:value-of select="concat('value As ',substring-after($fieldType, 'google.protobuf.'))"/>)
		</xsl:when>
		<xsl:otherwise>
		Set(<xsl:value-of select="concat('value As ',$fieldType)"/>)
		</xsl:otherwise>
	</xsl:choose>
			<xsl:if test="$optionPartialMethods">On<xsl:value-of select="$name"/>Changing(value)
			</xsl:if><xsl:if test="$optionPreObservable">OnPropertyChanging("<xsl:value-of select="$name"/>") 
			</xsl:if><xsl:value-of select="$field"/> = value 
			<xsl:if test="$optionObservable">OnPropertyChanged("<xsl:value-of select="$name"/>") </xsl:if><xsl:if test="$optionPartialMethods">On<xsl:value-of select="$name"/>Changed()</xsl:if>
		End Set
	End Property
    <xsl:if test="$optionPartialMethods">
    partial void On<xsl:value-of select="$name"/>Changing(<xsl:value-of select="$propType"/> value);
    partial void On<xsl:value-of select="$name"/>Changed();</xsl:if><xsl:if test="$specified">
    &lt;Global.System.Xml.Serialization.XmlIgnore&gt; _
    <xsl:if test="$optionFullFramework">&lt;Global.System.ComponentModel.Browsable(false)&gt; _ </xsl:if>
	<xsl:choose>
		<xsl:when test="$specified and $primitiveType='struct'">
	Public Property <xsl:value-of select="$name"/>Specified As Boolean
        Get 
            Return <xsl:value-of select="$field"/>.HasValue
        End Get
        Set (ByVal value As Boolean) 
            If Not <xsl:value-of select="$field"/>.HasValue Then
				If value = True then <xsl:value-of select="$field"/> = <xsl:value-of select="$name"/>
			Else
				If value = False then <xsl:value-of select="$field"/> = Nothing
			End If
        End Set
    End Property
		</xsl:when>
		<xsl:otherwise>
	Public Property <xsl:value-of select="$name"/>Specified As Boolean
        Get 
            Return <xsl:value-of select="$field"/> IsNot Nothing
        End Get
        Set (ByVal value As Boolean) 
            If <xsl:value-of select="$field"/> Is Nothing Then
				If value = True then <xsl:value-of select="$field"/> = <xsl:value-of select="$name"/>
			Else
				If value = False then <xsl:value-of select="$field"/> = Nothing
			End If
        End Set
    End Property
		</xsl:otherwise>
	</xsl:choose>
	Private Function ShouldSerialize<xsl:value-of select="$name"/>() As Boolean
		Return <xsl:value-of select="$name"/>Specified 
	End Function
    Private Sub Reset<xsl:value-of select="$name"/>()
		<xsl:value-of select="$name"/>Specified = false
	End Sub
    </xsl:if>
  </xsl:template>
  <xsl:template match="FieldDescriptorProto[label='LABEL_REPEATED']">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    <xsl:variable name="format"><xsl:apply-templates select="." mode="format"/></xsl:variable>
    <xsl:variable name="field"><xsl:apply-templates select="." mode="field"/></xsl:variable>
	<xsl:choose>
		<xsl:when test="substring-after($type, 'google.protobuf.')">
    Private <xsl:if test="not($optionXml)">ReadOnly </xsl:if> <xsl:value-of select="$field"/> as Global.System.Collections.Generic.List(Of <xsl:value-of select="substring-after($type, 'google.protobuf.')" />) = New Global.System.Collections.Generic.List(Of <xsl:value-of select="substring-after($type, 'google.protobuf.')"/>)()
		</xsl:when>
		<xsl:otherwise>
    Private <xsl:if test="not($optionXml)">ReadOnly </xsl:if> <xsl:value-of select="$field"/> as Global.System.Collections.Generic.List(Of <xsl:value-of select="$type" />) = New Global.System.Collections.Generic.List(Of <xsl:value-of select="$type"/>)()
		</xsl:otherwise>
	</xsl:choose>
	<xsl:choose>
		<xsl:when test="$optionDataContract">
	&lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
	&lt;Global.System.Runtime.Serialization.DataMember(Name:="<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>, IsRequired:=False)&gt; _ 
		</xsl:when>
		<xsl:when test="$optionXml">
	&lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
	&lt;Global.System.Xml.Serialization.XmlElement("<xsl:value-of select="name"/>", Order:=<xsl:value-of select="number"/>)&gt; _
		</xsl:when>
		<xsl:otherwise>
	&lt;Global.ProtoBuf.ProtoMember(<xsl:value-of select="number"/>, Name:="<xsl:value-of select="name"/>", DataFormat:=Global.ProtoBuf.DataFormat.<xsl:value-of select="$format"/>)&gt; _
		</xsl:otherwise>
	</xsl:choose><!--
	--><xsl:choose>
		<xsl:when test="substring-after($type, 'google.protobuf.')"><!--
    -->Public <xsl:if test="not($optionXml)">ReadOnly </xsl:if>Property <xsl:value-of select="name"/> As Global.System.Collections.Generic.List(Of <xsl:value-of select="substring-after($type, 'google.protobuf.')" />)
		</xsl:when>
		<xsl:otherwise><!--
    -->Public <xsl:if test="not($optionXml)">ReadOnly </xsl:if>Property <xsl:value-of select="name"/> As Global.System.Collections.Generic.List(Of <xsl:value-of select="$type" />)
		</xsl:otherwise>
	</xsl:choose>
		Get
			Return <xsl:value-of select="$field"/>
		End Get
		<!----><xsl:if test="$optionXml">
		<xsl:choose>
			<xsl:when test="substring-after($type, 'google.protobuf.')">
		Set (value As Global.System.Collections.Generic.List(Of <xsl:value-of select="substring-after($type, 'google.protobuf.')" />))
			</xsl:when>
			<xsl:otherwise>
		Set (value As Global.System.Collections.Generic.List(Of <xsl:value-of select="$type" />))
			</xsl:otherwise>
		</xsl:choose>
			<xsl:value-of select="$field"/> = value 
		End Set
		</xsl:if>
	End Property
  </xsl:template>

  <xsl:template match="ServiceDescriptorProto">
    <xsl:if test="($optionClientProxy or $optionDataContract)">
    &lt;Global.System.ServiceModel.ServiceContract(Name:="<xsl:value-of select="name"/>")&gt; _
    </xsl:if>
    Public Interface I<xsl:value-of select="name"/>
      <xsl:apply-templates select="method"/>
    End Interface
    
    <xsl:if test="$optionProtoRpc">
    Public Class <xsl:value-of select="name"/>Client : Global.ProtoBuf.ServiceModel.RpcClient
      public <xsl:value-of select="name"/>Client() : base(typeof(I<xsl:value-of select="name"/>)) { }

      <xsl:apply-templates select="method/MethodDescriptorProto" mode="protoRpc"/>
    End Class
    </xsl:if>
    <xsl:apply-templates select="." mode="clientProxy"/>
    
  </xsl:template>

  <xsl:template match="MethodDescriptorProto">
    <xsl:if test="$optionDataContract">
    &lt;Global.System.ServiceModel.OperationContract(Name:="<xsl:value-of select="name"/>")&gt; _
    &lt;Global.ProtoBuf.ServiceModel.ProtoBehavior()&gt; _
    </xsl:if>
    <xsl:apply-templates select="output_type"/><xsl:text xml:space="preserve"> </xsl:text><xsl:value-of select="name"/>(<xsl:apply-templates select="input_type"/> request);
    <xsl:if test="$optionAsynchronous and $optionDataContract">
    &lt;Global.System.ServiceModel.OperationContract(AsyncPattern:=True, Name:="<xsl:value-of select="name"/>")&gt; _
    Global.System.IAsyncResult Begin<xsl:value-of select="name"/>(<xsl:apply-templates select="input_type"/> request, Global.System.AsyncCallback callback, object state);
    <xsl:apply-templates select="output_type"/> End<xsl:value-of select="name"/>(Global.System.IAsyncResult ar);
    </xsl:if>
  </xsl:template>

  <xsl:template match="MethodDescriptorProto" mode="protoRpc">
      <xsl:apply-templates select="output_type"/><xsl:text xml:space="preserve"> </xsl:text><xsl:value-of select="name"/>(<xsl:apply-templates select="input_type"/> request)
      {
        return (<xsl:apply-templates select="output_type"/>) Send("<xsl:value-of select="name"/>", request);
      }
  </xsl:template>

  <xsl:template match="MethodDescriptorProto/input_type | MethodDescriptorProto/output_type">
    <xsl:value-of select="substring-after(.,'.')"/>
  </xsl:template>

  <xsl:template match="MethodDescriptorProto" mode="CompleteEvent">
  <xsl:if test="$optionAsynchronous and $optionDataContract">
    Public Class <xsl:value-of select="name"/>CompletedEventArgs : Global.System.ComponentModel.AsyncCompletedEventArgs
        private object[] results;

        public <xsl:value-of select="name"/>CompletedEventArgs(object[] results, Global.System.Exception exception, bool cancelled, object userState)
            : base(exception, cancelled, userState) 
        {
            this.results = results;
        }
        
        public <xsl:apply-templates select="output_type"/> Result
        {
            get { 
                base.RaiseExceptionIfNecessary();
                return (<xsl:apply-templates select="output_type"/>)(this.results[0]); 
            }
        }
    End Class
  </xsl:if>
  </xsl:template>

  <xsl:template match="ServiceDescriptorProto" mode="clientProxy">
  <xsl:if test="$optionAsynchronous and $optionDataContract and $optionClientProxy">
    <xsl:apply-templates select="method/MethodDescriptorProto" mode="CompleteEvent"/>
    
    &lt;Global.System.Diagnostics.DebuggerStepThroughAttribute()&gt; _
    public partial class <xsl:value-of select="name"/>Client : Global.System.ServiceModel.ClientBase&lt;I<xsl:value-of select="name"/>&gt;, I<xsl:value-of select="name"/>
    {

        public <xsl:value-of select="name"/>Client()
        {}
        public <xsl:value-of select="name"/>Client(string endpointConfigurationName) 
            : base(endpointConfigurationName) 
        {}
        public <xsl:value-of select="name"/>Client(string endpointConfigurationName, string remoteAddress) 
            : base(endpointConfigurationName, remoteAddress)
        {}
        public <xsl:value-of select="name"/>Client(string endpointConfigurationName, Global.System.ServiceModel.EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {}
        public <xsl:value-of select="name"/>Client(Global.System.ServiceModel.Channels.Binding binding, Global.System.ServiceModel.EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {}

        <xsl:apply-templates select="method/MethodDescriptorProto" mode="clientProxy"/>
    }  
  </xsl:if>
  </xsl:template>

  <xsl:template match="MethodDescriptorProto" mode="clientProxy">
  <xsl:if test="$optionAsynchronous and $optionDataContract and $optionClientProxy">
        private BeginOperationDelegate onBegin<xsl:value-of select="name"/>Delegate;
        private EndOperationDelegate onEnd<xsl:value-of select="name"/>Delegate;
        private Global.System.Threading.SendOrPostCallback on<xsl:value-of select="name"/>CompletedDelegate;

        public event Global.System.EventHandler&lt;<xsl:value-of select="name"/>CompletedEventArgs&gt; <xsl:value-of select="name"/>Completed;

        public <xsl:apply-templates select="output_type"/><xsl:text xml:space="preserve"> </xsl:text><xsl:value-of select="name"/>(<xsl:apply-templates select="input_type"/> request)
        {
            return base.Channel.<xsl:value-of select="name"/>(request);
        }

        &lt;Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)&gt; _
        public Global.System.IAsyncResult Begin<xsl:value-of select="name"/>(<xsl:apply-templates select="input_type"/> request, Global.System.AsyncCallback callback, object asyncState)
        {
            return base.Channel.Begin<xsl:value-of select="name"/>(request, callback, asyncState);
        }

        &lt;Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)&gt; _
        public <xsl:apply-templates select="output_type"/> End<xsl:value-of select="name"/>(Global.System.IAsyncResult result)
        {
            return base.Channel.End<xsl:value-of select="name"/>(result);
        }

        private Global.System.IAsyncResult OnBegin<xsl:value-of select="name"/>(object[] inValues, Global.System.AsyncCallback callback, object asyncState)
        {
            <xsl:apply-templates select="input_type"/> request = ((<xsl:apply-templates select="input_type"/>)(inValues[0]));
            return this.Begin<xsl:value-of select="name"/>(request, callback, asyncState);
        }

        private object[] OnEnd<xsl:value-of select="name"/>(Global.System.IAsyncResult result)
        {
            <xsl:apply-templates select="output_type"/> retVal = this.End<xsl:value-of select="name"/>(result);
            return new object[] {
                retVal};
        }

        private void On<xsl:value-of select="name"/>Completed(object state)
        {
            if ((this.<xsl:value-of select="name"/>Completed != null))
            {
                InvokeAsyncCompletedEventArgs e = ((InvokeAsyncCompletedEventArgs)(state));
                this.<xsl:value-of select="name"/>Completed(this, new <xsl:value-of select="name"/>CompletedEventArgs(e.Results, e.Error, e.Cancelled, e.UserState));
            }
        }

        public void <xsl:value-of select="name"/>Async(<xsl:apply-templates select="input_type"/> request)
        {
            this.<xsl:value-of select="name"/>Async(request, null);
        }

        public void <xsl:value-of select="name"/>Async(<xsl:apply-templates select="input_type"/> request, object userState)
        {
            if ((this.onBegin<xsl:value-of select="name"/>Delegate == null))
            {
                this.onBegin<xsl:value-of select="name"/>Delegate = new BeginOperationDelegate(this.OnBegin<xsl:value-of select="name"/>);
            }
            if ((this.onEnd<xsl:value-of select="name"/>Delegate == null))
            {
                this.onEnd<xsl:value-of select="name"/>Delegate = new EndOperationDelegate(this.OnEnd<xsl:value-of select="name"/>);
            }
            if ((this.on<xsl:value-of select="name"/>CompletedDelegate == null))
            {
                this.on<xsl:value-of select="name"/>CompletedDelegate = new Global.System.Threading.SendOrPostCallback(this.On<xsl:value-of select="name"/>Completed);
            }
            base.InvokeAsync(this.onBegin<xsl:value-of select="name"/>Delegate, new object[] {
                    request}, this.onEnd<xsl:value-of select="name"/>Delegate, this.on<xsl:value-of select="name"/>CompletedDelegate, userState);
        }
    </xsl:if>
    </xsl:template>
  
  <xsl:template name="escapeKeyword"><xsl:param name="value"/><xsl:choose>
      <xsl:when test="contains($keywordsUpper,concat('|',translate($value, $alpha, $ALPHA),'|'))">[<xsl:value-of select="$value"/>]</xsl:when>
      <xsl:otherwise><xsl:value-of select="$value"/></xsl:otherwise>
    </xsl:choose></xsl:template>
  <xsl:variable name="keywords">|AddHandler|AddressOf|Alias|And|AndAlso|As|Boolean|ByRef|Byte|ByVal|Call|Case|Catch|CBool|CByte|CChar|CDate|CDec|CDbl|Char|CInt|Class|CLng|CObj|Const|Continue|CSByte|CShort|CSng|CStr|CType|CUInt|CULng|CUShort|Date|Decimal|Declare|Default|Delegate|Dim|DirectCast|Do|Double|Each|Else|ElseIf|End|EndIf|Enum|Erase|Error|Event|Exit|False|Finally|For|Friend|Function|Get|GetType|GetXMLNamespace|Global|GoSub|GoTo|Handles|If|Implements|Imports|In|Inherits|Integer|Interface|Is|IsNot|Let|Lib|Like|Long|Loop|Me|Mod|Module|MustInherit|MustOverride|MyBase|MyClass|Namespace|Narrowing|New|Next|Not|Nothing|NotInheritable|NotOverridable|Object|Of|On|Operator|Option|Optional|Or|OrElse|Overloads|Overridable|Overrides|ParamArray|Partial|Private|Property|Protected|Public|RaiseEvent|ReadOnly|ReDim|REM|RemoveHandler|Resume|Return|SByte|Select|Set|Shadows|Shared|Short|Single|Static|Step|Stop|String|Structure|Sub|SyncLock|Then|Throw|To|True|Try|TryCast|TypeOf|Variant|Wend|UInteger|ULong|UShort|Using|When|While|Widening|With|WithEvents|WriteOnly|Xor|</xsl:variable>
  <xsl:variable name="keywordsUpper" select="translate($keywords, $alpha, $ALPHA)"/>

</xsl:stylesheet>
