using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberField : MyMember
    {
        public MyMemberField(System.Reflection.FieldInfo fieldInfo, MyTypeBase parentType) : base(fieldInfo, parentType)
        {
            FieldType = new MyTypeBase(fieldInfo.FieldType, false);
            IsStatic = fieldInfo.IsStatic;
            IsPublic = fieldInfo.IsPublic;
        }
        public MyTypeBase FieldType { get; set; }

        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }

        public string StaticOrNot => (IsStatic ? "static " : String.Empty);
        public string PublicOrNot => (IsPublic ? "public " : String.Empty);

        public override string ToString()
        {
            return $@"{StaticOrNot}{PublicOrNot}{FieldType.FullName} {Name}";
        }
    }
}
