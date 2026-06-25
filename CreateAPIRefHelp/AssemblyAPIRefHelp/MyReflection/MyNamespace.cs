using MyXmlDoc;
using MdxUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
  public class MyNamespace : MyTypeBase
  {
    public MyNamespace(string namespaceName, List<MyTypeBase> typesInNamespace) : base(namespaceName)
    {
      KindOf = KindType.Namespace;
      Name = namespaceName;
      Namespace = namespaceName;
      GenericTypes.AddRange(typesInNamespace);
    }

    /// <summary>
    /// Write the .mdx file for the namespace, which includes the list of types in that namespace and their descriptions. The .mdx file is created in a subfolder named after the namespace under the outputRoot folder.
    /// </summary>
    /// <param name="outputRoot"></param>
    /// <param name="namespaceName"></param>
    public override void WriteMdx(string outputRoot, string namespaceName)
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

      var sbNamespace = new StringBuilder(MdxUtil.MdxUtil.PageNamespaceTemplate);
      sbNamespace.Replace("$namespace$", namespaceName);
      var shortDescription = DatabaseIO.ModifyDatabase.GetShortDesc(MdxUtil.MdxUtil.GetShortLibName(namespaceName));
      sbNamespace.Replace("$namespaceRemarks$", shortDescription);
      // create lists for interfaces, classes, and constants (enums) with their name and descriptions
      List<MyTypeBase> classes = [];
      List<MyTypeBase> interfaces = [];
      List<MyTypeBase> constants = [];
      List<(string? Name, string? Description)> lstInterfaces = [];
      List<(string? Name, string? Description)> lstClasses = [];
      List<(string? Name, string? Description)> lstConstants = [];
      foreach (var myType in GenericTypes)
      {
        var shortDesc = DatabaseIO.ModifyDatabase.GetShortDesc(myType.FullName);
        if (myType.KindOf == KindType.Interface)
        {
          lstInterfaces.Add((myType.Name, shortDesc));
          interfaces.Add(myType);
        }
        else if (myType.KindOf == KindType.Class)
        {
          lstClasses.Add((myType.Name, shortDesc));
          classes.Add(myType);
        }
        else if (myType.KindOf == KindType.Enum)
        {
          lstConstants.Add((myType.Name, shortDesc));
          constants.Add(myType);
        }
      }

      // write the interface summary table
      var interfacesFilename = $@"{MdxUtil.MdxUtil.CreateMemberName(namespaceName, "")}{MdxUtil.MdxUtil.MdxExtension}";
      sbNamespace.Replace("$interfaces$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", lstInterfaces));
      // write all interfaces
      if (interfaces.Count > 0)
      {
        TocStore.AddTocEntry(1, "Interfaces", string.Empty);
        foreach (var myInterface in interfaces)
        {
          myInterface.WriteMdx(outputRoot, namespaceName);
          TocStore.AddTocEntry(2, myInterface.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, myInterface.Name));
        }
      }

      // write the class summary table
      var classesFilename = $@"{MdxUtil.MdxUtil.CreateMemberName(namespaceName, "")}{MdxUtil.MdxUtil.MdxExtension}";
      sbNamespace.Replace("$Classes$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", lstClasses));
      // write all classes
      if (classes.Count > 0)
      {
        TocStore.AddTocEntry(1, "Classes", string.Empty);
        foreach (var myClass in classes)
        {
          myClass.WriteMdx(outputRoot, namespaceName);
          TocStore.AddTocEntry(2, myClass.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, myClass.Name));
        }
      }

      // write all constants (enums)
      var constantsFilename = $@"{MdxUtil.MdxUtil.CreateMemberName(namespaceName, "")}{MdxUtil.MdxUtil.MdxExtension}";
      sbNamespace.Replace("$Enumerations$", MdxUtil.MdxUtil.MemberHeader2Template + MdxUtil.MdxUtil.CreateTwoColumnTable(namespaceName, "", lstConstants));
      // write all constants (enums)
      if (constants.Count > 0)
      {
        TocStore.AddTocEntry(1, "Enumerations", string.Empty);
        foreach (var myConstant in constants)
        {
          myConstant.WriteMdx(outputRoot, namespaceName);
          TocStore.AddTocEntry(2, myConstant.Name, MdxUtil.MdxUtil.CreateMemberRelativePath(namespaceName, string.Empty, myConstant.Name));
        }
      }
      // save the library mdx file
      File.WriteAllText(System.IO.Path.Combine(mdxFolder, namespaceFileName), sbNamespace.ToString());
    }
  }
}
