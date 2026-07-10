using DatabaseIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyReflection
{
  public class MyEnum : MyTypeBase
  {
    public MyEnum(Type enumType, bool processRecursive) : base(enumType, processRecursive)
    {
      KindOf = KindType.Enum;
      var ti = IntrospectionExtensions.GetTypeInfo(enumType);

      FullName = (enumType.BaseType != null) ? $@"{enumType.BaseType.Name} {Name}" : Name;
      if (FullName.Contains("esriGraphPropertyUpdateMask"))
        System.Diagnostics.Trace.WriteLine(FullName);
      foreach (var field in ti.DeclaredFields)
      {
        if (!field.IsLiteral) continue;
        EnumParameters.Add(new MyMemberEnumField(field, this));
      }
    }

    /// <summary>
    /// Write the .mdx file for this enum, which includes the list of enum members. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteMdx(string outputRoot, string namespaceName)
    {
      // create the complete file path for the enum .mdx file
      var enumFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);
      // $Enum$ Constants
      // $EnumDescription$
      // $EnumRemarks$
      // $Constants$
      // get the constant MDX template
      var sbConstants = new StringBuilder(MdxUtil.MdxUtil.PageEnumTemplate);
      // replace $Enum$ with the constant name
      sbConstants.Replace("$Enum$", Name);
      // replace $EnumDescription$ with the constant help string
      sbConstants.Replace("$EnumDescription$", Summary);
      // replace $EnumRemarks$ with the constant help string
      sbConstants.Replace("$EnumRemarks$", Remarks);
      // replace $Constants$ with a three column table of members
      List<(string Constant, string? Value, string? Description)> enumList = [];
      foreach (var enumParam in EnumParameters)
      {
        enumList.Add((enumParam.Name, enumParam.Value, enumParam.Summary));
      }
      var sbClassesTable = MdxUtil.MdxUtil.EnumHeaderTemplate + MdxUtil.MdxUtil.CreateThreeSimpleColumnTable(namespaceName, "Constant", enumList);
      sbConstants.Replace("$Constants$", sbClassesTable);
    }

    /// <summary>
    /// Write the .xml API documentation file for this enum, which includes the list of enum members. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteXML(string outputRoot, string namespaceName)
    {
      // create the complete file path for the enum .mdx file
      var enumFilename = MdxUtil.MdxUtil.CreateMemberFilePath(outputRoot, namespaceName, Name);
      // $Enum$ Constants
      // $EnumDescription$
      // $EnumRemarks$
      // $Constants$
      // get the constant MDX template
      var sbConstants = new StringBuilder(MdxUtil.MdxUtil.PageEnumTemplate);
      // replace $Enum$ with the constant name
      sbConstants.Replace("$Enum$", Name);
      // replace $EnumDescription$ with the constant help string
      sbConstants.Replace("$EnumDescription$", Summary);
      // replace $EnumRemarks$ with the constant help string
      sbConstants.Replace("$EnumRemarks$", Remarks);
      // replace $Constants$ with a three column table of members
      List<(string Constant, string? Value, string? Description)> enumList = [];
      foreach (var enumParam in EnumParameters)
      {
        enumList.Add((enumParam.Name, enumParam.Value, enumParam.Summary));
      }
      var sbClassesTable = MdxUtil.MdxUtil.EnumHeaderTemplate + MdxUtil.MdxUtil.CreateThreeSimpleColumnTable(namespaceName, "Constant", enumList);
      sbConstants.Replace("$Constants$", sbClassesTable);
    }

    public override List<XElement> CreateXML()
    {
      var members = new List<XElement>();
      var enumMemberName = $"{this.Namespace}.{this.Name}";
      var enumMember = new XElement("member",
                  new XAttribute("name", $@"T:{enumMemberName}"),
                  new XElement("summary", this.Summary)
                );
      // no remarks:MakeRemarkNode(enumMember, constantInfo.Remarks);
      members.Add(enumMember);

      Console.WriteLine($@"CreateXML: {this.Name} Enum");
      // add the enum values to the XML document
      foreach (var variable in this.EnumParameters)
      {
        var enumValueMember = new XElement("member",
                  new XAttribute("name", $"F:{enumMemberName}.{variable.Name}"),
                  new XElement("summary", $@"{variable.Value}: {variable.Summary}")
                );
        members.Add(enumValueMember);
      }
      return members;
    }

    public override string ToString()
    {
      return $@"public {FullName.Replace('+', '.')}";
    }
  }
}
