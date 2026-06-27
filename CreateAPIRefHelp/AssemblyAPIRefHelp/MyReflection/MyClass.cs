using DatabaseIO;
using MdxUtil;
using MyXmlDoc;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace MyReflection
{
  public class MyClass : MyTypeBase
  {
    public MyClass(TypeInfo classType, bool processRecursive) : base(classType, processRecursive)
    {
      KindOf = KindType.Class;
      if (FullName.Contains("ArcGIS.Core.Data.FeatureClass"))
      {
        System.Diagnostics.Trace.WriteLine(FullName);
      }
      if (!processRecursive) return;
      var state = string.Empty;
      try
      {
        // Get the information related to all public members
        var myMemberInfo = classType.DeclaredMethods;

        // Get the syntax for the class
        CSharpSyntax = MdxUtil.CSharpSyntaxGenerator.GetClassSyntax(classType);

        //System.Diagnostics.Trace.WriteLine($@"{Environment.NewLine}The members of class '{classType.FullName}' are :{Environment.NewLine}");
        // Inherited Constructors are not shown
        foreach (var constructorInfo in classType.DeclaredConstructors)
        {
          //if (constructorInfo.IsEditorBrowsableNeverOrObsolete(out (bool IsObsolete, string? ObsoleteMessage) obsolete)) continue;
          if (!constructorInfo.IsPublic) continue;
          if (constructorInfo.IsAssembly) continue;
          if (constructorInfo.HasExcludeTag()) continue;
          state = $@"processing constructor: {constructorInfo.Name}";
          Members.Add(new MyMemberConstructor(constructorInfo, this) { Obsolete = new(false, null) });
        }
        // ignore fields:

        foreach (var fieldInfo in classType.GetAllFields())
        {
          if (!fieldInfo.IsPublic) continue;
          if (fieldInfo.HasExcludeTag()) continue;
          state = $@"processing fieldInfo: {fieldInfo.Name}";
          Members.Add(new MyMemberField(fieldInfo, this));
        }

        foreach (var propertyInfo in classType.GetUniqueProperties())
        {
          //if (propertyInfo.IsEditorBrowsableNeverOrObsolete(out (bool IsObsolete, string? ObsoleteMessage) obsolete)) continue;
          state = $@"processing propertyInfo: {propertyInfo.Name}";
          // Check inheritance
          var isInherited = false;
          var baseDeclaration = propertyInfo.DeclaringType;
          if (baseDeclaration != null)
          {
            isInherited = baseDeclaration.FullName != classType.FullName;
          }
          // ignore inherited methods
          //if (isInherited) continue;
          var isPrivateGet = false;
          var isPrivateSet = false;
          var getMethod = propertyInfo.GetMethod;
          var setMethod = propertyInfo.SetMethod;
          var isGetInternal = false;
          var isSetInternal = false;
          if (getMethod != null)
          {
            isPrivateGet = getMethod.IsPrivate;
            isGetInternal = getMethod.IsAssembly;
          }
          if (setMethod != null)
          {
            isPrivateSet = setMethod.IsPrivate;
            isSetInternal = setMethod.IsAssembly;
          }
          if ($@"{classType.FullName}.{propertyInfo.Name}".Contains("Shadow"))
          {
            System.Diagnostics.Trace.WriteLine(classType.FullName);
          }
          if (getMethod != null && setMethod != null)
          {
            if (isGetInternal && isSetInternal)
            {
              //Console.WriteLine($@"*** {classType.FullName}.{propertyInfo.Name} set/get {setMethod.Attributes} {getMethod.Attributes}");
              continue;
            }
          }
          else
          {
            if (getMethod != null && isGetInternal
              || setMethod != null && isSetInternal)
            {
              //Console.WriteLine($@"*** {classType.FullName}.{propertyInfo.Name} set/get {(setMethod != null ? setMethod.Attributes : (getMethod != null ? getMethod.Attributes : "get/set are null"))}");
              continue;
            }
          }
          if (propertyInfo.HasExcludeTag()) continue;
          //string? fullName = propertyInfo.PropertyType?.FullName;
          //if (propertyInfo.Name.Contains("SmallImage", StringComparison.CurrentCultureIgnoreCase)
          //  && fullName != null && fullName.Contains ("System.Object"))
          //  System.Diagnostics.Trace.WriteLine(propertyInfo.Name);
          var tmp = new MyMemberProperty(propertyInfo, this) { Obsolete = new(false, null) };
          if (tmp.IsUsable)
            Members.Add(tmp);
        }
        MethodInfo? lastMethodInfo = null;
        var lastMethodName = string.Empty;
        var lastMethodFullname = string.Empty;
        var lastAttr = string.Empty;
        foreach (var methodInfo in classType.GetUniqueMethods())
        {
          if (methodInfo == null) continue;
          state = $@"processing methodInfo: {methodInfo.Name}";
          // Check inheritance
          var isInherited = false;
          var baseDeclaration = methodInfo.DeclaringType;
          if (baseDeclaration != null)
          {
            isInherited = baseDeclaration.FullName != classType.FullName;
          }
          // ignore inherited methods
          //if (isInherited) continue;
          //if (methodInfo.IsEditorBrowsableNeverOrObsolete(out (bool IsObsolete, string? ObsoleteMessage) obsolete)) continue;
          if (!methodInfo.IsPublic)
          {
            // the visibility of this method or constructor is described by Family;
            // that is, the method or constructor is visible only within its class and derived classes.
            if (!methodInfo.IsVirtual || !methodInfo.IsFamily)
            {
              //System.Diagnostics.Trace.WriteLine(methodInfo.Name);
              continue;
            }
          }
          if (methodInfo.IsSpecialName) continue;
          //if (methodInfo.IsHideBySig) continue;
          // is internal
          if (methodInfo.IsAssembly) continue;
          if (methodInfo.HasExcludeTag()) continue;
          var minfo = methodInfo.ToString();
          if (minfo == null) continue;
          if (!MyConsts.ExcludeMethodSignatures.Contains(minfo))
          {
            var tmp = new MyMemberMethod(methodInfo, this) { Obsolete = new(false, null) };
            if (tmp.Name == lastMethodName)
            {
              System.Diagnostics.Trace.WriteLine($@"Last: {lastMethodInfo} {lastMethodName} full: {lastMethodFullname} attr: {lastAttr}");
              System.Diagnostics.Trace.WriteLine($@"next: {methodInfo} {classType.FullName}.{tmp.Name} full: {methodInfo.DeclaringType?.FullName} attr: {methodInfo.Attributes.ToString()}");
              if (lastMethodInfo != null && methodInfo.Name.StartsWith ("GetDefinition"))
              {
                System.Diagnostics.Trace.WriteLine($@"Last: {lastMethodInfo} {lastMethodName} full: {lastMethodFullname} attr: {lastAttr}");
              }
            }
            lastMethodInfo = methodInfo;
            lastMethodName = tmp.Name;
            lastMethodFullname = methodInfo.DeclaringType?.FullName;
            lastAttr = methodInfo.Attributes.ToString();
            if (tmp.IsUsable)
              Members.Add(tmp);
            if (tmp.HasInternalParams)
            {
              System.Diagnostics.Trace.WriteLine(tmp.ToString());
            }
          }
        }
        foreach (var eventInfo in classType.GetAllEvents())
        {
          state = $@"processing eventInfo: {eventInfo.Name}";

          //if (eventInfo.IsEditorBrowsableNeverOrObsolete(out (bool IsObsolete, string? ObsoleteMessage) obsolete)) continue;
          if (eventInfo.HasExcludeTag()) continue;
          Members.Add(new MyMemberEvent(eventInfo, this) { Obsolete = new(false, null) });
        }
      }
      catch (Exception ex)
      {
        // We are missing the required dependency assembly.
        HasError = $@"GetMembers Error: {ex.Message}";
        throw new Exception($@"Class {FullName} has error while {state}: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Write the .mdx file for this class, which includes the list of members and their descriptions. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteMdx(string outputRoot, string namespaceName)
    {
      // create the complete file path for the class .mdx file
      var classFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);

      // get the class MXD template
      var sbClass = new StringBuilder(MdxUtil.MdxUtil.PageClassTemplate);

      // replace $Class$ with the class name
      sbClass.Replace("$Class$", Name);
      // replace $ClassDescription$ with the class help string
      sbClass.Replace("$ClassDescription$", Summary);
      // replace remarks
      if (string.IsNullOrEmpty(Remarks))
      {
        sbClass.Replace("$ClassRemarks$", string.Empty);
      }
      else
      {
        sbClass.Replace("$ClassRemarks$", Remarks);
      }
      // replace $ClassSyntax$
      if (string.IsNullOrEmpty(this.CSharpSyntax))
      {
        sbClass.Replace("$ClassSyntax$", string.Empty);
      }
      else
      {
        var sbSyntax = new StringBuilder(MdxUtil.MdxUtil.SyntaxTemplate);
        sbSyntax.Replace("$SyntaxCS$", this.CSharpSyntax);
        sbClass.Replace("$ClassSyntax$", sbSyntax.ToString());
      }
      // replace $ClassInheritance$
      if (string.IsNullOrEmpty(this.CSharpSyntax))
      {
        sbClass.Replace("$ClassInheritance$", string.Empty);
      }
      else
      {
        var sbInheritance = new StringBuilder(MdxUtil.MdxUtil.InheritanceTemplate);
        sbInheritance.Replace("$Inheritance$", MdxUtil.InheritanceHelper.PrintInheritanceTree(this.ReflectionTypeInfo));
        sbClass.Replace("$ClassInheritance$", sbInheritance.ToString());
      }

      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      List<(string Name, string Description)> constructorList = [];
      var sbMembersDetails = new StringBuilder();
      var sbConstructorDetails = new StringBuilder();
      foreach (var member in this.Members)
      {
        // members can be constructors
        if (member is MyMemberConstructor constructor)
        {
          constructorList.Add((constructor.CSharpSyntaxShort, constructor.Summary));
          var sbConstructor = new StringBuilder(MdxUtil.MdxUtil.ConstructorDetailMdTemplate);
          sbConstructor.Replace("$ConstructorName$", constructor.CSharpSyntaxShort);
          if (constructor.CSharpSyntaxShort.Contains("CIM3DSymbolProperties"))
            System.Diagnostics.Trace.WriteLine(constructor.CSharpSyntaxShort);
          sbConstructor.Replace("$ConstructorDescription$", constructor.Summary);
          sbConstructor.Replace("$ConstructorSyntax$", constructor.CSharpSyntax);
          var sbCtorParam = new StringBuilder();
          sbConstructor.Replace("$ConstructorParameters$", sbCtorParam.ToString());
          sbConstructorDetails.Append(sbConstructor);
          continue;
        }
        // not a constructor
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
        var memberSummary = member.Summary;
        if (MdxUtil.InheritanceHelper.IsOverride(member.memberInfo))
        {
          memberSummary = "Overridden. " + memberSummary;
        }
        if (member.IsInherited)
        {
          var inheritedFrom = member.ReflectionMember?.DeclaringType?.FullName
                              ?? member.ReflectionMember?.DeclaringType?.Name
                              ?? "unknown type";
          memberSummary += $" (Inherited from {inheritedFrom})";
        }
        var oneMemberColumn = (invokeKind, member.Name, memberSummary);
        memberList.Add(oneMemberColumn);
      }
      // constructors
      var sbCtorHeader = new StringBuilder(MdxUtil.MdxUtil.ConstructorTemplate);
      sbCtorHeader.Replace("$ConstructorList$", MdxUtil.MdxUtil.CreateLocalLinkTwoColumnTable(namespaceName, "", constructorList));
      sbCtorHeader.Replace("$ConstructorDetails$", sbConstructorDetails.ToString());
      sbClass.Replace("$Constructors$", sbCtorHeader.ToString());
      // members
      var sbMemberHeader = new StringBuilder(MdxUtil.MdxUtil.MemberMdTemplate);
      sbMemberHeader.Replace("$MemberList$", MdxUtil.MdxUtil.CreateThreeColumnTable(namespaceName, "", memberList));
      sbMemberHeader.Replace("$MemberDetails$", sbMembersDetails.ToString());
      sbClass.Replace("$Members$", sbMemberHeader.ToString());
      sbClass.Replace("$Methods$", "");
      sbClass.Replace("$Fields$", "");
      sbClass.Replace("$Properties$", "");
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface MDX file
      File.WriteAllText(classFilename, sbClass.ToString());
    }

    /// <summary>
    /// Write the .xml API  documentation file for this class, which includes the list of members and their descriptions. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteXML(string outputRoot, string namespaceName)
    {
      // create the complete file path for the class .mdx file
      var classFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);

      // get the class MXD template
      var sbClass = new StringBuilder(MdxUtil.MdxUtil.PageClassTemplate);

      // replace $Class$ with the class name
      sbClass.Replace("$Class$", Name);
      // replace $ClassDescription$ with the class help string
      sbClass.Replace("$ClassDescription$", Summary);
      // replace remarks
      if (string.IsNullOrEmpty(Remarks))
      {
        sbClass.Replace("$ClassRemarks$", string.Empty);
      }
      else
      {
        sbClass.Replace("$ClassRemarks$", Remarks);
      }
      // replace $ClassSyntax$
      if (string.IsNullOrEmpty(this.CSharpSyntax))
      {
        sbClass.Replace("$ClassSyntax$", string.Empty);
      }
      else
      {
        var sbSyntax = new StringBuilder(MdxUtil.MdxUtil.SyntaxTemplate);
        sbSyntax.Replace("$SyntaxCS$", this.CSharpSyntax);
        sbClass.Replace("$ClassSyntax$", sbSyntax.ToString());
      }
      // replace $ClassInheritance$
      if (string.IsNullOrEmpty(this.CSharpSyntax))
      {
        sbClass.Replace("$ClassInheritance$", string.Empty);
      }
      else
      {
        var sbInheritance = new StringBuilder(MdxUtil.MdxUtil.InheritanceTemplate);
        sbInheritance.Replace("$Inheritance$", MdxUtil.InheritanceHelper.PrintInheritanceTree(this.ReflectionTypeInfo));
        sbClass.Replace("$ClassInheritance$", sbInheritance.ToString());
      }

      // replace $Members$ with a three column table of members
      List<(string Invoke, string Name, string Description)> memberList = [];
      List<(string Name, string Description)> constructorList = [];
      var sbMembersDetails = new StringBuilder();
      var sbConstructorDetails = new StringBuilder();
      foreach (var member in this.Members)
      {
        // members can be constructors
        if (member is MyMemberConstructor constructor)
        {
          constructorList.Add((constructor.CSharpSyntaxShort, constructor.Summary));
          var sbConstructor = new StringBuilder(MdxUtil.MdxUtil.ConstructorDetailMdTemplate);
          sbConstructor.Replace("$ConstructorName$", constructor.CSharpSyntaxShort);
          if (constructor.CSharpSyntaxShort.Contains("CIM3DSymbolProperties"))
            System.Diagnostics.Trace.WriteLine(constructor.CSharpSyntaxShort);
          sbConstructor.Replace("$ConstructorDescription$", constructor.Summary);
          sbConstructor.Replace("$ConstructorSyntax$", constructor.CSharpSyntax);
          var sbCtorParam = new StringBuilder();
          sbConstructor.Replace("$ConstructorParameters$", sbCtorParam.ToString());
          sbConstructorDetails.Append(sbConstructor);
          continue;
        }
        // not a constructor
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
        var memberSummary = member.Summary;
        if (MdxUtil.InheritanceHelper.IsOverride(member.memberInfo))
        {
          memberSummary = "Overridden. " + memberSummary;
        }
        if (member.IsInherited)
        {
          var inheritedFrom = member.ReflectionMember?.DeclaringType?.FullName
                              ?? member.ReflectionMember?.DeclaringType?.Name
                              ?? "unknown type";
          memberSummary += $" (Inherited from {inheritedFrom})";
        }
        var oneMemberColumn = (invokeKind, member.Name, memberSummary);
        memberList.Add(oneMemberColumn);
      }
      // constructors
      var sbCtorHeader = new StringBuilder(MdxUtil.MdxUtil.ConstructorTemplate);
      sbCtorHeader.Replace("$ConstructorList$", MdxUtil.MdxUtil.CreateLocalLinkTwoColumnTable(namespaceName, "", constructorList));
      sbCtorHeader.Replace("$ConstructorDetails$", sbConstructorDetails.ToString());
      sbClass.Replace("$Constructors$", sbCtorHeader.ToString());
      // members
      var sbMemberHeader = new StringBuilder(MdxUtil.MdxUtil.MemberMdTemplate);
      sbMemberHeader.Replace("$MemberList$", MdxUtil.MdxUtil.CreateThreeColumnTable(namespaceName, "", memberList));
      sbMemberHeader.Replace("$MemberDetails$", sbMembersDetails.ToString());
      sbClass.Replace("$Members$", sbMemberHeader.ToString());
      sbClass.Replace("$Methods$", "");
      sbClass.Replace("$Fields$", "");
      sbClass.Replace("$Properties$", "");
      // replace: $MemberRemarks$ and $MemberSamples$

      // write the interface MDX file
      File.WriteAllText(classFilename, sbClass.ToString());
    }

    public override List<XElement> CreateXML()
    {
      var members = new List<XElement>();
      var classMember = new XElement("member",
                new XAttribute("name", $"T:{this.Namespace}.{this.Name}"),
                new XElement("summary", this.Summary)
              );
      {
        var docLibraries = ModifyDatabase.GetDocumentationFromDb(this.FullName, this.Name);
        MakeRemarkNode(classMember, docLibraries.Remarks);
      }
      Console.WriteLine($@"CreateXML: {this.Name} Class");
      members.Add(classMember);
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
