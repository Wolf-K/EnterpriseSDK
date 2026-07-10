using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberEvent : MyMember
    {
        public MyMemberEvent(System.Reflection.EventInfo eventInfo, MyTypeBase parentType) : base(eventInfo, parentType)
        {
            ParameterType = null;
            if (eventInfo.EventHandlerType != null)
                ParameterType = new MyTypeBase(eventInfo.EventHandlerType, false);
            //while (Name.Contains("`"))
            //{
            //  Name = MyUtil.FixTemplateTypeSyntax(Name);
            //}
        }

        public MyTypeBase? ParameterType { get; set; }

        public override string ToString()
        {
            var fullyQualified = $@"public event ";
            fullyQualified += $@"{ParameterType?.Name} ";
            fullyQualified += Name;
            return fullyQualified;
        }
    }
}
