namespace DatabaseIO
{
    public class DocReturned
    {
        public string Name { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public string ErrorsReturned { get; set; } = string.Empty;
        public string WhenToUse { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string SeeAlsos { get; set; } = string.Empty;
        public string CSharp { get; set; } = string.Empty;
        public string VbNet { get; set; } = string.Empty;
        public bool HasSampleCode => !string.IsNullOrWhiteSpace(CSharp) || !string.IsNullOrWhiteSpace(VbNet);
  }
}
