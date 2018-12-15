/***************************************************************************
 *  XmlInjector.cs
 *
 *  Copyright (C) 2008 Brian Nickel
 *  Written by Brian Nickel (brian.nickel@gmail.com)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Xml;

public static class XmlInjector
{
	public static int Main (string [] args)
	{
		if (args.Length != 2) {
			Console.WriteLine ("XmlInjector.exe ACTIONFILE.XML ACTION_NAME");
			return 1;
		}
		
		XmlDocument doc = new XmlDocument ();
		try {
			doc.Load (args [0]);
		} catch {
			Console.WriteLine ("Could not open {0}.", args [0]);
			return 1;
		}
		
		string dir = System.IO.Directory.GetParent (args [0]).FullName;
		Console.WriteLine ("Setting working directory to {0}.", dir);
		System.IO.Directory.SetCurrentDirectory (dir);
		
		string path = string.Format ("//ActionSet[@Name='{0}']/File[@Path]", args [1]);
		foreach (XmlNode node in doc.SelectNodes (path))
			if (!RunFileAction (node))
				return 1;
		
		return 0;
	}
	
	private static bool RunFileAction (XmlNode fileElement)
	{
		string path = GetAttribute (fileElement, "Path");
		XmlDocument doc = new XmlDocument ();
		try {
			doc.Load (path);
		} catch {
			Console.WriteLine ("ERROR: Could not open {0}.", path);
			return false;
		}
		
		Console.WriteLine ("Processing {0}...", path);
		
		foreach (XmlNode element in fileElement.SelectNodes ("Replace"))
			if (!ReplaceNode (fileElement.OwnerDocument, doc, element))
				return false;
		
		foreach (XmlNode element in fileElement.SelectNodes ("Insert"))
			if (!InsertNode (fileElement.OwnerDocument, doc, element))
				return false;
		
		foreach (XmlNode element in fileElement.SelectNodes ("Remove"))
			if (!RemoveNodes (doc, element))
				return false;
		
		doc.Save (path);
		Console.WriteLine ("{0} saved.", path);
		return true;
	}
	
	private static bool ReplaceNode (XmlDocument sourceDocument,
	                                  XmlDocument targetDocument,
	                                  XmlNode replaceElement)
	{
		string sourcePath = GetAttribute (replaceElement, "Source");
		string targetPath = GetAttribute (replaceElement, "Target");
		
		if (OperationNotNeccessary (targetDocument, replaceElement)) {
			Console.WriteLine ("   Skipping replacement of {0}.", targetPath);
			return true;
		}
		
		Console.WriteLine ("   Replacing {0}.", targetPath);
		XmlNode sourceNode = sourcePath == null ? null : sourceDocument.SelectSingleNode (sourcePath);
		XmlNode targetNode = targetPath == null ? null : targetDocument.SelectSingleNode (targetPath);
		
		if (sourceNode == null)
			sourceNode = replaceElement.FirstChild;
		
		if (sourceNode == null) {
			Console.WriteLine ("ERROR: Could not find source node: {0}", sourcePath);
			return false;
		}
		
		if (targetNode == null) {
			Console.WriteLine ("ERROR: Could not find target node: {0}", targetPath);
			return false;
		}
		
		targetNode.ParentNode.ReplaceChild (targetDocument.ImportNode (sourceNode, true), targetNode);
		return true;
	}
	
	private static bool InsertNode (XmlDocument sourceDocument, XmlDocument targetDocument, XmlNode insertElement)
	{
		string sourcePath = GetAttribute (insertElement, "Source");
		string targetPath = GetAttribute (insertElement, "Target");
		
		if (OperationNotNeccessary (targetDocument, insertElement)) {
			Console.WriteLine ("   Skipping insertion into {0}.", targetPath);
			return true;
		}
		
		Console.WriteLine ("   Inserting into {0}.", targetPath);
		XmlNode sourceNode = sourcePath == null ? null : sourceDocument.SelectSingleNode (sourcePath);
		XmlNode targetNode = targetPath == null ? null : targetDocument.SelectSingleNode (targetPath);
		
		if (sourceNode == null)
			sourceNode = insertElement.FirstChild;
		
		if (sourceNode == null) {
			Console.WriteLine ("ERROR: Could not find source node: {0}", sourcePath);
			return false;
		}
		
		if (targetNode == null) {
			Console.WriteLine ("ERROR: Could not find target node: {0}", targetPath);
			return false;
		}
		
		targetNode.AppendChild (targetDocument.ImportNode (sourceNode, true));
		return true;
	}
	
	private static bool RemoveNodes (XmlDocument targetDocument,
	                                 XmlNode removeElement)
	{
		string targetPath = GetAttribute (removeElement, "Target");
		
		if (OperationNotNeccessary (targetDocument, removeElement)) {
			Console.WriteLine ("   Skipping removal of {0}.", targetPath);
			return true;
		}
		
		Console.WriteLine ("   Removing {0}.", targetPath);
		
		while (true) {
			XmlNode targetNode = targetDocument.SelectSingleNode (targetPath);
			
			if (targetNode == null)
				return true;
			
			targetNode.ParentNode.RemoveChild (targetNode);
		}
	}
	
	private static bool OperationNotNeccessary (XmlDocument targetDocument, XmlNode actionElement)
	{
		string ifMissingPath = GetAttribute (actionElement, "IfMissing");
		if (ifMissingPath != null && targetDocument.SelectSingleNode (ifMissingPath) != null)
			return true;
		
		return false;
	}
	
	private static string GetAttribute (XmlNode node, string attribute)
	{
		XmlAttribute xmlAttr = node.Attributes [attribute];
		return xmlAttr == null ? null : xmlAttr.Value;
	}
}
