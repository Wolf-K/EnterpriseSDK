using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReflection
{
    public class MyDiffResult
    {
        public string File { get; set; } = String.Empty;
        public string OldAssembly { get; set; } = String.Empty;
        public string NewAssembly { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Version { get; set; } = String.Empty;
        public string Error { get; set; } = String.Empty;

        public List<(string change, MyReflectionRow row)> DiffQueries { get; set; } = new();

        public List<MyReflectionRowChange> ReflectionRowChanges
        {
            get
            {
                List<MyReflectionRowChange> changes = new();
                foreach (var change in DiffQueries)
                {
                    changes.Add(new MyReflectionRowChange(change));
                }
                return changes;
            }
        }

    }
}
