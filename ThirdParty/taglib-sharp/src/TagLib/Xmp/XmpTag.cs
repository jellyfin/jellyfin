//
// XmpTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2009 Ruben Vermeersch
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;
using System.Collections.Generic;
using System.Xml;

using TagLib.Image;
using TagLib.IFD.Entries;


namespace TagLib.Xmp
{
	/// <summary>
	///    Holds XMP (Extensible Metadata Platform) metadata.
	/// </summary>
	public class XmpTag : ImageTag
	{
		static XmpTag () {
			Initialize ();
		}

#region Parsing speedup
		private Dictionary<string, Dictionary<string, XmpNode>> nodes;

		/// <summary>
		///    Adobe namespace
		/// </summary>
		public static readonly string ADOBE_X_NS = "adobe:ns:meta/";

		/// <summary>
		///    Camera Raw Settings namespace
		/// </summary>
		public static readonly string CRS_NS = "http://ns.adobe.com/camera-raw-settings/1.0/";

		/// <summary>
		///    Dublin Core namespace
		/// </summary>
		public static readonly string DC_NS = "http://purl.org/dc/elements/1.1/";

		/// <summary>
		///    Exif namespace
		/// </summary>
		public static readonly string EXIF_NS = "http://ns.adobe.com/exif/1.0/";

		/// <summary>
		///    Exif aux namespace
		/// </summary>
		public static readonly string EXIF_AUX_NS = "http://ns.adobe.com/exif/1.0/aux/";

		/// <summary>
		///    JOB namespace
		/// </summary>
		public static readonly string JOB_NS = "http://ns.adobe.com/xap/1.0/sType/Job#";

		/// <summary>
		///    Microsoft Photo namespace
		/// </summary>
		public static readonly string MS_PHOTO_NS = "http://ns.microsoft.com/photo/1.0/";

		/// <summary>
		///    Photoshop namespace
		/// </summary>
		public static readonly string PHOTOSHOP_NS = "http://ns.adobe.com/photoshop/1.0/";

		/// <summary>
		///    Prism namespace
		/// </summary>
		public static readonly string PRISM_NS = "http://prismstandard.org/namespaces/basic/2.1/";

		/// <summary>
		///    RDF namespace
		/// </summary>
		public static readonly string RDF_NS = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

		/// <summary>
		///    STDIM namespace
		/// </summary>
		public static readonly string STDIM_NS = "http://ns.adobe.com/xap/1.0/sType/Dimensions#";

		/// <summary>
		///    TIFF namespace
		/// </summary>
		public static readonly string TIFF_NS = "http://ns.adobe.com/tiff/1.0/";

		/// <summary>
		///    XAP (XMP's previous name) namespace
		/// </summary>
		public static readonly string XAP_NS = "http://ns.adobe.com/xap/1.0/";

		/// <summary>
		///    XAP bj namespace
		/// </summary>
		public static readonly string XAP_BJ_NS = "http://ns.adobe.com/xap/1.0/bj/";

		/// <summary>
		///    XAP mm namespace
		/// </summary>
		public static readonly string XAP_MM_NS = "http://ns.adobe.com/xap/1.0/mm/";

		/// <summary>
		///    XAP rights namespace
		/// </summary>
		public static readonly string XAP_RIGHTS_NS = "http://ns.adobe.com/xap/1.0/rights/";

		/// <summary>
		///    XML namespace
		/// </summary>
		public static readonly string XML_NS = "http://www.w3.org/XML/1998/namespace";

		/// <summary>
		///    XMLNS namespace
		/// </summary>
		public static readonly string XMLNS_NS = "http://www.w3.org/2000/xmlns/";

		/// <summary>
		///    XMP TPg (XMP Paged-Text) namespace
		/// </summary>
		public static readonly string XMPTG_NS = "http://ns.adobe.com/xap/1.0/t/pg/";

		internal static readonly string ABOUT_URI = "about";
		internal static readonly string ABOUT_EACH_URI = "aboutEach";
		internal static readonly string ABOUT_EACH_PREFIX_URI = "aboutEachPrefix";
		internal static readonly string ALT_URI = "Alt";
		internal static readonly string BAG_URI = "Bag";
		internal static readonly string BAG_ID_URI = "bagID";
		internal static readonly string DATA_TYPE_URI = "datatype";
		internal static readonly string DESCRIPTION_URI = "Description";
		internal static readonly string ID_URI = "ID";
		internal static readonly string LANG_URI = "lang";
		internal static readonly string LI_URI = "li";
		internal static readonly string NODE_ID_URI = "nodeID";
		internal static readonly string PARSE_TYPE_URI = "parseType";
		internal static readonly string RDF_URI = "RDF";
		internal static readonly string RESOURCE_URI = "resource";
		internal static readonly string SEQ_URI = "Seq";
		internal static readonly string VALUE_URI = "value";

		// This allows for fast string comparison using operator==
		static readonly NameTable NameTable = new NameTable ();
		static bool initialized = false;

