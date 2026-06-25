using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace OlbLib
{
    public enum EnumParamFlags
    {
        PARAMFLAG_NONE = 0,
        PARAMFLAG_FIN = 1,
        PARAMFLAG_FOUT = 2,
        PARAMFLAG_FLCID = 4,
        PARAMFLAG_FRETVAL = 8,
        PARAMFLAG_FOPT = 16,
        PARAMFLAG_FHASDEFAULT = 32,
        PARAMFLAG_FHASCUSTDATA = 64
    }
    public class TlbParameterInfo
    {
        public string Name { get; set; }

        public string HelpString { get; set; }

        public string Type { get; set; }

        public EnumParamFlags Flags { get; protected set; }

        public string DefaultValue { get; set; }

        public bool Default { get { return !string.IsNullOrEmpty(DefaultValue); } }

        public TlbVarTypeInfo VarTypeInfo { get; set; }

        public string TransFlagVC { get; set; }

        public string TransFlag { get; set; }

        public TlbParameterInfo(TlbTypeLibInfo typeLib,
                                ITypeInfo typeInfo, 
                                int index,
                                string name, 
            string docString, 
            string type, 
            System.Runtime.InteropServices.ComTypes.PARAMFLAG paramFlags,
            System.Runtime.InteropServices.ComTypes.TYPEDESC typeDesc)
        {
            Name = name;
            HelpString = docString;
            Type = type;
            
            TransFlag = TlbUtil.TransFlag(paramFlags, this);
            TransFlagVC = TlbUtil.TransFlagVC(paramFlags, this);

            VarTypeInfo = new TlbVarTypeInfo(typeLib, typeInfo, index, Name, typeDesc);
        }
    }
}
