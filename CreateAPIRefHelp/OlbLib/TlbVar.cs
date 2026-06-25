using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace OlbLib
{
    public class TlbVar
    {
        public string Name { get; set; }

        public string HelpString { get; set; }

        public int MemberId { get; set; }

        public TlbVar(VARDESC funcdesc, ITypeInfo pTypeInfo, int idx)
        {
            MemberId = funcdesc.memid;

            string sName;
            string sDocString;
            int dwHelpContext;
            string sHelpFile;
            pTypeInfo.GetDocumentation(MemberId, out sName, out sDocString, out dwHelpContext, out sHelpFile);

            Name = sName;
            HelpString = sDocString;
        }
    }
}