		static void Initialize ()
		{
			if (initialized)
				return;

			lock (NameTable) {
				if (initialized)
					return;
				PrepareNamespaces ();
				initialized = true;
			}
		}

		static void PrepareNamespaces ()
		{
			// Namespaces
			AddNamespacePrefix ("", ""); // Needed for the about attribute, which can be unqualified.
			AddNamespacePrefix ("x", ADOBE_X_NS);
			AddNamespacePrefix ("crs", CRS_NS);
			AddNamespacePrefix ("dc", DC_NS);
			AddNamespacePrefix ("exif", EXIF_NS);
			AddNamespacePrefix ("aux", EXIF_AUX_NS);
			AddNamespacePrefix ("stJob", JOB_NS);
			AddNamespacePrefix ("MicrosoftPhoto", MS_PHOTO_NS);
			AddNamespacePrefix ("photoshop", PHOTOSHOP_NS);
			AddNamespacePrefix ("prism", PRISM_NS);
			AddNamespacePrefix ("rdf", RDF_NS);
			AddNamespacePrefix ("stDim", STDIM_NS);
			AddNamespacePrefix ("tiff", TIFF_NS);
			AddNamespacePrefix ("xmp", XAP_NS);
			AddNamespacePrefix ("xapBJ", XAP_BJ_NS);
			AddNamespacePrefix ("xapMM", XAP_MM_NS);
			AddNamespacePrefix ("xapRights", XAP_RIGHTS_NS);
			AddNamespacePrefix ("xml", XML_NS);
			AddNamespacePrefix ("xmlns", XMLNS_NS);
			AddNamespacePrefix ("xmpTPg", XMPTG_NS);

			// Attribute names
			NameTable.Add (ABOUT_URI);
			NameTable.Add (ABOUT_EACH_URI);
			NameTable.Add (ABOUT_EACH_PREFIX_URI);
			NameTable.Add (ALT_URI);
			NameTable.Add (BAG_URI);
			NameTable.Add (BAG_ID_URI);
			NameTable.Add (DATA_TYPE_URI);
			NameTable.Add (DESCRIPTION_URI);
			NameTable.Add (ID_URI);
			NameTable.Add (LANG_URI);
			NameTable.Add (LI_URI);
			NameTable.Add (NODE_ID_URI);
			NameTable.Add (PARSE_TYPE_URI);
			NameTable.Add (RDF_URI);
			NameTable.Add (RESOURCE_URI);
			NameTable.Add (SEQ_URI);
			NameTable.Add (VALUE_URI);
		}

		/// <summary>
		///    Mapping between full namespaces and their short prefix. Needs to be public for the unit test generator.
		/// </summary>
		public static Dictionary<string, string> NamespacePrefixes = new Dictionary<string, string>();

		static int anon_ns_count = 0;

		static void AddNamespacePrefix (string prefix, string ns)
		{
			NameTable.Add (ns);
			NamespacePrefixes.Add (ns, prefix);
		}

#endregion

#region Constructors

		/// <summary>
		///    Construct a new empty <see cref="XmpTag"/>.
		/// </summary>
		public XmpTag ()
		{
			NodeTree = new XmpNode (String.Empty, String.Empty);
			nodes = new Dictionary<string, Dictionary<string, XmpNode>> ();
		}

		/// <summary>
		///    Construct a new <see cref="XmpTag"/>, using the data parsed from the given string.
		/// </summary>
		/// <param name="data">
		///    A <see cref="System.String"/> containing an XMP packet. This should be a valid
		///    XMP block.
		/// </param>
		/// <param name="file">
		///    The file that's currently being parsed, used for reporting corruptions.
		/// </param>
		public XmpTag (string data, TagLib.File file)
		{
			// For some cameras, we have XMP data ending with the null value.
			// This is fine with Mono, but with Microsoft .NET it will throw
			// an XmlException. See also XmpNullValuesTest.cs.
			if (data[data.Length-1] == '\0')
				data = data.Substring(0, data.Length-1);
			
			XmlDocument doc = new XmlDocument (NameTable);
			doc.LoadXml (data);

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("x", ADOBE_X_NS);
			nsmgr.AddNamespace("rdf", RDF_NS);

			XmlNode node = doc.SelectSingleNode("/x:xmpmeta/rdf:RDF", nsmgr);
			// Old versions of XMP were called XAP, fall back to this case (tested in sample_xap.jpg)
			node = node ?? doc.SelectSingleNode("/x:xapmeta/rdf:RDF", nsmgr);
			if (node == null)
				throw new CorruptFileException ();

			NodeTree = ParseRDF (node, file);
			AcceptVisitors ();
		}

#endregion

#region Private Methods

