//
// XmpNode.cs:
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

namespace TagLib.Xmp
{
	/// <summary>
	///    An <see cref="XmpNode"/> represents a node in the XMP document.
	///    This is any valid XMP element.
	/// </summary>
	public class XmpNode
	{

#region Private Fields

		/// <value>
		///    The children of the current node
		/// </value>
		private List<XmpNode> children;

		/// <value>
		///    The qualifiers of the current node
		/// </value>
		private Dictionary<string, Dictionary<string, XmpNode>> qualifiers;

		/// <value>
		///    The name of the current node
		/// </value>
		private string name;

#endregion

#region Properties

		/// <value>
		///    The namespace the current instance belongs to
		/// </value>
		public string Namespace { get; private set; }

		/// <value>
		///    The name of the current node instance
		/// </value>
		public string Name {
			get { return name; }
			internal set {
				if (name != null)
					throw new Exception ("Cannot change named node");

				if (value == null)
					throw new ArgumentException ("value");

				name = value;
			}
		}

		/// <value>
		///    The text value of the current node
		/// </value>
		public string Value { get; set; }

		/// <value>
		///    The type of the current node
		/// </value>
		public XmpNodeType Type { get; internal set; }


		/// <value>
		///    The number of qualifiers of the current instance
		/// </value>
		public int QualifierCount {
			get {
				if (qualifiers == null)
					return 0;
				int count = 0;
				foreach (var collection in qualifiers.Values) {
					count += collection == null ? 0 : collection.Count;
				}
				return count;
			}
		}

