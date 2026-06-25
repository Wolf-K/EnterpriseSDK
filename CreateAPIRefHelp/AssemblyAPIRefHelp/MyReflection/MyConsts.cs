using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    internal class MyConsts
    {
        internal static List<string> ExcludeMethodSignatures = new List<string>()
    {
      "System.Type GetType()",
      "System.String ToString()",
      "System.Boolean Equals(System.Object)",
      "System.Int32 GetHashCode()"
    };
    }
}