		// 7.2.9 RDF
		//		start-element ( URI == rdf:RDF, attributes == set() )
		//		nodeElementList
		//		end-element()
		private XmpNode ParseRDF (XmlNode rdf_node, TagLib.File file)
		{
			XmpNode top = new XmpNode (String.Empty, String.Empty);
			foreach (XmlNode node in rdf_node.ChildNodes) {
				if (node is XmlWhitespace)
					continue;

				if (node.Is (RDF_NS, DESCRIPTION_URI)) {
					var attr = node.Attributes.GetNamedItem (RDF_NS, ABOUT_URI) as XmlAttribute;
					if (attr != null) {
						if (top.Name != String.Empty && top.Name != attr.InnerText)
							throw new CorruptFileException ("Multiple inconsistent rdf:about values!");
						top.Name = attr.InnerText;
					}
					continue;
				}

				file.MarkAsCorrupt ("Cannot have anything other than rdf:Description at the top level");
				return top;
			}
			ParseNodeElementList (top, rdf_node);
			return top;
		}

		// 7.2.10 nodeElementList
		//		ws* ( nodeElement ws* )*
		private void ParseNodeElementList (XmpNode parent, XmlNode xml_parent)
		{
			foreach (XmlNode node in xml_parent.ChildNodes) {
				if (node is XmlWhitespace)
					continue;
				ParseNodeElement (parent, node);
			}
		}

		// 7.2.11 nodeElement
		//		start-element ( URI == nodeElementURIs,
		//						attributes == set ( ( idAttr | nodeIdAttr | aboutAttr )?, propertyAttr* ) )
		//		propertyEltList
		//		end-element()
		//
		// 7.2.13 propertyEltList
		//		ws* ( propertyElt ws* )*
		private void ParseNodeElement (XmpNode parent, XmlNode node)
		{
			if (!node.IsNodeElement ())
				throw new CorruptFileException ("Unexpected node found, invalid RDF?");

			if (node.Is (RDF_NS, SEQ_URI)) {
				parent.Type = XmpNodeType.Seq;
			} else if (node.Is (RDF_NS, ALT_URI)) {
				parent.Type = XmpNodeType.Alt;
			} else if (node.Is (RDF_NS, BAG_URI)) {
				parent.Type = XmpNodeType.Bag;
			} else if (node.Is (RDF_NS, DESCRIPTION_URI)) {
				parent.Type = XmpNodeType.Struct;
			} else {
				throw new Exception ("Unknown nodeelement found! Perhaps an unimplemented collection?");
			}

			foreach (XmlAttribute attr in node.Attributes) {
				if (attr.In (XMLNS_NS))
					continue;
				if (attr.Is (RDF_NS, ID_URI) || attr.Is (RDF_NS, NODE_ID_URI) || attr.Is (RDF_NS, ABOUT_URI))
					continue;
				if (attr.Is (XML_NS, LANG_URI))
					throw new CorruptFileException ("xml:lang is not allowed here!");
				parent.AddChild (new XmpNode (attr.NamespaceURI, attr.LocalName, attr.InnerText));
			}

			foreach (XmlNode child in node.ChildNodes) {
				if (child is XmlWhitespace || child is XmlComment)
					continue;
				ParsePropertyElement (parent, child);
			}
		}

		// 7.2.14 propertyElt
		//		resourcePropertyElt | literalPropertyElt | parseTypeLiteralPropertyElt |
		//		parseTypeResourcePropertyElt | parseTypeCollectionPropertyElt |
		//		parseTypeOtherPropertyElt | emptyPropertyElt
		private void ParsePropertyElement (XmpNode parent, XmlNode node)
		{
			int count = 0;
			bool has_other = false;
			foreach (XmlAttribute attr in node.Attributes) {
				if (!attr.In (XMLNS_NS))
					count++;

				if (!attr.Is (XML_NS, LANG_URI) && !attr.Is (RDF_NS, ID_URI) && !attr.In (XMLNS_NS))
					has_other = true;
			}

			if (count > 3) {
				ParseEmptyPropertyElement (parent, node);
			} else {
				if (!has_other) {
					if (!node.HasChildNodes) {
						ParseEmptyPropertyElement (parent, node);
					} else {
						bool only_text = true;
						foreach (XmlNode child in node.ChildNodes) {
							if (!(child is XmlText))
								only_text = false;
						}

						if (only_text) {
							ParseLiteralPropertyElement (parent, node);
						} else {
							ParseResourcePropertyElement (parent, node);
						}
					}
				} else {
					foreach (XmlAttribute attr in node.Attributes) {
						if (attr.Is (XML_NS, LANG_URI) || attr.Is (RDF_NS, ID_URI) || attr.In (XMLNS_NS))
							continue;

						if (attr.Is (RDF_NS, DATA_TYPE_URI)) {
							ParseLiteralPropertyElement (parent, node);
						} else if (!attr.Is (RDF_NS, PARSE_TYPE_URI)) {
							ParseEmptyPropertyElement (parent, node);
						} else if (attr.InnerText.Equals ("Resource")) {
							ParseTypeResourcePropertyElement (parent, node);
						} else {
							// Neither Literal, Collection or anything else is allowed
							throw new CorruptFileException (String.Format ("This is not allowed in XMP! Bad XMP: {0}", node.OuterXml));
						}
					}
				}
			}
		}

