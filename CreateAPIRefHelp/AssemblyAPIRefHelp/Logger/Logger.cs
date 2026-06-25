using System.Text;

namespace Logger
{
	public enum LoggerType
	{
		Internal,
		Debug
	}

	public static class Logger
	{
		private static Dictionary<LoggerType, StringBuilder> _logInfos = new Dictionary<LoggerType, StringBuilder>();

		public static void WriteLine (LoggerType type, string logString)
		{
			if (_logInfos.ContainsKey(type)) 
				_logInfos[type].AppendLine (logString);
			else
				_logInfos.Add(type, new StringBuilder (logString+Environment.NewLine));
		}

		public static void WriteLogFiles (string rootPath)
		{
			foreach (string loggerType in Enum.GetNames(typeof(LoggerType)))
			{
				var fileName = Path.Combine(rootPath, $@"Log-{loggerType}.txt");
				if (Enum.TryParse<LoggerType> (loggerType, out LoggerType loggerTypeEnum))
				{
					if (!_logInfos.ContainsKey(loggerTypeEnum)) continue;
					File.WriteAllText(fileName, _logInfos[loggerTypeEnum].ToString());
				}
			}
		}
	}
}