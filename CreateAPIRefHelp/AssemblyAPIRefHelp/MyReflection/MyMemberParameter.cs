using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberParameter
    {
        public MyMemberParameter(System.Reflection.ParameterInfo paramInfo)
        {
            var paramType = paramInfo.ParameterType;
            Name = paramType.ToString();
            if (Name.StartsWith("ArcGIS"))
                System.Diagnostics.Trace.WriteLine(Name);
            IsInternal = Name.Contains(".Internal") || Name.Contains(".ServiceContracts") || Name.Contains("XamlGeneratedNamespace");
            if (IsInternal)
                System.Diagnostics.Trace.WriteLine(Name);

            foreach (var genArg in paramInfo.ParameterType.GetGenericArguments())
            {
                Name = Name.Replace($@"[{genArg.FullName}]", $@"{{{genArg.Name}}}").Replace("`1", "");
            }
            HasError = String.Empty;
            IsIn = paramInfo.IsIn;
            IsLcid = paramInfo.IsLcid;
            IsOptional = paramInfo.IsOptional;
            IsOut = paramInfo.IsOut;
            IsRetval = paramInfo.IsRetval;
            Position = paramInfo.Position;
            //var ti = IntrospectionExtensions.GetTypeInfo(paramInfo.ParameterType);
            ParameterType = new MyTypeBase(paramType, false);
            GenericsName = ParameterType.FullName;
        }

        public string Name { get; set; }
        public string GenericsName { get; set; }
        public string HasError { get; set; }

        public bool IsIn { get; set; }

        //
        // Summary:
        //     Gets a value indicating whether this parameter is a locale identifier (lcid).
        //
        // Returns:
        //     true if the parameter is a locale identifier; otherwise, false.
        public bool IsLcid { get; set; }

        public bool IsOptional { get; set; }

        public bool IsOut { get; set; }

        public bool IsRetval { get; set; }

        public bool IsInternal { get; set; }

        public MyTypeBase ParameterType { get; set; }

        //
        // Summary:
        //     Gets the zero-based position of the parameter in the formal parameter list.
        //
        // Returns:
        //     An integer representing the position this parameter occupies in the parameter
        //     list.
        public int Position { get; set; }

        public override string ToString()
        {
            var fullyQualified = $@"{(IsIn ? "in " : String.Empty)}{(IsLcid ? "Lcid " : String.Empty)}{(IsOptional ? "Optional " : String.Empty)}{(IsOut ? "Out " : String.Empty)}{(IsRetval ? "Retval " : String.Empty)}";
            //fullyQualified += $@"{ParameterType.Name} ";
            fullyQualified += GenericsName;
            return fullyQualified;
        }
    }
}
