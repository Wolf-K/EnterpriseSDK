using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using TYPEATTR = System.Runtime.InteropServices.ComTypes.TYPEATTR;
using TYPEKIND = System.Runtime.InteropServices.ComTypes.TYPEKIND;

namespace OlbLib
{
	public class TlbTypeDefInfo
	{
		internal string _varComType;
		internal string _varClrType;
		internal string _name;
		internal string _docString;

		internal TlbTypeDefInfo(TlbTypeLibInfo typeLib, TYPEKIND typeKind, int index) 
		{
			TYPEATTR typeAttr;
			IntPtr typeAttrPtr;

			ITypeInfo _typeInfo;
			typeLib._iTypeLib.GetTypeInfo(index, out _typeInfo);

			_typeInfo.GetTypeAttr(out typeAttrPtr);
			typeAttr = (TYPEATTR)Marshal.PtrToStructure(typeAttrPtr, typeof(TYPEATTR));

		    int _helpContext;
		    string _helpFile;
			typeLib._iTypeLib.GetDocumentation(index, out _name,
						   out _docString, out _helpContext,
						   out _helpFile);
			// Console.Error.WriteLine("TypeDefInfo: " + _name);
			_varComType =
				TlbUtil.TypedescToString(typeLib,
											 _typeInfo,
											 typeAttr.tdescAlias,
											 TlbUtil.COMTYPE);
			_varClrType =
				TlbUtil.TypedescToString(typeLib,
											 _typeInfo,
											 typeAttr.tdescAlias,
											 !TlbUtil.COMTYPE);
			_typeInfo.ReleaseTypeAttr(typeAttrPtr);

			// Add to the typelibrary for resolution
			typeLib.TypeDefHash.Add(_name, this);
		}
	}
}
