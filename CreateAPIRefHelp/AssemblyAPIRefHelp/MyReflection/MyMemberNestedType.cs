using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberNestedType : MyMember
    {
        public MyMemberNestedType(System.Reflection.TypeInfo typeInfo, MyTypeBase parentType) : base(typeInfo, parentType)
        {
            FullName = (typeInfo.BaseType != null) ? $@"{typeInfo.BaseType.Name} {Name}" : Name;
            //foreach (var field in typeInfo.DeclaredFields)
            //{
            //  if (!field.IsLiteral) continue;
            //  EnumParameters.Add(new MyMemberEnumField(field));
            //}
        }

        public override string ToString()
        {
            return $@"public {FullName}";
        }
    }
}
