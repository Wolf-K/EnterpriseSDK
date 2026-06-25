using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberType : MyMember
    {
        public MyMemberType(System.Reflection.TypeInfo typeInfo, MyTypeBase parentType) : base(typeInfo, parentType)
        {

        }
    }
}