		// 7.2.15 resourcePropertyElt
		//		start-element ( URI == propertyElementURIs, attributes == set ( idAttr? ) )
		//		ws* nodeElement ws*
		//		end-element()
		private void ParseResourcePropertyElement (XmpNode parent, XmlNode node)
		{
			if (!node.IsPropertyElement ())
				throw new CorruptFileException ("Invalid property");

			XmpNode new_node = new XmpNode (node.NamespaceURI, node.LocalName);
			foreach (XmlAttribute attr in node.Attributes) {
				if (attr.Is (XML_NS, LANG_URI)) {
					new_node.AddQualifier (new XmpNode (XML_NS, LANG_URI, attr.InnerText));
				} else if (attr.Is (RDF_NS, ID_URI) || attr.In (XMLNS_NS)) {
					continue;
				}

				throw new CorruptFileException (String.Format ("Invalid attribute: {0}", attr.OuterXml));
			}

			bool has_xml_children = false;
			foreach (XmlNode child in node.ChildNodes) {
				if (child is XmlWhitespace)
					continue;
				if (child is XmlText)
					throw new CorruptFileException ("Can't have text here!");
				has_xml_children = true;

				ParseNodeElement (new_node, child);
			}

			if (!has_xml_children)
				throw new CorruptFileException ("Missing children for resource property element");

			parent.AddChild (new_node);
		}

		// 7.2.16 literalPropertyElt
		//		start-element ( URI == propertyElementURIs, attributes == set ( idAttr?, datatypeAttr?) )
		//		text()
		//		end-element()
		private void ParseLiteralPropertyElement (XmpNode parent, XmlNode node)
		{
			if (!node.IsPropertyElement ())
				throw new CorruptFileException ("Invalid property");
			parent.AddChild (CreateTextPropertyWithQualifiers (node, node.InnerText));
		}

		// 7.2.18 parseTypeResourcePropertyElt
		//		start-element ( URI == propertyElementURIs, attributes == set ( idAttr?, parseResource ) )
		//		propertyEltList
		//		end-element()
		private void ParseTypeResourcePropertyElement (XmpNode parent, XmlNode node)
		{
			if (!node.IsPropertyElement ())
				throw new CorruptFileException ("Invalid property");

			XmpNode new_node = new XmpNode (node.NamespaceURI, node.LocalName);
			new_node.Type = XmpNodeType.Struct;

			foreach (XmlNode attr in node.Attributes) {
				if (attr.Is (XML_NS, LANG_URI))
					new_node.AddQualifier (new XmpNode (XML_NS, LANG_URI, attr.InnerText));
			}

			foreach (XmlNode child in node.ChildNodes) {
				if (child is XmlWhitespace || child is XmlComment)
					continue;
				ParsePropertyElement (new_node, child);
			}

			parent.AddChild (new_node);
		}

		// 7.2.21 emptyPropertyElt
		//		start-element ( URI == propertyElementURIs,
		//						attributes == set ( idAttr?, ( resourceAttr | nodeIdAttr )?, propertyAttr* ) )
		//		end-element()
		private void ParseEmptyPropertyElement (XmpNode parent, XmlNode node)
		{
			if (!node.IsPropertyElement ())
				throw new CorruptFileException ("Invalid property");
			if (node.HasChildNodes)
				throw new CorruptFileException (String.Format ("Can't have content in this node! Node: {0}", node.OuterXml));

			var rdf_value = node.Attributes.GetNamedItem (VALUE_URI, RDF_NS) as XmlAttribute;
			var rdf_resource = node.Attributes.GetNamedItem (RESOURCE_URI, RDF_NS) as XmlAttribute;

			// Options 1 and 2
			var simple_prop_val = rdf_value ?? rdf_resource ?? null;
			if (simple_prop_val != null) {
				string value = simple_prop_val.InnerText;
				parent.AddChild (CreateTextPropertyWithQualifiers (node, value));
				return;
			}

			// Options 3 & 4
			var new_node = new XmpNode (node.NamespaceURI, node.LocalName);
			foreach (XmlAttribute a in node.Attributes) {
				if (a.Is(RDF_NS, ID_URI) || a.Is(RDF_NS, NODE_ID_URI)) {
					continue;
				} else if (a.In (XMLNS_NS)) {
					continue;
				} else if (a.Is (XML_NS, LANG_URI)) {
					new_node.AddQualifier (new XmpNode (XML_NS, LANG_URI, a.InnerText));
				}

				new_node.AddChild (new XmpNode (a.NamespaceURI, a.LocalName, a.InnerText));
			}
			parent.AddChild (new_node);
		}

		private XmpNode CreateTextPropertyWithQualifiers (XmlNode node, string value)
		{
			XmpNode t = new XmpNode (node.NamespaceURI, node.LocalName, value);
			foreach (XmlAttribute attr in node.Attributes) {
				if (attr.In (XMLNS_NS))
					continue;
				if (attr.Is (RDF_NS, VALUE_URI) || attr.Is (RDF_NS, RESOURCE_URI))
					continue; // These aren't qualifiers
				t.AddQualifier (new XmpNode (attr.NamespaceURI, attr.LocalName, attr.InnerText));
			}
			return t;
		}

