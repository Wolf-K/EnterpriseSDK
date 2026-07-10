using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyReflectionRow
    {
        public static Dictionary<string, string> TypeMappingDictionary = new();

        public MyReflectionRow(string nameSpace)
        {
            this.NameSpace = nameSpace;
            this.Type = String.Empty;
            this.Member = string.Empty;
            this.KindType = KindType.Namespace;
            this.Parent = string.Empty;
            this.GenericsName = string.Empty;
            this.IsInherited = false;
            this.HasInternalParams = false;
            this.TheDeclarationType = string.Empty;
            this.TheName = string.Empty;
        }

        public MyReflectionRow(string nameSpace, string type, KindType kindType, string parent,
          string? theName, string? theDeclarationType, bool hasInternalParams)
        {
            this.NameSpace = nameSpace;
            this.Type = type;
            this.KindType = kindType;
            this.Parent = parent;
            this.ParentType = null;
            this.Member = String.Empty;
            this.GenericsName = string.Empty;
            this.IsInherited = false;
            this.HasInternalParams = hasInternalParams;
            this.TheName = theName;
            this.TheDeclarationType = theDeclarationType;
        }

        public MyReflectionRow(string nameSpace, string type, string member, KindType kindType, string parent,
          string genericsName, bool isInherited, (string prefix, string postfix) help,
          MyTypeBase parentType, string? theName, string? theDeclarationType, bool hasInternalParams)
        {
            this.NameSpace = nameSpace;
            this.Type = type;
            this.Member = member;
            this.KindType = kindType;
            this.Parent = parent;
            this.ParentType = parentType;
            this.GenericsName = genericsName;
            this.HasInternalParams = hasInternalParams;
            this.TheName = theName;
            this.TheDeclarationType = theDeclarationType;
            if (genericsName.Contains("OPENTASKFILEASYNC", StringComparison.CurrentCultureIgnoreCase))
                System.Diagnostics.Trace.WriteLine(genericsName);
            this.IsInherited = isInherited;            
        }

        public MyReflectionRow(MyReflectionRow copyRow)
        {
            this.NameSpace = copyRow.NameSpace;
            this.Type = copyRow.Type;
            this.Member = copyRow.Member;
            this.KindType = copyRow.KindType;
            this.Parent = copyRow.Parent;
            this.GenericsName = copyRow.GenericsName;
            this.IsInherited = copyRow.IsInherited;
            this.ParentType = copyRow.ParentType;
            this.TheDeclarationType = copyRow.TheDeclarationType;
            this.TheName = copyRow.TheName;
            this.HasInternalParams = copyRow.HasInternalParams;
        }

        public MyTypeBase? ParentType { get; set; }

        public bool IsInherited { get; set; }
        public string NameSpace { get; set; }
        public string Type { get; set; }
        public string Member { get; set; }
        public string? TheName { get; set; }
        public string? TheDeclarationType { get; set; }

        public bool HasInternalParams { get; set; }

        public KindType KindType { get; set; }
        public string KindOf
        {
            get
            {
                var kindOf = KindType.ToString();
                if (kindOf.Equals("Constructor")) return "ctor";
                return kindOf.Replace(" ", @"<br/>");
            }
        }
        public string Parent { get; set; }
        public string GenericsName { get; set; }
        public string SummaryContent
        {
            get
            {
                var summaryContent = string.IsNullOrEmpty(Member) ? string.Empty : Member;

                summaryContent = MyUtil.ReplaceWordsFromDictionary(summaryContent, TypeMappingDictionary);
                if (string.IsNullOrEmpty(summaryContent)) return SummaryTopline;
                return summaryContent;
            }
        }
        public string SummaryTopline
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Member))
                {
                    return MyUtil.ReplaceWordsFromDictionary(Type, TypeMappingDictionary);
                }
                if (!string.IsNullOrEmpty(this.Type))
                {
                    return MyUtil.ReplaceWordsFromDictionary(Type, TypeMappingDictionary);
                }
                return NameSpace;
            }
        }
        public string Summary
        {
            get
            {
                var summary = SummaryTopline;
                if (!string.IsNullOrEmpty(SummaryContent))
                {
                    if (summary.Trim().CompareTo(SummaryContent.Trim()) != 0)
                        summary += $@"{Environment.NewLine}<br/>&nbsp;{SummaryContent}";
                }
                return summary;
            }
        }
    }

    /// <summary>
    /// Tracks a change to a ReflectionRow
    /// </summary>
    public class MyReflectionRowChange
    {
        public MyReflectionRowChange(string change, MyReflectionRow row)
        {
            Change = change;
            Row = row;
        }
        public MyReflectionRowChange((string change, MyReflectionRow row) row)
        {
            Change = row.change;
            Row = row.row;
        }

        public string Change { get; set; }
        public MyReflectionRow Row { get; set; }
    }

    // create a Comparison class for MyReflectionRowChange
    public class MyReflectionRowChangeComparer : Comparer<MyReflectionRowChange>
    {
        public override int Compare(MyReflectionRowChange? x, MyReflectionRowChange? y)
        {
            if (x == null || y == null) return -1;

            // first by the Row.SummaryTopline
            var comp = x.Row.SummaryTopline.CompareTo(y.Row.SummaryTopline);
            if (comp != 0) return comp;

            // if SummaryTopline is a match when compare SummaryContent without the parameters
            comp = GetNameOnly(x.Row.SummaryContent).CompareTo(GetNameOnly(y.Row.SummaryContent));
            if (comp != 0) return comp;

            // we have a match -> sort by Change descending so that 'delete' comes before 'add'
            // and from comes before to
            //if ((x.Change != "del" && x.Change != "add")
            //  || (y.Change != "del" && y.Change != "add")
            //  || (x.Change != "from" && x.Change != "to")
            //  || (y.Change != "from" && y.Change != "to"))
            //{
            //  throw new ArgumentException($@"Unknown change type: {x.Change} {y.Change}");
            //}
            if ((x.Change != "from" && x.Change != "to")
              || (y.Change != "from" && y.Change != "to")
              || y.Change == x.Change)
                comp = x.Change.CompareTo(y.Change);
            else
                comp = y.Change.CompareTo(x.Change);
            if (comp != 0) return comp;
            return comp;
        }


        /// <summary>
        /// Find the name part of a declaration
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string GetNameOnly(string inputString)
        {
            var outputString = inputString;
            var lastIdx = inputString.LastIndexOf('{');
            if (lastIdx >= 0)
            {
                outputString = inputString[..lastIdx];
            }
            else
            {
                lastIdx = inputString.LastIndexOf('(');
                if (lastIdx >= 0)
                    outputString = inputString[..lastIdx];
                else
                {
                    lastIdx = inputString.LastIndexOf('=');
                    if (lastIdx >= 0)
                        outputString = inputString[..lastIdx];
                }
            }
            return outputString;
        }
    }
}