		/// <value>
		///    The children of the current instance.
		/// </value>
		public List<XmpNode> Children {
			// TODO: do not return a list, because it can be modified elsewhere
			get { return children ?? new List<XmpNode> (); }
		}

#endregion

#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the new instance.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the new instance.
		/// </param>
		public XmpNode (string ns, string name)
		{
			// Namespaces in XMP need to end with / or #. Broken files are known
			// to be floating around (we have one with MicrosoftPhoto in our tree).
			// Correcting below.
			if (ns != String.Empty && ns != XmpTag.XML_NS && !ns.EndsWith ("/") && !ns.EndsWith ("#"))
				ns = String.Format ("{0}/", ns);

			Namespace = ns;
			Name = name;
			Type = XmpNodeType.Simple;
			Value = String.Empty;
		}

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the new instance.
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the new instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="System.String"/> with the txt value of the new instance.
		/// </param>
		public XmpNode (string ns, string name, string value) : this (ns, name)
		{
			Value = value;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Adds a node as child of the current node
		/// </summary>
		/// <param name="node">
		///    A <see cref="XmpNode"/> to be add as child
		/// </param>
		public void AddChild (XmpNode node)
		{
			if (node == null || node == this)
				throw new ArgumentException ("node");

			if (children == null)
				children = new List<XmpNode> ();

			children.Add (node);
		}

		/// <summary>
		///    Removes the given node as child of the current instance
		/// </summary>
		/// <param name="node">
		///    A <see cref="XmpNode"/> to remove as child
		/// </param>
		public void RemoveChild (XmpNode node)
		{
			if (children == null)
				return;

			children.Remove (node);
		}

		/// <summary>
		///    Get a named child from the current node
		/// </summary>
		/// <param name="ns">
		///    The namespace of the child node.
		/// </param>
		/// <param name="name">
		///    The name of the child node.
		/// </param>
		/// <returns>
		///    A <see cref="XmpNode"/> with the given name and namespace.
		/// </returns>
		public XmpNode GetChild (string ns, string name)
		{
			foreach (var node in children) {
				if (node.Namespace.Equals (ns) && node.Name.Equals (name))
					return node;
			}
			return null;
		}

		/// <summary>
		///    Adds a node as qualifier of the current instance
		/// </summary>
		/// <param name="node">
		///    A <see cref="XmpNode"/> to add as qualifier
		/// </param>
		public void AddQualifier (XmpNode node)
		{
			if (node == null || node == this)
				throw new ArgumentException ("node");

			if (qualifiers == null)
				qualifiers = new Dictionary<string, Dictionary<string, XmpNode>> ();

			if (!qualifiers.ContainsKey (node.Namespace))
				qualifiers [node.Namespace] = new Dictionary<string, XmpNode> ();

			qualifiers [node.Namespace][node.Name] = node;
		}

		/// <summary>
		///    Returns the qualifier associated with the given namespace <paramref name="ns"/>
		///    and name <paramref name="name"/>
		/// </summary>
		/// <param name="ns">
		///    A <see cref="System.String"/> with the namespace of the qualifier
		/// </param>
		/// <param name="name">
		///    A <see cref="System.String"/> with the name of the qualifier
		/// </param>
		/// <returns>
		///    A <see cref="XmpNode"/> with the qualifier
		/// </returns>
		public XmpNode GetQualifier (string ns, string name)
		{
			if (qualifiers == null)
				return null;
			if (!qualifiers.ContainsKey (ns))
				return null;
			if (!qualifiers [ns].ContainsKey (name))
				return null;
			return qualifiers [ns][name];
		}

		/// <summary>
		///    Print a debug output of the node.
		/// </summary>
		public void Dump ()
		{
			Dump ("");
		}

		/// <summary>
		///    Calls the Visitor for this node and every child node.
		/// </summary>
		/// <param name="visitor">
		///    A <see cref="XmpNodeVisitor"/> to access the node and the children.
		/// </param>
		public void Accept (XmpNodeVisitor visitor)
		{
			visitor.Visit (this);

			// TODO: what is with the qualifiers ?
			// either add them to be also visited, or add a comment
			if (children != null) {
				foreach (XmpNode child in children) {
					child.Accept (visitor);
				}
			}
		}

		/// <summary>
		///    Renders the current instance as child of the given node to the
		///    given <see cref="XmlNode"/>
		/// </summary>
		/// <param name="parent">
		///    A <see cref="XmlNode"/> to render the current instance as child of.
		/// </param>
		public void RenderInto (XmlNode parent)
		{
			if (IsRootNode) {
				AddAllChildrenTo (parent);

			} else if (IsReallySimpleType && parent.Attributes.GetNamedItem (XmpTag.PARSE_TYPE_URI, XmpTag.RDF_NS) == null) {
				// Simple values can be added as attributes of the parent node. Not allowed when the parent has an rdf:parseType.
				XmlAttribute attr = XmpTag.CreateAttribute (parent.OwnerDocument, Name, Namespace);
				attr.Value = Value;
				parent.Attributes.Append (attr);

			} else if (Type == XmpNodeType.Simple || Type == XmpNodeType.Struct) {
				var node = XmpTag.CreateNode (parent.OwnerDocument, Name, Namespace);
				node.InnerText = Value;

				if (Type == XmpNodeType.Struct) {
					// Structured types are always handled as a parseType=Resource node. This way, IsReallySimpleType will
					// not match for child nodes, which makes sure they are added as extra nodes to this node. Does the
					// trick well, unit tests that prove this are in XmpSpecTest.
					XmlAttribute attr = XmpTag.CreateAttribute (parent.OwnerDocument, XmpTag.PARSE_TYPE_URI, XmpTag.RDF_NS);
					attr.Value = "Resource";
					node.Attributes.Append (attr);
				}

				AddAllQualifiersTo (node);
				AddAllChildrenTo (node);
				parent.AppendChild (node);

			} else if (Type == XmpNodeType.Bag) {
				var node = XmpTag.CreateNode (parent.OwnerDocument, Name, Namespace);
				// TODO: Add all qualifiers.
				if (QualifierCount > 0)
					throw new NotImplementedException ();
				var bag = XmpTag.CreateNode (parent.OwnerDocument, XmpTag.BAG_URI, XmpTag.RDF_NS);
				foreach (var child in Children)
					child.RenderInto (bag);
				node.AppendChild (bag);
				parent.AppendChild (node);

			} else if (Type == XmpNodeType.Alt) {
				var node = XmpTag.CreateNode (parent.OwnerDocument, Name, Namespace);
				// TODO: Add all qualifiers.
				if (QualifierCount > 0)
					throw new NotImplementedException ();
				var bag = XmpTag.CreateNode (parent.OwnerDocument, XmpTag.ALT_URI, XmpTag.RDF_NS);
				foreach (var child in Children)
					child.RenderInto (bag);
				node.AppendChild (bag);
				parent.AppendChild (node);

			} else if (Type == XmpNodeType.Seq) {
				var node = XmpTag.CreateNode (parent.OwnerDocument, Name, Namespace);
				// TODO: Add all qualifiers.
				if (QualifierCount > 0)
					throw new NotImplementedException ();
				var bag = XmpTag.CreateNode (parent.OwnerDocument, XmpTag.SEQ_URI, XmpTag.RDF_NS);
				foreach (var child in Children)
					child.RenderInto (bag);
				node.AppendChild (bag);
				parent.AppendChild (node);

			} else {
				// Probably some combination of things we don't fully cover yet.
				Dump ();
				throw new NotImplementedException ();
			}
		}


#endregion

#region Internal Methods

		internal void Dump (string prefix) {
			Console.WriteLine ("{0}{1}{2} ({4}) = \"{3}\"", prefix, Namespace, Name, Value, Type);
			if (qualifiers != null) {
				Console.WriteLine ("{0}Qualifiers:", prefix);

				foreach (string ns in qualifiers.Keys) {
					foreach (string name in qualifiers [ns].Keys) {
						qualifiers [ns][name].Dump (prefix+"  ->  ");
					}
				}
			}
			if (children != null) {
				Console.WriteLine ("{0}Children:", prefix);

				foreach (XmpNode child in children) {
					child.Dump (prefix+"  ->  ");
				}
			}
		}

#endregion

#region Private Methods

		/// <summary>
		///    Is this a node that we can transform into an attribute of the
		///    parent node? Yes if it has no qualifiers or children, nor is
		///    it part of a list.
		/// </summary>
		private bool IsReallySimpleType {
			get {
				return Type == XmpNodeType.Simple && (children == null || children.Count == 0)
					&& QualifierCount == 0 && (Name != XmpTag.LI_URI || Namespace != XmpTag.RDF_NS);
			}
		}

		/// <summary>
		///    Is this the root node of the tree?
		/// </summary>
		private bool IsRootNode {
			get { return Name == String.Empty && Namespace == String.Empty; }
		}

		private void AddAllQualifiersTo (XmlNode xml)
		{
			if (qualifiers == null)
				return;
			foreach (var collection in qualifiers.Values) {
				foreach (XmpNode node in collection.Values) {
					XmlAttribute attr = XmpTag.CreateAttribute (xml.OwnerDocument, node.Name, node.Namespace);
					attr.Value = node.Value;
					xml.Attributes.Append (attr);
				}
			}
		}

		private void AddAllChildrenTo (XmlNode parent)
		{
			if (children == null)
				return;
			foreach (var child in children)
				child.RenderInto (parent);
		}
#endregion


	}
}
