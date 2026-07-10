using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyStruct : MyTypeBase
    {
        public MyStruct(Type structType, bool processRecursive) : base(structType, processRecursive)
        {
            KindOf = KindType.Struct;
            if (Name.Contains("<>c"))
            {
                System.Diagnostics.Trace.WriteLine(Name);
            }
            if (!processRecursive) return;
            try
            {
                // Get the information related to all public members
                var myMemberInfo = structType.GetMembers();
                //System.Diagnostics.Trace.WriteLine($@"{Environment.NewLine}The members of class '{structType.FullName}' are :{Environment.NewLine}");
                foreach (var constructorInfo in structType.GetConstructors())
                {
                    if (!constructorInfo.IsPublic) continue;
                    Members.Add(new MyMemberConstructor(constructorInfo, this));
                }
                foreach (var fieldInfo in structType.GetFields())
                {
                    if (!fieldInfo.IsPublic) continue;
                    Members.Add(new MyMemberField(fieldInfo, this));
                }
                foreach (var propertyInfo in structType.GetProperties())
                {
                    var tmp = new MyMemberProperty(propertyInfo, this);
                    if (tmp.IsUsable)
                        Members.Add(tmp);
                }
                foreach (var methodInfo in structType.GetMethods())
                {
                    if (methodInfo == null) continue;
                    if (!methodInfo.IsPublic) continue;
                    //System.Diagnostics.Trace.WriteLine(methodInfo.ToString());
                    if (methodInfo.IsSpecialName) continue;
                    var methinfo = methodInfo.ToString();
                    if (methinfo == null) continue;
                    if (!MyConsts.ExcludeMethodSignatures.Contains(methinfo))
                    {
                        var tmp = new MyMemberMethod(methodInfo, this);
                        if (tmp.IsUsable)
                            Members.Add(tmp);
                    }
                }
                foreach (var eventInfo in structType.GetEvents())
                {
                    Members.Add(new MyMemberEvent(eventInfo, this));
                }
            }
            catch (Exception ex)
            {
                // We are missing the required dependency assembly.
                HasError = $@"GetMembers Error: {ex.Message}";
            }
        }
    }
}
