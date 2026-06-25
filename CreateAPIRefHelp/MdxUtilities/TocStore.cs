using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MdxUtil
{
  public class TocStore
  {
    private static List<(uint Level, string Title, string Url)> TocEntries = new();

    public static string TocFilename = "EnterpriseSDK_NET_TOC.xml";

    public static void AddTocEntry(uint level, string title, string url)
    {
      TocEntries.Add((level, title, url));
    }

    /// <summary>
    /// Write the content of TocEntries as XML using this format: 
    /// &lt;HelpTOCNode Title="Title" Url="Title"&gt;
    /// </summary>
    public static void WriteTocAsXml(string xmlFilePath)
    {
      var xmlDoc = new System.Xml.XmlDocument();
      if (File.Exists(xmlFilePath))
      {
        string xmlContent = File.ReadAllText(xmlFilePath, Encoding.UTF8).Trim();
        xmlDoc.LoadXml(xmlContent);       
      }
      // insert the TocEntries into the xmlDoc as the last child of the root node
      var root = xmlDoc.DocumentElement;
      if (root == null)
      {
        root = xmlDoc.CreateElement("HelpTOC");
        xmlDoc.AppendChild(root);
        // Create an XML declaration.
        XmlDeclaration xmldecl;
        xmldecl = xmlDoc.CreateXmlDeclaration("1.0", null, null);
        xmldecl.Encoding = "UTF-8";
        xmldecl.Standalone = "yes";
        // Add the new node to the document.
        xmlDoc.InsertBefore(xmldecl, root);
      }
      var appendNode = root;
      var lastLevel = 0u;
      var lastNode = root;
      foreach (var entry in TocEntries)
      {          
        var newNode = xmlDoc.CreateElement("HelpTOCNode");
        newNode.SetAttribute("Title", $@"{entry.Title}");
        newNode.SetAttribute("Url", entry.Url);
        if (entry.Level > lastLevel)
        {
          // go down one level, append to the last node
          appendNode = lastNode;
          appendNode.AppendChild(newNode);
        }
        else if (entry.Level < lastLevel)
        {
          // go up one level, append to the appropriate parent node
          if (lastNode.ParentNode == null) throw new Exception($@"No parent node found for {entry.Title}");
          appendNode = lastNode;
          for (uint i = 0; i <= lastLevel - entry.Level; i++)
          {
            if (appendNode.ParentNode == null) throw new Exception($@"No parent node found for {entry.Title}");
            appendNode = (XmlElement)appendNode.ParentNode;
          }
          appendNode.AppendChild(newNode);
        }
        else
        {
          // same level, append to the parent of the last node
          appendNode.AppendChild(newNode);
        }
        lastNode = newNode;
        lastLevel = entry.Level;
      }
      XmlWriterSettings settings = new() { Indent = true };
      using XmlWriter writer = XmlWriter.Create(xmlFilePath, settings);
      xmlDoc.Save(writer);
    }
  }
}
