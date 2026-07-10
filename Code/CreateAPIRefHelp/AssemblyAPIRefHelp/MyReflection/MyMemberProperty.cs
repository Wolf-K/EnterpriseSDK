using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyMemberProperty : MyMember
    {
        public MyMemberProperty(System.Reflection.PropertyInfo propInfo, MyTypeBase parentType) : base(propInfo, parentType)
        {
            // check for private setter
            var bPrivateSetter = false;
            IsGetInternal = false;
            IsSetInternal = false;
            IsGetPrivate = false;

            if (propInfo.SetMethod != null)
            {
                bPrivateSetter = propInfo.SetMethod.IsPrivate;
                IsSetInternal = propInfo.SetMethod.IsAssembly;
            }
            GetSet = $@"{{{(propInfo.CanRead ? " get;" : string.Empty)}{((propInfo.CanWrite && !bPrivateSetter && !IsSetInternal) ? " set;" : string.Empty)} }}";
            if (propInfo.GetMethod != null)
            {
                PropMethod = new MyMemberMethod(propInfo.GetMethod, parentType);
                IsGetInternal = propInfo.GetMethod.IsAssembly;
                IsGetPrivate = propInfo.GetMethod.IsPrivate;
            }
            else
            {
                if (propInfo.SetMethod != null)
                    PropMethod = new MyMemberMethod(propInfo.SetMethod, parentType);
                else
                    throw new Exception($@"Error: property {propInfo.Name} has neither setter nor getter");
            }
            Parameters = new List<MyMemberParameter>();
            try
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var paras = (propInfo.GetMethod ?? propInfo.SetMethod).GetParameters();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                foreach (var param in paras)
                {
                    Parameters.Add(new MyMemberParameter(param));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            NameSpace = parentType.Namespace;
        }

        public MyMemberMethod PropMethod { get; set; }
        public List<MyMemberParameter> Parameters { get; set; }

        public string GetSet { get; set; }

        public bool IsUsable
        {
            get
            {
                return PropMethod.IsUsable && !IsGetInternal && !IsGetPrivate;
            }
        }

        public bool IsGetInternal { get; set; }

        public bool IsGetPrivate { get; set; }

        public bool IsSetInternal { get; set; }

        public override string Name
        {
            get
            {
                var theName = base.Name;
                if (Arguments.Length > 0)
                {
                    var args = $@"({Arguments})";
                    theName += MyUtil.SimplifyParentheses(args);
                }
                return theName;
            }
            set => base.Name = value;
        }

        public string NameSpace { get; set; }

        public string Arguments
        {
            get
            {
                return string.Join(", ", Parameters.Select(p => p.ToString()));
            }
        }

        public override string ToString()
        {
            var fullName = PropMethod.FullName ?? String.Empty;
            fullName = fullName.Substring(0, fullName.LastIndexOf(" ") < 0 ? 0 : fullName.LastIndexOf(" "));
            if (Arguments.Length > 0)
            {
                fullName += $@"({Arguments})";
            }
            var str = $@"{(PropMethod.IsPublic ? "public " : String.Empty)}{PropMethod.StaticOrNot}{fullName} {Name} {GetSet}";
            str = str.Replace(NameSpace + ".", string.Empty);
            return str;
        }
    }
}
