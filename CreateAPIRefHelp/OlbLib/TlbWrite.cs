using DatabaseIO;
using MdxUtil;
using MyXmlDoc;
using Microsoft.VisualBasic;
using SyntaxGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;

namespace OlbLib
{
  public static class TlbWrite
  {
    /// <summary>
    /// Generates and writes Markdown documentation files for the specified type library collection, including interfaces,
    /// classes, and constants, to the designated output directory.
    /// </summary>
    /// <remarks>This method creates a subdirectory for the specified library and generates Markdown files for its
    /// interfaces, classes, and constants. Existing files with the same names will be overwritten. Ensure that the output
    /// directory is accessible and that the library name is valid for use as a folder name.</remarks>
    /// <param name="tlbLibCollection">The collection of type libraries containing the metadata to be documented.</param>
    /// <param name="outputRoot">The root directory where the generated Markdown files will be created. If the target subdirectory does not exist, it
    /// will be created.</param>
    /// <param name="namespaceName">The namespace for which documentation files are generated. This is used to organize output and as a
    /// namespace in the documentation.</param>
    public static void WriteMDXFiles(TlbLibCollection tlbLibCollection, string outputRoot, string namespaceName)
    {
      // create a sub folder if it doesn't exist
      var mdxFolder = Path.Combine(outputRoot, namespaceName);
      if (!Directory.Exists(mdxFolder))
      {
        Directory.CreateDirectory(mdxFolder);
      }
      var namespaceFileName = $@"{MdxUtil.MdxUtil.CreateMemberName("_Namespace", namespaceName)}{MdxUtil.MdxUtil.MdxExtension}";
      TocStore.AddTocEntry(0, namespaceName, Path.Combine(namespaceName, namespaceFileName));


      // namespace replacement strings: $namespace$,$namespaceRemarks$,$InterfaceDescriptions$,$ClassDescriptions$,$EnumDescriptions$

      var sbNamespace = new StringBuilder(MdxUtil.MdxUtil.PageComNamespaceTemplate);
      sbNamespace.Replace("$namespace$", MdxUtil.MdxUtil.GetShortLibName(namespaceName));
      var shortDescription = ModifyDatabase.GetShortDesc(MdxUtil.MdxUtil.GetShortLibName(namespaceName));
      sbNamespace.Replace("$namespaceRemarks$", shortDescription);
      var docLibraries = ModifyDatabase.GetDocumentationFromDb(MdxUtil.MdxUtil.GetShortLibName(namespaceName), namespaceName, "Libraries");
      // write all interfaces
      TocStore.AddTocEntry(1, "Interfaces", string.Empty);
      foreach (var interfaceInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].InterfaceInfos)
      {
        var interfaceFilename = $@"{MdxUtil.MdxUtil.CreateMemberName("", interfaceInfo.Name)}{MdxUtil.MdxUtil.MdxExtension}";
        WriteInterfaceMDXFiles(tlbLibCollection, outputRoot, interfaceFilename, namespaceName, interfaceInfo);
        TocStore.AddTocEntry(2, interfaceInfo.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, interfaceInfo.Name));
      }
      sbNamespace.Replace("$interfaces$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].InterfaceInfos.Select(i => (i.Name, i.HelpString)).ToList()));

      // write all classes
      TocStore.AddTocEntry(1, "CoClasses", string.Empty);
      foreach (var classInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].CoClassInfos)
      {
        var classFilename = $@"{MdxUtil.MdxUtil.CreateMemberName("", classInfo.Name)}{MdxUtil.MdxUtil.MdxExtension}";
        WriteClassesMDXFiles(tlbLibCollection, outputRoot, classFilename, namespaceName, classInfo);
        TocStore.AddTocEntry(2, classInfo.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, classInfo.Name));
      }
      sbNamespace.Replace("$Classes$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].CoClassInfos.Select(i => (i.Name, i.HelpString)).ToList()));

      // write all constants (enums)
      TocStore.AddTocEntry(1, "Constants", string.Empty);
      foreach (var constantInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].ConstantInfos)
      {
        var constantFilename = $@"{MdxUtil.MdxUtil.CreateMemberName("", constantInfo.Name)}{MdxUtil.MdxUtil.MdxExtension}";
        WriteEnumMDXFiles(tlbLibCollection, outputRoot, constantFilename, namespaceName, constantInfo);
        TocStore.AddTocEntry(2, constantInfo.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, constantInfo.Name));
      }
      sbNamespace.Replace("$Enumerations$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].ConstantInfos.Select(i => (i.Name, i.HelpString)).ToList()));

      // save the library mdx file
      File.WriteAllText(Path.Combine(mdxFolder, namespaceFileName), sbNamespace.ToString());
      // write the TOC file into the outputRoot folder
      TocStore.WriteTocAsXml(Path.Combine(outputRoot, TocStore.TocFilename));
    }

    /// <summary>
    /// Generates and writes the XML documentation file the specified type library collection, including interfaces,
    /// classes, and constants, to an XML file.
    /// </summary>
    /// <remarks>This method creates a subdirectory for the specified library and generates XML files for its
    /// interfaces, classes, and constants. Existing files with the same names will be overwritten. Ensure that the output
    /// directory is accessible and that the library name is valid for use as a folder name.</remarks>
    /// <param name="tlbLibCollection">The collection of type libraries containing the metadata to be documented.</param>
    /// <param name="outputRoot">The root directory where the generated XML file will be created.</param>
    /// <param name="namespaceName">The namespace for which documentation files are generated. This is used to organize output and as a
    /// namespace in the documentation.</param>
    public static void WriteXmlDocument(TlbLibCollection tlbLibCollection, string outputRoot, string namespaceName)
    {
      // make the first character of the namespace name uppercase
      var myNamespace = char.ToUpper(namespaceName[0]) + namespaceName.Substring(1);
      // using tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary] to get the current library
      var currentLib = tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary];
      var dllFileName = Path.GetFileNameWithoutExtension(currentLib.AssemblyName);
      var outputDocFile = Path.Combine(outputRoot, $"{dllFileName}.xml");
      // create XML DocumentFile format same as the one generated by Visual Studio for C# projects with the GenerateDocumentationFile option enabled
      // create the root element
      var members = new XElement("members");
      // add members for interfaces
      foreach (var interfaceInfo in currentLib.InterfaceInfos) {
        WriteInterfaceXMLDocument(members, interfaceInfo);
      }
      // add members for classes
      foreach (var classInfo in currentLib.CoClassInfos)
      {
        WriteClassXMLDocument(members, classInfo);
      }
      // add members for constants (enums)
      foreach (var constantInfo in currentLib.ConstantInfos)
      {
        WriteEnumXMLDocument(members, constantInfo);
      }
      var xmlDoc = new XDocument(
        new XElement("doc",
          new XElement("assembly",
            new XElement("name", myNamespace)
          ),
          members)
      );
      xmlDoc.Save(outputDocFile);
    }


    private static void WriteInterfaceXMLDocument(XElement members, TlbInterfaceInfo interfaceInfo)
    {
      var interfaceMember = new XElement("member",
          new XAttribute("name", $"T:{interfaceInfo.Namespace}.{interfaceInfo.Name}"),
          new XElement("summary", interfaceInfo.HelpString)
      );
      members.Add(interfaceMember);
      // Add all members of the class to the XML document
      foreach (var member in interfaceInfo.Members)
      {
        var memberNode = new XElement("member",
                new XAttribute("name", MemberHelper.ToXmlDocMemberName(member)),
                new XElement("summary", member.HelpString)
              );
        var docMemberLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        MakeRemarkNode(memberNode, docMemberLibraries.Remarks);
        MakeCodeNode(memberNode, "Sample Snippet", docMemberLibraries.CSharp);
        members.Add(memberNode);
      }
    }


    private static void WriteClassXMLDocument(XElement members, TlbCoClassInfo classInfo)
    {
      var classMember = new XElement("member",
                new XAttribute("name", $"T:{classInfo.Namespace}.{classInfo.Name}"),
                new XElement("summary", classInfo.HelpString)
              );
      {
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(classInfo.FullName, classInfo.Name);
        MakeRemarkNode(classMember, docLibraries.Remarks);
      }
      members.Add(classMember);
      // Add all members of the class to the XML document
      foreach (var member in classInfo.Members)
      {
        var memberNode = new XElement("member",
                new XAttribute("name", MemberHelper.ToXmlDocMemberName(member)),
                new XElement("summary", member.HelpString)
              );
        var docMemberLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        MakeRemarkNode(memberNode, docMemberLibraries.Remarks);
        MakeCodeNode(memberNode, "Sample Snippet", docMemberLibraries.CSharp);
        members.Add(memberNode);
      }
    }

    private static void WriteEnumXMLDocument(XElement members, TlbConstantInfo constantInfo)
    {
      var enumMemberName = $"{constantInfo.Namespace}.{constantInfo.Name}";
      var enumMember = new XElement("member",
                  new XAttribute("name", $@"T:{enumMemberName}"),
                  new XElement("summary", constantInfo.HelpString)
                ); 
      // no remarks:MakeRemarkNode(enumMember, constantInfo.Remarks);
      members.Add(enumMember);
      // add the enum values to the XML document
      foreach (var variable in constantInfo.Variables)
      {
        var enumValueMember = new XElement("member", 
                  new XAttribute("name", $"F:{enumMemberName}.{variable.Name}"),
                  new XElement("summary", $@"{variable.Value}: {variable.HelpString}")
                );
        members.Add(enumValueMember);
      }
    }

    /// <summary>
    /// The expected output should look like this under the member node:
    /// &lt;example&gt;
    /// codeTitle
    /// ```csharp
    /// code
    /// ```
    /// &lt;/example&gt;   
    /// </summary>
    /// <param name="sourceMember">the XML element representing the member</param>
    /// <param name="codeTitle">the title of the code snippet</param>
    /// <param name="code">the code snippet</param>
    /// <param name="language">csharp</param>
    private static void MakeCodeNode (XElement sourceMember, string codeTitle, string code, string language = "csharp")
    {
      var exampleNode = new XElement("example");
      var titleNode = new XElement("codeTitle", codeTitle);
      exampleNode.Add(titleNode);
      var codeNode = new XElement("code", code);
      codeNode.SetAttributeValue("language", language);
      exampleNode.Add(codeNode);
      sourceMember.Add(exampleNode);
    }

    /// <summary>
    /// The expected output should look like this under the member node:
    /// &lt;remarks&gt;
    /// remarks
    /// &lt;/remarks&gt;   
    /// </summary>
    /// <param name="sourceMember">the XML element representing the member</param>
    /// <param name="remark">the remark text</param>
    private static void MakeRemarkNode(XElement sourceMember, string remarks)
    {
    if (!string.IsNullOrEmpty(remarks))
      {
        var remarksNode = new XElement("remarks", remarks);
        sourceMember.Add(remarksNode);
      }
    }

    private static void WriteInterfaceMDXFiles(TlbLibCollection tlbLibCollection, 
      string outputRoot, string interfaceFilename, 
      string libraryName, TlbInterfaceInfo interfaceInfo)
    {
      // get the interface MDX template
      var sbInterfaceMDX = new StringBuilder(MdxUtil.MdxUtil.PageInterfaceTemplate);
      // replace $Interface$ with the interface name
      sbInterfaceMDX.Replace("$Interface$", interfaceInfo.Name);
      // replace $InterfaceDescription$ with the interface help string
      sbInterfaceMDX.Replace("$InterfaceDescription$", interfaceInfo.HelpString);
      sbInterfaceMDX.Replace("$InterfaceRemarks$", string.Empty);
      // load any interface documentation / remarks
      var docILibraries = ModifyDatabase.GetDocumentationFromDb(interfaceInfo.FullName, interfaceInfo.FullName);
      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      var sbMembersDetails = new StringBuilder();
      foreach (var member in interfaceInfo.Members)
      {
        // $MemberNameId$, $MemberName$, $MemberDescription$, $SyntaxVB$, $SyntaxCS$, $MemberRemarks$
        //memberDict[member.Name] = (member.HelpString, member.MemberType.ToString());
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.MemberDetailTemplate);
        var memberId = MdxUtil.MdxUtil.ToMarkdownAnchor(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDescription$", member.HelpString);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax.Replace("\n", "").Replace("&nbsp;", " ")); // Remove any carriage return that could be in the string retrieved
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax.Replace("\n", "").Replace("&nbsp;", " ")); // Remove any carriage return that could be in the string retrieved
        sbMembersDetails.Append(sbMember);
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        // replace: $MemberRemarks$ and $MemberSamples$
        // write out the interface remarks, write $NameId$ and $Remarks$
        var sbRemarks = new StringBuilder();
        if (!string.IsNullOrEmpty(docLibraries.Remarks))
        {
          sbRemarks.Append(MdxUtil.MdxUtil.RemarksTemplate);
          sbRemarks.Replace("$NameId$", memberId);
          sbRemarks.Replace("$Remarks$", docLibraries.Remarks);
        }
        sbMembersDetails.Replace("$MemberRemarks$", sbRemarks.ToString());
        // write out any sample code for the interface $language$ language-csharp, language-vbnet, $sampleCode$
        var sbSamples = new StringBuilder();
        if (docLibraries.HasSampleCode)
        {
          if (!string.IsNullOrEmpty(docLibraries.CSharp))
          {
            sbSamples.Append(MdxUtil.MdxUtil.SampleTemplate);
            sbSamples.Replace("$language$", "language-csharp");
            sbSamples.Replace("$sampleCode$", docLibraries.CSharp);
          }
          if (!string.IsNullOrEmpty(docLibraries.VbNet))
          {
            sbSamples.Append(MdxUtil.MdxUtil.SampleTemplate);
            sbSamples.Replace("$language$", "language-vbnet");
            sbSamples.Replace("$sampleCode$", docLibraries.VbNet);
          }
        }
        sbMembersDetails.Replace("$MemberSamples$", sbSamples.ToString());

        var invokeKind = "";
        invokeKind = member.InvokeKinds switch
        {
          1 => "Method",
          2 => "ReadOnly",
          4 => "WriteOnly",
          8 => "PutRefOnly",
          6 => "ReadWrite",
          10 or 14 => "ReadPutRef",
          16 => "Event",
          _ => throw (new NotImplementedException($"InvokeKind {member.InvokeKind} not implemented")),
        };
        var oneColumn = (invokeKind, member.Name, member.HelpString);
        memberList.Add(oneColumn);
      }
      var sbMembersTable = MdxUtil.MdxUtil.MemberHeader3Template + MdxUtil.MdxUtil.CreateThreeColumnTable(libraryName, "", memberList);
      sbInterfaceMDX.Replace("$Members$", sbMembersTable);
      // replace $MembersDetails$ with the member details;
      sbInterfaceMDX.Replace("$MembersDetails$", sbMembersDetails.ToString());

      // write out all CoClasses that implement this interface
      List<(string Name, string Description)> tableContent = [];
      foreach (var coClass in interfaceInfo.CoClasses)
      {
        tableContent.Add((coClass.Name, coClass.HelpString));
      }
      var sbInterfacesTable = MdxUtil.MdxUtil.MethodHeaderTemplate + MdxUtil.MdxUtil.CreateTwoColumnTable(libraryName, "", tableContent);
      sbInterfaceMDX.Replace("$ClassDescriptions$", sbInterfacesTable);

      // write out the interface remarks, write $NameId$ and $Remarks$
      var sbInterfaceRemarks = new StringBuilder();
      if (!string.IsNullOrEmpty(docILibraries.Remarks))
      {
        sbInterfaceRemarks.Append(MdxUtil.MdxUtil.RemarksTemplate);
        sbInterfaceRemarks.Replace("$NameId$", MdxUtil.MdxUtil.ToMarkdownAnchor(interfaceInfo.Name));
        sbInterfaceRemarks.Replace("$Remarks$", docILibraries.Remarks);
      }
      // replace $MemberRemarks$
      sbInterfaceMDX.Replace("$MemberRemarks$", sbInterfaceRemarks.ToString());

      var sbInterfaceSamples = new StringBuilder();
      // write out any sample code for the interface $language$ language-csharp, language-vbnet, $sampleCode$
      if (docILibraries.HasSampleCode)
      {
        if (!string.IsNullOrEmpty(docILibraries.CSharp))
        {
          sbInterfaceSamples.Append(MdxUtil.MdxUtil.SampleTemplate);
          sbInterfaceSamples.Replace("$language$", "language-csharp");
          sbInterfaceSamples.Replace("$sampleCode$", docILibraries.CSharp);
        }
        if (!string.IsNullOrEmpty(docILibraries.VbNet))
        {
          sbInterfaceSamples.Append(MdxUtil.MdxUtil.SampleTemplate);
          sbInterfaceSamples.Replace("$language$", "language-vbnet");
          sbInterfaceSamples.Replace("$sampleCode$", docILibraries.VbNet);
        }
      }
      // replace $MemberSamples$
      sbInterfaceMDX.Replace("$MemberSamples$", sbInterfaceSamples.ToString());

      // write the interface MDX file
      File.WriteAllText(Path.Combine(outputRoot, libraryName, interfaceFilename), sbInterfaceMDX.ToString());
    }

    
    private static void WriteClassesMDXFiles(TlbLibCollection tlbLibCollection, string outputRoot, string coclassFilename, string libraryName, TlbCoClassInfo classInfo)
    {
      //  $coclass$
      //  $coclassRemarks$
      //  $InterfaceDescriptions$
      //  $Members$
      //  $MembersDetails$
      // get the class MXD template
      var sbClasses = new System.Text.StringBuilder(MdxUtil.MdxUtil.PageCoClassTemplate);

      // replace $coclass$ with the coclass name
      sbClasses.Replace("$coclass$", classInfo.Name);
      // replace $ClassDescription$ with the class help string
      sbClasses.Replace("$coclassRemarks$", classInfo.HelpString);

      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      var sbMembersDetails = new StringBuilder();
      foreach (var member in classInfo.Members)
      {
        // $MemberNameId$, $MemberName$, $MemberDesciption$, $SyntaxVB$, $SyntaxCS$, $MemberRemarks$
        //memberDict[member.Name] = (member.HelpString, member.MemberType.ToString());
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.MemberDetailTemplate);
        var memberId = MdxUtil.MdxUtil.ToMarkdownAnchor(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDescription$", member.HelpString);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax.Replace("\n", "").Replace("&nbsp;", " ")); // Remove any carriage return that could be in the string retrieved
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax.Replace("\n", "").Replace("&nbsp;", " ")); // Remove any carriage return that could be in the string retrieved
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        if (!string.IsNullOrEmpty(docLibraries.Remarks))
        {
          sbMember.Replace("$MemberRemarks$", docLibraries.Remarks);
        }
        else sbMember.Replace("$MemberRemarks$", string.Empty);
        sbMembersDetails.Append(sbMember);
        var invokeKind = "";
        invokeKind = member.InvokeKinds switch
        {
          1 => "Method",
          2 => "ReadOnly",
          4 => "WriteOnly",
          8 => "PutRefOnly",
          6 => "ReadWrite",
          10 or 14 => "ReadPutRef",
          16 => "Event",
          _ => throw (new NotImplementedException($"InvokeKind {member.InvokeKind} not implemented")),
        };
        var oneColumn = (invokeKind, member.Name, member.HelpString);
        memberList.Add(oneColumn);
      }
      var sbMembersTable = MdxUtil.MdxUtil.CreateThreeColumnTable(libraryName, "", memberList);
      sbClasses.Replace("$Members$", sbMembersTable);
      // replace $MembersDetails$ with the member details;
      sbClasses.Replace("$MembersDetails$", sbMembersDetails.ToString());

      // write out all Classes that implement this interface
      List<(string Name, string Description)> tableContent = [];
      foreach (var implementedInterface in classInfo.ImplementedInterfaceInfos)
      {
        tableContent.Add((implementedInterface.Name, implementedInterface.HelpString));
      }
      var sbClassesTable = MdxUtil.MdxUtil.MethodHeaderTemplate + MdxUtil.MdxUtil.CreateTwoColumnTable(libraryName, "", tableContent);
      sbClasses.Replace("$InterfaceDescriptions$", sbClassesTable);
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface MDX file
      System.IO.File.WriteAllText(System.IO.Path.Combine(outputRoot, libraryName, coclassFilename), sbClasses.ToString());
    }
  
    private static void WriteEnumMDXFiles(TlbLibCollection tlbLibCollection, string outputRoot, string enumFilename, string libraryName, TlbConstantInfo constantInfo)
    {
      // $Enum$ Constants
      // $EnumDescription$
      // $EnumRemarks$
      // $Constants$
      // get the constant MDX template
      var sbConstants = new System.Text.StringBuilder(MdxUtil.MdxUtil.PageEnumTemplate);
      // replace $Enum$ with the constant name
      sbConstants.Replace("$Enum$", constantInfo.Name);
      // replace $EnumDescription$ with the constant help string
      sbConstants.Replace("$EnumDescription$", constantInfo.HelpString);
      // replace $EnumRemarks$ with the constant help string
      sbConstants.Replace("$EnumRemarks$", string.Empty);
      // replace $Constants$ with a three column table of members
      List<(string Constant, string Value, string Description)> enumList = [];
      foreach (var variable in constantInfo.Variables)
      {
        enumList.Add((variable.Name, variable.Value, variable.HelpString));
      }
      var sbEnumTable = MdxUtil.MdxUtil.EnumHeaderTemplate + MdxUtil.MdxUtil.CreateThreeSimpleColumnTable(libraryName, "Constant", enumList);
      sbConstants.Replace("$Constants$", sbEnumTable);

      // write the interface MDX file
      File.WriteAllText(Path.Combine(outputRoot, libraryName, enumFilename), sbConstants.ToString());
    }


    /* old code for writing classes to html files */
    /* ========================================== */

    /// <summary>
    /// Write out the html files for an interface, CoClass, or Constant 
    /// replace the appropriate replacement strings in the template: $Interface$, $InterfaceRemarks$, $Members$, $MembersDetails$, $MemberNameId$, $Remarks$
    /// </summary>
    /// <param name="tlbLibCollection"></param>
    /// <param name="outputRoot"></param>
    /// <param name="interfaceFilename"></param>
    /// <param name="libraryName"></param>
    /// <param name="interfaceInfo"></param>
    /// <param name="namespacePrefix"></param>
    /// <param name="releaseVersion"></param>
    /// <exception cref="NotImplementedException"></exception>
    /*
    private static void WriteInterfaceHtmlFiles(TlbLibCollection tlbLibCollection, string outputRoot, string interfaceFilename, string libraryName, TlbInterfaceInfo interfaceInfo)
    {
      // get the interface html template
      var sbInterfaceHtml = new System.Text.StringBuilder(MdxUtil.MdxUtil.TlbInterfaceTemplate);
      // replace $Interface$ with the interface name
      sbInterfaceHtml.Replace("$Interface$", interfaceInfo.Name);
      // replace $InterfaceRemarks$ with the interface help string
      sbInterfaceHtml.Replace("$InterfaceRemarks$", interfaceInfo.HelpString);
      // load any interface documentation / remarks
      var docILibraries = ModifyDatabase.GetDocumentationFromDb(interfaceInfo.FullName, interfaceInfo.FullName);
      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      var sbMembersDetails = new StringBuilder();
      foreach (var member in interfaceInfo.Members)
      {
        // $MemberNameId$, $MemberName$, $MemberDesciption$, $SyntaxVB$, $SyntaxCS$, $MemberRemarks$
        //memberDict[member.Name] = (member.HelpString, member.MemberType.ToString());
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.TlbMembersDetailsTemplate);
        var memberId = GetMemberNameLink(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDesciption$", member.HelpString);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax);
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax);
        sbMembersDetails.Append(sbMember);
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        // replace: $MemberRemarks$ and $MemberSamples$
        // write out the interface remarks, write $NameId$ and $Remarks$
        var sbRemarks = new StringBuilder();
        if (!string.IsNullOrEmpty(docLibraries.Remarks))
        {
          sbRemarks.Append (MdxUtil.MdxUtil.TlbRemarksTemplate);
          sbRemarks.Replace("$NameId$", memberId);
          sbRemarks.Replace("$Remarks$", docLibraries.Remarks);
        }
        sbMembersDetails.Replace("$MemberRemarks$", sbRemarks.ToString());
        // write out any sample code for the interface $language$ language-csharp, language-vbnet, $sampleCode$
        var sbSamples = new StringBuilder();
        if (docLibraries.HasSampleCode)
        {
          if (!string.IsNullOrEmpty(docLibraries.CSharp))
          {
            sbSamples.Append(MdxUtil.MdxUtil.TlbSampleTemplate);
            sbSamples.Replace("$language$", "language-csharp");
            sbSamples.Replace("$sampleCode$", docLibraries.CSharp);
          }
          if (!string.IsNullOrEmpty(docLibraries.VbNet))
          {
            sbSamples.Append(MdxUtil.MdxUtil.TlbSampleTemplate);
            sbSamples.Replace("$language$", "language-vbnet");
            sbSamples.Replace("$sampleCode$", docLibraries.VbNet);
          }
        }
        sbMembersDetails.Replace("$MemberSamples$", sbSamples.ToString());

        var invokeKind = "";
        invokeKind = member.InvokeKinds switch
        {
          1 => "Method",
          2 => "ReadOnly",
          4 => "WriteOnly",
          8 => "PutRefOnly",
          6 => "ReadWrite",
          10 or 14 => "ReadPutRef",
          16 => "Event",
          _ => throw (new NotImplementedException($"InvokeKind {member.InvokeKind} not implemented")),
        };
        var oneColumn = (invokeKind, member.Name, member.HelpString);
        memberList.Add(oneColumn);
      }
      var sbMembersTable = CreateThreeColumnTable(libraryName, "Interface", memberList);
      sbInterfaceHtml.Replace("$Members$", sbMembersTable);
      // replace $MembersDetails$ with the member details;
      sbInterfaceHtml.Replace("$MembersDetails$", sbMembersDetails.ToString());

      // write out all CoClasses thet implement this interface
      List<(string Name, string Description)> tableContent = [];
      foreach (var coClass in interfaceInfo.CoClasses)
      {
        tableContent.Add((coClass.Name, coClass.HelpString));
      }
      var sbInterfacesTable = CreateTwoColumnTable(libraryName, "Class", tableContent);
      sbInterfaceHtml.Replace("$ClassDescriptions$", sbInterfacesTable);

      // write out the interface remarks, write $NameId$ and $Remarks$
      var sbInterfaceRemarks = new StringBuilder();
      if (!string.IsNullOrEmpty(docILibraries.Remarks))
      {
        sbInterfaceRemarks.Append(MdxUtil.MdxUtil.TlbRemarksTemplate);
        sbInterfaceRemarks.Replace("$NameId$", GetMemberNameLink(interfaceInfo.Name));
        sbInterfaceRemarks.Replace("$Remarks$", docILibraries.Remarks);
      }
      // replace $MemberRemarks$
      sbInterfaceHtml.Replace("$MemberRemarks$", sbInterfaceRemarks.ToString());

      var sbInterfaceSamples = new StringBuilder();
      // write out any sample code for the interface $language$ language-csharp, language-vbnet, $sampleCode$
      if (docILibraries.HasSampleCode)
      {
        if (!string.IsNullOrEmpty(docILibraries.CSharp))
        {
          sbInterfaceSamples.Append(MdxUtil.MdxUtil.TlbSampleTemplate);
          sbInterfaceSamples.Replace("$language$", "language-csharp");
          sbInterfaceSamples.Replace("$sampleCode$", docILibraries.CSharp);
        }
        if (!string.IsNullOrEmpty(docILibraries.VbNet))
        {
          sbInterfaceSamples.Append(MdxUtil.MdxUtil.TlbSampleTemplate);
          sbInterfaceSamples.Replace("$language$", "language-vbnet");
          sbInterfaceSamples.Replace("$sampleCode$", docILibraries.VbNet);
        }
      }
      // replace $MemberSamples$
      sbInterfaceHtml.Replace("$MemberSamples$", sbInterfaceSamples.ToString());

      // write the interface html file
      System.IO.File.WriteAllText(System.IO.Path.Combine(outputRoot, libraryName, interfaceFilename), sbInterfaceHtml.ToString());
    }
    */

    /// <summary>
    /// Write out the html files for an interface, CoClass, or Constant 
    /// replace the appropriate replacement strings in the template: $Interface$, $InterfaceRemarks$, $Members$, $MembersDetails$, $MemberNameId$, $Remarks$
    /// </summary>
    /// <param name="tlbLibCollection"></param>
    /// <param name="outputRoot"></param>
    /// <param name="interfaceFilename"></param>
    /// <param name="libraryName"></param>
    /// <param name="coclassInfo"></param>
    /// <param name="namespacePrefix"></param>
    /// <param name="releaseVersion"></param>
    /// <exception cref="NotImplementedException"></exception>
    /*
    private static void WriteClassesHtmlFiles(TlbLibCollection tlbLibCollection, string outputRoot, string coclassFilename, string libraryName, TlbCoClassInfo classInfo)
    {
      //  $coclass$
      //  $coclassRemarks$
      //  $InterfaceDescriptions$
      //  $Members$
      //  $MembersDetails$
      // get the Coclass html template
      var sbClasses = new System.Text.StringBuilder(MdxUtil.MdxUtil.TlbCoClassTemplate);
      // replace $coclass$ with the coclass name
      sbClasses.Replace("$coclass$", classInfo.Name);
      // replace $coclassRemarks$ with the coclass help string
      sbClasses.Replace("$coclassRemarks$", classInfo.HelpString);
      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      var sbMembersDetails = new StringBuilder();
      foreach (var member in classInfo.Members)
      {
        // $MemberNameId$, $MemberName$, $MemberDesciption$, $SyntaxVB$, $SyntaxCS$, $MemberRemarks$
        //memberDict[member.Name] = (member.HelpString, member.MemberType.ToString());
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.TlbMembersDetailsTemplate);
        var memberId = GetMemberNameLink(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDesciption$", member.HelpString);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax);
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax);
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        if (!string.IsNullOrEmpty(docLibraries.Remarks))
        {
          sbMember.Replace("$MemberRemarks$", docLibraries.Remarks);
        }
        else sbMember.Replace("$MemberRemarks$", string.Empty);
        sbMembersDetails.Append(sbMember);
        var invokeKind = "";
        switch (member.InvokeKinds)
        {
          case 1:
            invokeKind = "Method";
            break;
          case 2:
            invokeKind = "ReadOnly";
            break;
          case 4:
            invokeKind = "WriteOnly";
            break;
          case 8:
            invokeKind = "PutRefOnly";
            break;
          case 6:
            invokeKind = "ReadWrite";
            break;
          case 10:
          case 14:
            invokeKind = "ReadPutRef";
            break;
          case 16:
            invokeKind = "Event";
            break;
          default:
            throw (new NotImplementedException($"InvokeKind {member.InvokeKind} not implemented"));
        }
        var oneColumn = (invokeKind, member.Name, member.HelpString);
        memberList.Add(oneColumn);
      }
      var sbMembersTable = CreateThreeColumnTable(libraryName, "Interface", memberList);
      sbClasses.Replace("$Members$", sbMembersTable);
      // replace $MembersDetails$ with the member details;
      sbClasses.Replace("$MembersDetails$", sbMembersDetails.ToString());

      // write out all CoClasses thet implement this interface
      List<(string Name, string Description)> tableContent = [];
      foreach (var implementedInterface in classInfo.ImplementedInterfaceInfos)
      {
        tableContent.Add((implementedInterface.Name, implementedInterface.HelpString));
      }
      var sbClassesTable = CreateTwoColumnTable(libraryName, "Interface", tableContent);
      sbClasses.Replace("$InterfaceDescriptions$", sbClassesTable);
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface html file
      System.IO.File.WriteAllText(System.IO.Path.Combine(outputRoot, libraryName, coclassFilename), sbClasses.ToString());
    }
    */

    /// <summary>
    /// Write out the html files for an enum
    /// replace the appropriate replacement strings in the template: $Interface$, $InterfaceRemarks$, $Members$, $MembersDetails$, $MemberNameId$, $Remarks$
    /// </summary>
    /// <param name="tlbLibCollection"></param>
    /// <param name="outputRoot"></param>
    /// <param name="interfaceFilename"></param>
    /// <param name="libraryName"></param>
    /// <param name="constantInfo"></param>
    /// <param name="namespacePrefix"></param>
    /// <param name="releaseVersion"></param>
    /// <exception cref="NotImplementedException"></exception>
    /*
    private static void WriteEnumHtmlFiles(TlbLibCollection tlbLibCollection, string outputRoot, string coclassFilename, string libraryName, TlbConstantInfo constantInfo)
    {
      //  $enum$
      //  $coclassRemarks$
      //  $InterfaceDescriptions$
      //  $Members$
      //  $MembersDetails$
      // get the Coclass html template
      var sbConstants = new System.Text.StringBuilder(MdxUtil.MdxUtil.TlbEnumTemplate);
      // replace $enum$ with the constant name
      sbConstants.Replace("$enum$", constantInfo.Name);
      // replace $coclassRemarks$ with the coclass help string
      sbConstants.Replace("$enumRemarks$", constantInfo.HelpString);
      // replace $Constants$ with a three column table of members
      List<(string Constant, string Value, string Description)> enumList = [];
      foreach (var variable in constantInfo.Variables)
      {
        enumList.Add((variable.Name, variable.Value, variable.HelpString));
      }
      var sbClassesTable = CreateThreeSimpleColumnTable(libraryName, "Constant", enumList);
      sbConstants.Replace("$Constants$", sbClassesTable);

      // write the interface html file
      System.IO.File.WriteAllText(System.IO.Path.Combine(outputRoot, libraryName, coclassFilename), sbConstants.ToString());
    }
    */

    /// <summary>
    /// Writes the Htx TOC file and the namespace summary file.  The Htx table of contents file is an XML file that describes the structure of the help content.
    /// it has HelpTOC as the root node, with Library, Interface, Class, and Constant nodes as children.  All child nodes are of the type HelpTOCNode with Icon, Title, and URl attributes.
    /// </summary>
    /// <param name="tlbLibCollection">the TlbLibCollection containing the library information</param>
    /// <param name="outputRoot">location of the htx file</param>
    /// <param name="libraryName">name of the library to create the TOC for</param>
    /*
    public static void WriteHtxTocAndNamespaceFile(TlbLibCollection tlbLibCollection, string outputRoot, string libraryName)
    {
      // create an html subfolder if it doesn't exist
      var htmlFolder = System.IO.Path.Combine(outputRoot, libraryName);
      if (!System.IO.Directory.Exists(htmlFolder))
      {
        System.IO.Directory.CreateDirectory(htmlFolder);
      }
      // create the XML with HelpTOC as the root node
      XElement tocXml = new("HelpTOC");
      // write the library node
      // HelpTOCNode Title = "ESRI.ArcGIS.GeoDatabase" Url = "html\<libraryName>_library.htm" >
      var libraryFileName = $@"{CreateMemberName(libraryName, "Library")}.htm";
      var rootNode = new XElement("HelpTOCNode",
                        new XAttribute("Title", libraryName),
                        new XAttribute("Url", $@"html\{libraryFileName}.htm"));
      tocXml.Add(rootNode);
      // Library replacement strings: $namespace$,$namespaceRemarks$,$InterfaceDescriptions$,$ClassDescriptions$,$EnumDescriptions$

      var sbLibrary = new System.Text.StringBuilder(MdxUtil.MdxUtil.TlbLibraryTemplate);
      sbLibrary.Replace("$namespace$", libraryName);
      var shortDescription = ModifyDatabase.GetShortDesc(GetShortLibName(libraryName));
      sbLibrary.Replace("$namespaceRemarks$", shortDescription);
      var docLibraries = ModifyDatabase.GetDocumentationFromDb(GetShortLibName(libraryName), libraryName, "Libraries");
      if (!string.IsNullOrEmpty(docLibraries.CSharp))
        System.Diagnostics.Trace.WriteLine(docLibraries.CSharp);
      // write all interfaces
      var interfacesFilename = $@"{CreateMemberName(libraryName, "Interfaces")}.htm";
      var interfacesNode = new XElement("HelpTOCNode",
        new XAttribute("Title", "Interfaces"),
        new XAttribute("Url", GetUrlFromName(libraryName, interfacesFilename)));
      List<(string Name, string Description)> tableContent = [];
      foreach (var interfaceInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].InterfaceInfos)
      {
        var interfaceFilename = $@"{CreateMemberName(libraryName, "Interface", interfaceInfo.Name)}.htm";
        tableContent.Add((interfaceInfo.Name, interfaceInfo.HelpString));
        var interfaceNode = new XElement("HelpTOCNode",
          new XAttribute("Title", interfaceInfo.Name),
          new XAttribute("Url", GetUrlFromName(libraryName, interfaceFilename)));
        interfacesNode.Add(interfaceNode);
        WriteInterfaceHtmlFiles(tlbLibCollection, outputRoot, interfaceFilename, libraryName, interfaceInfo);
      }
      rootNode.Add(interfacesNode);
      var sbInterfacesTable = CreateTwoColumnTable(libraryName, "Interface", tableContent);
      sbLibrary.Replace("$InterfaceDescriptions$", sbInterfacesTable);
      // write all classes
      var classesFilename = $@"{CreateMemberName(libraryName, "Classes")}.htm";
      var classesNode = new XElement("HelpTOCNode",
        new XAttribute("Title", "Classes"),
        new XAttribute("Url", GetUrlFromName(libraryName, classesFilename)));
      tableContent = [];
      foreach (var classInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].ClassInfos)
      {
        var coClassFilename = $@"{CreateMemberName(libraryName, "CoClass", classInfo.Name)}.htm";
        tableContent.Add((classInfo.Name, classInfo.HelpString));
        var coClassNode = new XElement("HelpTOCNode",
          new XAttribute("Title", classInfo.Name),
          new XAttribute("Url", GetUrlFromName(libraryName, coClassFilename)));
        classesNode.Add(coClassNode);
        WriteClassesHtmlFiles(tlbLibCollection, outputRoot, coClassFilename, libraryName, classInfo);
      }
      rootNode.Add(classesNode);
      var sbCoClassesTable = CreateTwoColumnTable(libraryName, "CoClass", tableContent);
      sbLibrary.Replace("$ClassDescriptions$", sbCoClassesTable);
      var constantsFilename = $@"{CreateMemberName(libraryName, "Constants")}.htm";
      // write all constants (enums)
      var constantsNode = new XElement("HelpTOCNode",
        new XAttribute("Title", "Constants"),
        new XAttribute("Url", GetUrlFromName(libraryName, constantsFilename)));
      tableContent = [];
      foreach (var constantInfo in tlbLibCollection.Libraries[TlbLibCollection.CurrentLibrary].ConstantInfos)
      {
        var constantFilename = $@"{CreateMemberName(libraryName, "Constant", constantInfo.Name)}.htm";
        tableContent.Add((constantInfo.Name, constantInfo.HelpString));
        var constantNode = new XElement("HelpTOCNode",
          new XAttribute("Title", constantInfo.Name),
          new XAttribute("Url", GetUrlFromName(libraryName, constantFilename)));
        constantsNode.Add(constantNode);
        WriteEnumHtmlFiles(tlbLibCollection, outputRoot, constantFilename, libraryName, constantInfo);
      }
      rootNode.Add(constantsNode);
      var sbConstantsTable = CreateTwoColumnTable(libraryName, "Constant", tableContent);
      sbLibrary.Replace("$EnumDescriptions$", sbConstantsTable);
      // save the XML to the output root as <libraryName>_toc.htx
      var tocFilePath = System.IO.Path.Combine(outputRoot, $@"{libraryName}_toc.htx");
      tocXml.Save(tocFilePath);
      // save the library html file
      System.IO.File.WriteAllText(System.IO.Path.Combine(htmlFolder, libraryFileName), sbLibrary.ToString());
    }
    */

    private static string GetUrlFromName(string libraryName, string fileName)
    {
      return $@"..\{libraryName}\{fileName}.htm";
    }

    private static string GetMemberIdFromName(string memberName)
    {
      return memberName.ToLower().Replace(" ", "_");
    }
  }
}
