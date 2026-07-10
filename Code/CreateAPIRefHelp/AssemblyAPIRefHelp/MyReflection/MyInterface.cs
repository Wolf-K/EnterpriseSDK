using DatabaseIO;
using MyXmlDoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyReflection
{
  public class MyInterface : MyTypeBase
  {
    public MyInterface(Type interfaceType, bool processRecursive) : base(interfaceType, processRecursive)
    {
      KindOf = KindType.Interface;
      if (!processRecursive) return;
      try
      {
        // Get the information related to all public members
        var myMemberInfo = interfaceType.GetMembers();
        //System.Diagnostics.Trace.WriteLine($@"{Environment.NewLine}The members of class '{interfaceType.FullName}' are :{Environment.NewLine}");
        foreach (var constructorInfo in interfaceType.GetConstructors())
        {
          if (!constructorInfo.IsPublic) continue;
          Members.Add(new MyMemberConstructor(constructorInfo, this));
        }
        foreach (var fieldInfo in interfaceType.GetFields())
        {
          if (!fieldInfo.IsPublic) continue;
          Members.Add(new MyMemberField(fieldInfo, this));
        }
        foreach (var propertyInfo in interfaceType.GetProperties())
        {
          var tmp = new MyMemberProperty(propertyInfo, this);
          if (tmp.IsUsable)
            Members.Add(tmp);
        }
        foreach (var methodInfo in interfaceType.GetMethods())
        {
          if (methodInfo == null) continue;
          if (!methodInfo.IsPublic) continue;
          //System.Diagnostics.Trace.WriteLine(methodInfo.ToString());
          if (methodInfo.IsSpecialName) continue;
          var minfo = methodInfo.ToString();
          if (minfo == null) continue;
          if (!MyConsts.ExcludeMethodSignatures.Contains(minfo))
          {
            var tmp = new MyMemberMethod(methodInfo, this);
            if (tmp.IsUsable)
              Members.Add(tmp);
          }

        }
        foreach (var eventInfo in interfaceType.GetEvents())
        {
          Members.Add(new MyMemberEvent(eventInfo, this));
        }
      }
      catch (Exception ex)
      {
        // We are missing the required dependency assembly.
        HasError = $@"GetMembers Error: {ex.Message}";
      }
    }

    /// <summary>
    /// Write the .mdx file for this interface, which includes the list of interface members. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteMdx(string outputRoot, string namespaceName)
    {
      // create the complete file path for the interface .mdx file
      var interfaceFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);

      // get the interface MXD template
      var sbInterface = new StringBuilder(MdxUtil.MdxUtil.PageInterfaceTemplate);
      // replace $Interface$ with the interface name
      sbInterface.Replace("$Interface$", Name);
      // replace $InterfaceDescription$ with the interface help string
      sbInterface.Replace("$InterfaceDescription$", Summary);
      if (string.IsNullOrEmpty(Remarks))
      {
        sbInterface.Replace("$InterfaceRemarks$", string.Empty);
      }
      else
      {
        sbInterface.Replace("$InterfaceRemarks$", Remarks);
      }

      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      List<(string Name, string Description)> constructorList = [];
      var sbMembersDetails = new StringBuilder();
      var sbConstructorDetails = new StringBuilder();
      foreach (var member in this.Members)
      {
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.MemberDetailTemplate);
        var memberId = MdxUtil.MdxUtil.ToMarkdownAnchor(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDescription$", member.Summary);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax);
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax);
        if (!string.IsNullOrEmpty(this.Remarks))
        {
          sbMember.Replace("$MemberRemarks$", this.Remarks);
        }
        else sbMember.Replace("$MemberRemarks$", string.Empty);
        sbMember.Replace("$MemberSamples$", string.Empty);
        sbMembersDetails.Append(sbMember);
        // depending on the member type we will determine the invoke kind. For method, it is always "Method". For property, it can be "ReadOnly", "WriteOnly", "ReadWrite", "PutRefOnly" or "ReadPutRef". For event, it is "Event". 
        var invokeKind = "";
        var memberType = member.GetType().Name;
        invokeKind = memberType switch
        {
          "MyMemberMethod" => "Method",
          "MyMemberProperty" => (member as MyMemberProperty).IsGetPrivate ? "WriteOnly"
                          : (member as MyMemberProperty).IsSetInternal ? "ReadOnly" : "ReadWrite",
          "MyMemberEvent" => "Event",
          _ => $"InvokeKind {memberType} not implemented",
        };
        var oneColumn = (invokeKind, member.Name, member.Summary);
        memberList.Add(oneColumn);
      }
      // members
      var sbMemberHeader = new StringBuilder(MdxUtil.MdxUtil.MemberMdTemplate);
      sbMemberHeader.Replace("$MemberList$", MdxUtil.MdxUtil.CreateThreeColumnTable(namespaceName, "", memberList));
      sbMemberHeader.Replace("$MemberDetails$", sbMembersDetails.ToString());
      sbInterface.Replace("$Members$", sbMemberHeader.ToString());
      sbInterface.Replace("$Methods$", "");
      sbInterface.Replace("$Fields$", "");
      sbInterface.Replace("$Properties$", "");
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface MDX file
      File.WriteAllText(interfaceFilename, sbInterface.ToString());
    }

    /// <summary>
    /// Write the .xml API documentation file for this interface, which includes the list of interface members. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteXML(string outputRoot, string namespaceName)
    {
      // create the complete file path for the interface .mdx file
      var interfaceFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);

      // get the interface MXD template
      var sbInterface = new StringBuilder(MdxUtil.MdxUtil.PageInterfaceTemplate);
      // replace $Interface$ with the interface name
      sbInterface.Replace("$Interface$", Name);
      // replace $InterfaceDescription$ with the interface help string
      sbInterface.Replace("$InterfaceDescription$", Summary);
      if (string.IsNullOrEmpty(Remarks))
      {
        sbInterface.Replace("$InterfaceRemarks$", string.Empty);
      }
      else
      {
        sbInterface.Replace("$InterfaceRemarks$", Remarks);
      }

      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      List<(string Name, string Description)> constructorList = [];
      var sbMembersDetails = new StringBuilder();
      var sbConstructorDetails = new StringBuilder();
      foreach (var member in this.Members)
      {
        var sbMember = new StringBuilder(MdxUtil.MdxUtil.MemberDetailTemplate);
        var memberId = MdxUtil.MdxUtil.ToMarkdownAnchor(member.Name);
        sbMember.Replace("$MemberNameId$", memberId);
        sbMember.Replace("$MemberName$", member.Name);
        sbMember.Replace("$MemberDescription$", member.Summary);
        sbMember.Replace("$SyntaxVB$", member.VbSyntax);
        sbMember.Replace("$SyntaxCS$", member.CSharpSyntax);
        if (!string.IsNullOrEmpty(this.Remarks))
        {
          sbMember.Replace("$MemberRemarks$", this.Remarks);
        }
        else sbMember.Replace("$MemberRemarks$", string.Empty);
        sbMember.Replace("$MemberSamples$", string.Empty);
        sbMembersDetails.Append(sbMember);
        // depending on the member type we will determine the invoke kind. For method, it is always "Method". For property, it can be "ReadOnly", "WriteOnly", "ReadWrite", "PutRefOnly" or "ReadPutRef". For event, it is "Event". 
        var invokeKind = "";
        var memberType = member.GetType().Name;
        invokeKind = memberType switch
        {
          "MyMemberMethod" => "Method",
          "MyMemberProperty" => (member as MyMemberProperty).IsGetPrivate ? "WriteOnly"
                          : (member as MyMemberProperty).IsSetInternal ? "ReadOnly" : "ReadWrite",
          "MyMemberEvent" => "Event",
          _ => $"InvokeKind {memberType} not implemented",
        };
        var oneColumn = (invokeKind, member.Name, member.Summary);
        memberList.Add(oneColumn);
      }
      // members
      var sbMemberHeader = new StringBuilder(MdxUtil.MdxUtil.MemberMdTemplate);
      sbMemberHeader.Replace("$MemberList$", MdxUtil.MdxUtil.CreateThreeColumnTable(namespaceName, "", memberList));
      sbMemberHeader.Replace("$MemberDetails$", sbMembersDetails.ToString());
      sbInterface.Replace("$Members$", sbMemberHeader.ToString());
      sbInterface.Replace("$Methods$", "");
      sbInterface.Replace("$Fields$", "");
      sbInterface.Replace("$Properties$", "");
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface MDX file
      File.WriteAllText(interfaceFilename, sbInterface.ToString());
    }

    public override List<XElement> CreateXML()
    {
      List<XElement> members = [];
      var interfaceMember = new XElement("member",
          new XAttribute("name", $"T:{this.Namespace}.{this.Name}"),
          new XElement("summary", this.Summary)
      );
      Console.WriteLine($@"CreateXML: {this.Name} Interface");
      members.Add(interfaceMember);
      // Add all members of the class to the XML document
      foreach (var member in this.Members)
      {
        var memberNode = new XElement("member",
                new XAttribute("name", MemberHelper.ToXmlDocMemberName(member)),
                new XElement("summary", member.Summary)
              );
        var docMemberLibraries = ModifyDatabase.GetDocumentationFromDb(member.FullName, member.PartialName);
        MakeRemarkNode(memberNode, docMemberLibraries.Remarks);
        MakeCodeNode(memberNode, "Sample Snippet", docMemberLibraries.CSharp);
        members.Add(memberNode);
      }
      return members;
    }
  }
}

