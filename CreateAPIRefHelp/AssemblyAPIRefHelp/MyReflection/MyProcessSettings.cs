using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    static public class MyProcessSettings
    {
        public static bool ProcessOldSymbols { get; set; } = false;

        public static List<string> MissingApiReferences { get; private set; } = new List<string>();
    }
}