		private XmpNode NewNode (string ns, string name)
		{
			Dictionary <string, XmpNode> ns_nodes = null;

			if (!nodes.ContainsKey (ns)) {
				ns_nodes = new Dictionary <string, XmpNode> ();
				nodes.Add (ns, ns_nodes);

			} else
				ns_nodes = nodes [ns];

			if (ns_nodes.ContainsKey (name)) {
				foreach (XmpNode child_node in NodeTree.Children) {
					if (child_node.Namespace == ns && child_node.Name == name) {
						NodeTree.RemoveChild (child_node);
						break;
					}
				}

				ns_nodes.Remove (name);
			}

			XmpNode node = new XmpNode (ns, name);
			ns_nodes.Add (name, node);

			NodeTree.AddChild (node);

			return node;
		}

		private XmpNode NewNode (string ns, string name, XmpNodeType type)
		{
			XmpNode node = NewNode (ns, name);
			node.Type = type;

			return node;
		}

		private void RemoveNode (string ns, string name)
		{
			if (!nodes.ContainsKey (ns))
				return;

			foreach (XmpNode node in NodeTree.Children) {
				if (node.Namespace == ns && node.Name == name) {
					NodeTree.RemoveChild (node);
					break;
				}
			}

			nodes[ns].Remove (name);
		}

		/// <summary>
		/// Accept visitors to touch up the node tree.
		/// </summary>
		private void AcceptVisitors ()
		{
			NodeTree.Accept (new NodeIndexVisitor (this));
			//NodeTree.Dump ();
			//Console.WriteLine (node.OuterXml);
		}

#endregion

#region Public Properties

		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.XMP" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.XMP;}
		}

		/// <summary>
		///    Get the tree of <see cref="XmpNode" /> nodes. These contain the values
		///    parsed from the XMP file.
		/// </summary>
		public XmpNode NodeTree {
			get; private set;
		}

#endregion

#region Public Methods

		/// <summary>
		///	   Replace the current tag with the given one.
		///	</summary>
		/// <param name="tag">
		///    The tag from which the data should be copied.
		/// </param>
		public void ReplaceFrom (XmpTag tag)
		{
			NodeTree = tag.NodeTree;
			nodes = new Dictionary<string, Dictionary<string, XmpNode>> ();
			AcceptVisitors ();
		}

		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		///    Finds the node associated with the namespace <paramref name="ns"/> and the name
		///    <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A <see cref="XmpNode"/> with the found node, or <see langword="null"/>
		///    if no node was found.
		/// </returns>
		public XmpNode FindNode (string ns, string name)
		{
			if (!nodes.ContainsKey (ns))
				return null;
			if (!nodes [ns].ContainsKey (name))
				return null;
			return nodes [ns][name];

		}

		/// <summary>
		///    Returns the text of the node associated with the namespace
		///    <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A <see cref="System.String"/> with the text of the node, or
		///    <see langword="null"/> if no such node exists, or if it is not
		///    a text node.
		/// </returns>
		public string GetTextNode (string ns, string name)
		{
			var node = FindNode (ns, name);

			if (node == null || node.Type != XmpNodeType.Simple)
				return null;

			return node.Value;
		}

		/// <summary>
		///    Creates a new text node associated with the namespace
		///    <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <param name="value">
		///    A <see cref="System.String"/> with the value for the new node.
		///    If <see langword="null"/> is given, a possibly existing node will
		///    be deleted.
		/// </param>
		public void SetTextNode (string ns, string name, string value)
		{
			if (value == null) {
				RemoveNode (ns, name);
				return;
			}

			var node = NewNode (ns, name);
			node.Value = value;
		}

		/// <summary>
		///    Searches for a node holding language alternatives. The return value
		///    is the value of the default language stored by the node. The node is
		///    identified by the namespace <paramref name="ns"/> and the name
		///    <paramref name="name"/>. If the default language is not set, an arbitrary
		///    one is chosen.
		///    It is also tried to return the value a simple text node, if no
		///    associated alt-node exists.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A <see cref="System.String"/> with the value stored as default language
		///    for the referenced node.
		/// </returns>
		public string GetLangAltNode (string ns, string name)
		{
			var node = FindNode (ns, name);

			if (node == null)
				return null;

			if (node.Type == XmpNodeType.Simple)
				return node.Value;

			if (node.Type != XmpNodeType.Alt)
				return null;

			var children = node.Children;
			foreach (XmpNode child_node in children) {
				var qualifier = child_node.GetQualifier (XML_NS, "lang");
				if (qualifier != null && qualifier.Value == "x-default")
					return child_node.Value;
			}

			if (children.Count > 0 && children[0].Type == XmpNodeType.Simple)
				return children[0].Value;

			return null;
		}

