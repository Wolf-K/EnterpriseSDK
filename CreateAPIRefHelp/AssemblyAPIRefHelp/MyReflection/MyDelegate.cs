using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyDelegate : MyTypeBase
    {
        public MyDelegate(TypeInfo classType, bool processRecursive) : base(classType, processRecursive)
        {
            KindOf = KindType.Delegate;
            if (!processRecursive) return;
        }


    }
}
