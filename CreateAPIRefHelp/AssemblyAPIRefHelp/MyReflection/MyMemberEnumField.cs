using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberEnumField : MyMember
    {
        public MyMemberEnumField(FieldInfo fieldInfo, MyTypeBase parentType) : base(fieldInfo, parentType)
        {
            Name = fieldInfo.Name;
            HasError = String.Empty;
            Value = String.Empty;
            // See if this is a literal value (set at compile time).
            if (fieldInfo.IsLiteral)
            {
                var v = fieldInfo.GetRawConstantValue();
                if (v != null) Value = v.ToString();
            }
        }
        public string? Value { get; set; }

        public override string ToString()
        {
            var ret = Name;
            if (!string.IsNullOrEmpty(Value))
            {
                ret += $@" = {Value}";
            }
            return ret;
        }
    }
}