		/// <summary>
		///    Stores a the given <paramref name="value"/> as the default language
		///    value for the alt-node associated with the namespace
		///    <paramref name="ns"/> and the name <paramref name="name"/>.
		///    All other alternatives set, are deleted by this method.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <param name="value">
		///    A <see cref="System.String"/> with the value for the default language
		///    to set. If <see langword="null"/> is given, a possibly existing node
		///    will be deleted.
		/// </param>
		public void SetLangAltNode (string ns, string name, string value)
		{
			if (value == null) {
				RemoveNode (ns, name);
				return;
			}

			var node = NewNode (ns, name, XmpNodeType.Alt);

			var child_node = new XmpNode (RDF_NS, LI_URI, value);
			child_node.AddQualifier (new XmpNode (XML_NS, "lang", "x-default"));

			node.AddChild (child_node);
		}

		/// <summary>
		///    The method returns an array of <see cref="System.String"/> values
		///    which are the stored text of the child nodes of the node associated
		///    with the namespace <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.String[]"/> with the text stored in the child nodes.
		/// </returns>
		public string[] GetCollectionNode (string ns, string name)
		{
			var node = FindNode (ns, name);

			if (node == null)
				return null;

			List<string> items = new List<string> ();

			foreach (XmpNode child in node.Children) {

				string item = child.Value;
				if (item != null)
					items.Add (item);
			}

			return items.ToArray ();
		}

		/// <summary>
		///    Sets a <see cref="T:System.String[]"/> as texts to the children of the
		///    node associated with the namespace <paramref name="ns"/> and the name
		///    <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <param name="values">
		///    A <see cref="T:System.String[]"/> with the values to set for the children.
		/// </param>
		/// <param name="type">
		///    A <see cref="XmpNodeType"/> with the type of the parent node.
		/// </param>
		public void SetCollectionNode (string ns, string name, string [] values, XmpNodeType type)
		{
			if (type == XmpNodeType.Simple || type == XmpNodeType.Alt)
				throw new ArgumentException ("type");

			if (values == null) {
				RemoveNode (ns, name);
				return;
			}

			var node = NewNode (ns, name, type);
			foreach (string value in values)
				node.AddChild (new XmpNode (RDF_NS, LI_URI, value));
		}

		/// <summary>
		///    Returns the rational value of the node associated with the namespace
		///    <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A double? with the read value, or
		///    <see langword="null"/> if no such node exists, or if it is in wrong
		///    format.
		/// </returns>
		/// <remarks>
		///    Rational nodes only used in EXIF schema.
		/// </remarks>
		public double? GetRationalNode (string ns, string name)
		{
			var text = GetTextNode (ns, name);

			if (text == null)
				return null;

			// format is expected to be e.g. "1/200" ...
			string [] values = text.Split ('/');

			if (values.Length != 2) {

				// but we also try to parse a double value directly.
				double result;
				if (Double.TryParse (text, out result))
					return result;

				return null;
			}

			double nom, den;
			if (Double.TryParse (values[0], out nom) && Double.TryParse (values[1], out den)) {
				if (den != 0.0)
					return ((double) nom) / ((double) den);
			}

			return null;
		}

		/// <summary>
		///    Creates a new rational node with the namespace
		///    <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <param name="value">
		///    A <see cref="System.Double"/> with the value of the node.
		/// </param>
		public void SetRationalNode (string ns, string name, double value)
		{

			string fraction = DecimalToFraction (value, (long) Math.Pow (10, 10));
			SetTextNode (ns, name, fraction);
		}

		// Based on http://www.ics.uci.edu/~eppstein/numth/frap.c
		private string DecimalToFraction (double value, long max_denominator) {
			var m = new long [2, 2];
			m[0, 0] = m[1, 1] = 1;
			m[1, 0] = m[0, 1] = 0;

			double x = value;
			long ai;

			while (m[1, 0] *  (ai = (long)x) + m[1, 1] <= max_denominator) {
				long t = m[0, 0] * ai + m[0, 1];
				m[0, 1] = m[0, 0];
				m[0, 0] = t;
				t = m[1, 0] * ai + m[1, 1];
				m[1, 1] = m[1, 0];
				m[1, 0] = t;
				if (x == (double)ai) break;     // AF: division by zero
				x = 1 / (x - (double) ai);
				if (x > (double) 0x7FFFFFFF) break;  // AF: representation failure

			}

			return String.Format ("{0}/{1}", m[0, 0], m[1, 0]);
		}


		/// <summary>
		///    Returns the unsigned integer value of the node associated with the
		///    namespace <paramref name="ns"/> and the name <paramref name="name"/>.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the node.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the node.
		/// </param>
		/// <returns>
		///    A uint? with the read value, or
		///    <see langword="null"/> if no such node exists, or if it is in wrong
		///    format.
		/// </returns>
		public uint? GetUIntNode (string ns, string name)
		{
			var text = GetTextNode (ns, name);

			if (text == null)
				return null;

			uint result;

			if (UInt32.TryParse (text, out result))
				return result;

			return null;
		}

