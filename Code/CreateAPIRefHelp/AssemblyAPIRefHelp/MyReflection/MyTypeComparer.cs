using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyTypeComparer : IComparer<MyReflectionRow>
    {
        int IComparer<MyReflectionRow>.Compare(MyReflectionRow? a, MyReflectionRow? b)
        {
            return Comp(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static int Comp(MyReflectionRow? a, MyReflectionRow? b, StringComparison sComp)
        {
            if (a == null || b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            if (a.NameSpace.Equals(b.NameSpace, sComp))
            {
                if (a.Type.Equals(b.Type, sComp))
                {
                    if (a.Member.Equals(b.Member, sComp))
                    {
                        return 0;
                    }
                    return string.Compare(a.Member, b.Member, sComp);
                }
                return string.Compare(a.Type, b.Type, sComp);
            }
            return string.Compare(a.NameSpace, b.NameSpace, sComp);
        }

        public static List<MyReflectionRow> CompleteSort(List<MyTypeBase> types)
        {
            List<MyReflectionRow> result = new();
            var previousNS = string.Empty;
            foreach (var type in types)
            {
                if (previousNS != type.Namespace)
                {
                    result.Add(new MyReflectionRow(type.Namespace));
                    previousNS = type.Namespace;
                }
                result.Add(new MyReflectionRow(type.Namespace, type.ToString(), type.KindOf, type.Namespace, type.Name, type.ReturnType, false));
                foreach (var member in type.Members)
                {
                    result.Add(new MyReflectionRow(type.Namespace, type.ToString(), member.ToString(), member.KindType, member.ParentType.ToString(),
                      type.GenericsName, member.IsInherited, (member.ParentType.FullName, member.Name), type, member.Name, member.NewReturnType, member.HasInternalParams));
                }
                foreach (var enumParam in type.EnumParameters)
                {
                    result.Add(new MyReflectionRow(type.Namespace, type.ToString(), enumParam.ToString(), KindType.Enum, enumParam.ParentType.ToString(),
                      type.GenericsName, false, (type.Namespace, enumParam.ParentType.Name), type, enumParam.Name, enumParam.NewReturnType, false));
                }
            }
            // first sort list
            var comparer = new MyTypeComparer();
            result.Sort(comparer);
            return result;
        }
    }
}
