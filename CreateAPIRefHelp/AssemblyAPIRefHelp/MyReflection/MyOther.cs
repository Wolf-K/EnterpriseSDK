using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    internal class MyOther : MyTypeBase
    {
        public MyOther(Type otherType, bool processRecursive) : base(otherType, processRecursive)
        {
            KindOf = KindType.Other;

        }
    }
}