		/// <summary>
		///    Renders the current instance to an XMP <see cref="System.String"/>.
		/// </summary>
		/// <returns>
		///    A <see cref="System.String"/> with the XMP structure.
		/// </returns>
		public string Render ()
		{
			XmlDocument doc = new XmlDocument (NameTable);
			var meta = CreateNode (doc, "xmpmeta", ADOBE_X_NS);
			var rdf = CreateNode (doc, "RDF", RDF_NS);
			var description = CreateNode (doc, "Description", RDF_NS);
			NodeTree.RenderInto (description);
			doc.AppendChild (meta);
			meta.AppendChild (rdf);
			rdf.AppendChild (description);
			return doc.OuterXml;
		}

		/// <summary>
		///    Make sure there's a suitable prefix mapped for the given namespace URI.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace that will be rendered.
		/// </param>
		static void EnsureNamespacePrefix (string ns)
		{
			if (!NamespacePrefixes.ContainsKey (ns)) {
				NamespacePrefixes.Add (ns, String.Format ("ns{0}", ++anon_ns_count));
				Console.WriteLine ("TAGLIB# DEBUG: Added {0} prefix for {1} namespace (XMP)", NamespacePrefixes [ns], ns);
			}
		}

		internal static XmlNode CreateNode (XmlDocument doc, string name, string ns)
		{
			EnsureNamespacePrefix (ns);
			return doc.CreateElement (NamespacePrefixes [ns], name, ns);
		}

		internal static XmlAttribute CreateAttribute (XmlDocument doc, string name, string ns)
		{
			EnsureNamespacePrefix (ns);
			return doc.CreateAttribute (NamespacePrefixes [ns], name, ns);
		}

#endregion

		private class NodeIndexVisitor : XmpNodeVisitor
		{
			private XmpTag tag;

			public NodeIndexVisitor (XmpTag tag) {
				this.tag = tag;
			}

			public void Visit (XmpNode node)
			{
				// TODO: This should be a proper check to see if it is a nodeElement
				if (node.Namespace == XmpTag.RDF_NS && node.Name == XmpTag.LI_URI)
					return;

				AddNode (node);
			}

			void AddNode (XmpNode node)
			{
				if (tag.nodes == null)
					tag.nodes = new Dictionary<string, Dictionary<string, XmpNode>> ();
				if (!tag.nodes.ContainsKey (node.Namespace))
					tag.nodes [node.Namespace] = new Dictionary<string, XmpNode> ();

				tag.nodes [node.Namespace][node.Name] = node;
			}
		}

#region Metadata fields

		/// <summary>
		///    Gets or sets the comment for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the comment of the
		///    current instace.
		/// </value>
		public override string Comment {
			get {
				string comment = GetLangAltNode (DC_NS, "description");

				if (comment != null)
					return comment;

				comment = GetLangAltNode (EXIF_NS, "UserComment");
				return comment;
			}
			set {
				SetLangAltNode (DC_NS, "description", value);
				SetLangAltNode (EXIF_NS, "UserComment", value);
			}
		}

		/// <summary>
		///    Gets or sets the keywords for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the keywords of the
		///    current instace.
		/// </value>
		public override string[] Keywords {
			get { return GetCollectionNode (DC_NS, "subject") ?? new string [] {}; }
			set { SetCollectionNode (DC_NS, "subject", value, XmpNodeType.Bag); }
		}

		/// <summary>
		///    Gets or sets the rating for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> containing the rating of the
		///    current instace.
		/// </value>
		public override uint? Rating {
			get { return GetUIntNode (XAP_NS, "Rating"); }
			set { SetTextNode (XAP_NS, "Rating", value != null ? value.ToString () : null);
			}
		}

		/// <summary>
		///    Gets or sets the time when the image, the current instance
		///    belongs to, was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the time the image was taken.
		/// </value>
		public override DateTime? DateTime {
			get {
				// TODO: use correct parsing
				try {
					return System.DateTime.Parse (GetTextNode (XAP_NS, "CreateDate"));
				} catch {}

				return null;
			}
			set {
				// TODO: write correct format
				SetTextNode (XAP_NS, "CreateDate", value != null ? value.ToString () : null);
			}
		}

		/// <summary>
		///    Gets or sets the orientation of the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Image.ImageOrientation" /> containing the orientation of the
		///    image
		/// </value>
		public override ImageOrientation Orientation {
			get {
				var orientation = GetUIntNode (TIFF_NS, "Orientation");

				if (orientation.HasValue)
					return (ImageOrientation) orientation;

				return ImageOrientation.None;
			}
			set {
				if ((uint) value < 1U || (uint) value > 8U) {
					RemoveNode (TIFF_NS, "Orientation");
					return;
				}

				SetTextNode (TIFF_NS, "Orientation", String.Format ("{0}", (ushort) value));
			}
		}

		/// <summary>
		///    Gets or sets the software the image, the current instance
		///    belongs to, was created with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the name of the
		///    software the current instace was created with.
		/// </value>
		public override string Software {
			get { return GetTextNode (XAP_NS, "CreatorTool"); }
			set { SetTextNode (XAP_NS, "CreatorTool", value); }
		}

		/// <summary>
		///    Gets or sets the latitude of the GPS coordinate the current
		///    image was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the latitude ranging from -90.0
		///    to +90.0 degrees.
		/// </value>
		public override double? Latitude {
			get { return null; }
			set {}
		}

		/// <summary>
		///    Gets or sets the longitude of the GPS coordinate the current
		///    image was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the longitude ranging from -180.0
		///    to +180.0 degrees.
		/// </value>
		public override double? Longitude {
			get { return null; }
			set {}
		}

		/// <summary>
		///    Gets or sets the altitude of the GPS coordinate the current
		///    image was taken. The unit is meter.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the altitude. A positive value
		///    is above sea level, a negative one below sea level. The unit is meter.
		/// </value>
		public override double? Altitude {
			get { return null; }
			set {}
		}

		/// <summary>
		///    Gets the exposure time the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the exposure time in seconds.
		/// </value>
		public override double? ExposureTime {
			get { return GetRationalNode (EXIF_NS, "ExposureTime"); }
			set { SetRationalNode (EXIF_NS, "ExposureTime", value.HasValue ? (double) value : 0); }
		}

		/// <summary>
		///    Gets the FNumber the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the FNumber.
		/// </value>
		/// <remarks>
		///    Bibble wrongly tends to put this into tiff:FNumber so we
		///    use that as a fallback and correct it if needed.
		/// </remarks>
		public override double? FNumber {
			get {
				return GetRationalNode (EXIF_NS, "FNumber") ??
					GetRationalNode (TIFF_NS, "FNumber");
			}
			set {
				SetTextNode (TIFF_NS, "FNumber", null); // Remove wrong value
				SetRationalNode (EXIF_NS, "FNumber", value.HasValue ? (double) value : 0);
			}
		}

		/// <summary>
		///    Gets the ISO speed the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the ISO speed as defined in ISO 12232.
		/// </value>
		/// <remarks>
		///    Bibble writes ISOSpeedRating instead of ISOSpeedRatings.
		/// </remarks>
		public override uint? ISOSpeedRatings {
			get {
				string[] values = GetCollectionNode (EXIF_NS, "ISOSpeedRatings");

				if (values != null && values.Length > 0) {
					uint result;
					if (UInt32.TryParse (values[0], out result))
						return result;
				}

				// Bibble fallback.
				return GetUIntNode (EXIF_NS, "ISOSpeedRating");
			}
			set {
				SetCollectionNode (EXIF_NS, "ISOSpeedRating", null, XmpNodeType.Seq);
				SetCollectionNode (EXIF_NS, "ISOSpeedRatings", new string [] { value.ToString () }, XmpNodeType.Seq);
			}
		}

		/// <summary>
		///    Gets the focal length the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the focal length in millimeters.
		/// </value>
		public override double? FocalLength {
			get { return GetRationalNode (EXIF_NS, "FocalLength"); }
			set { SetRationalNode (EXIF_NS, "FocalLength", value.HasValue ? (double) value : 0); }
		}

		/// <summary>
		///    Gets the focal length the image, the current instance belongs
		///    to, was taken with, assuming a 35mm film camera.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the focal length in 35mm equivalent in millimeters.
		/// </value>
		public override uint? FocalLengthIn35mmFilm {
			get { return GetUIntNode (EXIF_NS, "FocalLengthIn35mmFilm"); }
			set { SetTextNode (EXIF_NS, "FocalLengthIn35mmFilm", value.HasValue ? value.Value.ToString () : String.Empty); }
		}

		/// <summary>
		///    Gets the manufacture of the recording equipment the image, the
		///    current instance belongs to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the manufacture name.
		/// </value>
		public override string Make {
			get { return GetTextNode (TIFF_NS, "Make"); }
			set { SetTextNode (TIFF_NS, "Make", value); }
		}

		/// <summary>
		///    Gets the model name of the recording equipment the image, the
		///    current instance belongs to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the model name.
		/// </value>
		public override string Model {
			get { return GetTextNode (TIFF_NS, "Model"); }
			set { SetTextNode (TIFF_NS, "Model", value); }
		}

		/// <summary>
		///    Gets or sets the creator of the image.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the name of the creator.
		/// </value>
		public override string Creator {
			get {
				string [] values = GetCollectionNode (DC_NS, "creator");
				if (values != null && values.Length > 0)
					return values [0];

				return null;
			}
			set {
				if (value == null)
					RemoveNode (DC_NS, "creator");

				SetCollectionNode (DC_NS, "creator", new string [] { value }, XmpNodeType.Seq);
			}
		}

		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		public override string Title {
			get { return GetLangAltNode (DC_NS, "title"); }
			set { SetLangAltNode (DC_NS, "title", value); }
		}

		/// <summary>
		///    Gets and sets the copyright information for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		public override string Copyright {
			get { return GetLangAltNode (DC_NS, "rights"); }
			set { SetLangAltNode (DC_NS, "rights", value); }
		}

#endregion
	}
}
