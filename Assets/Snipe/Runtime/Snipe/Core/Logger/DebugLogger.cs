using System.Collections.Concurrent;
using UnityEngine;

namespace MiniIT
{
	public class DebugLogger : MonoBehaviour
	{
		private const int DEFAULT_FONT_SIZE = 12;

		public static bool IsEnabled = true;
		public static int FontSize = 12;
		
		#region MonoBehaviour
		
		enum LogMessageType
		{
			Log,
			Warning,
			Error
		}
		
		struct LogMessage
		{
			internal LogMessageType MessageType;
			internal string Text;
		}
		
		private static DebugLogger mInstance;
		private ConcurrentQueue<LogMessage> mLogMessages;
		
		public static void InitInstance()
		{
			if (mInstance == null)
			{
				mInstance = new GameObject("SnipeDebugLogger").AddComponent<DebugLogger>();
				GameObject.DontDestroyOnLoad(mInstance.gameObject);
				mInstance.mLogMessages = new ConcurrentQueue<LogMessage>();
			}
		}
		
		private void Awake()
		{
			if (mInstance != null && mInstance != this)
			{
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
			mInstance = this;
		}
		
		private void Update()
		{
			if (mLogMessages == null || mLogMessages.IsEmpty)
				return;
			
#if !UNITY_EDITOR
			var stack_log_type_log = UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Log);
			var stack_log_type_warning = UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Warning);
			var stack_log_type_error = UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Error);
			UnityEngine.Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			UnityEngine.Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
			UnityEngine.Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
#endif
			
			while (!mLogMessages.IsEmpty)
			{
				if (mLogMessages.TryDequeue(out var item))
				{
					if (item.MessageType == LogMessageType.Error)
						UnityEngine.Debug.LogError(item.Text);
					else if (item.MessageType == LogMessageType.Warning)
						UnityEngine.Debug.LogWarning(item.Text);
					else
						UnityEngine.Debug.Log(item.Text);
				}
			}
			
#if !UNITY_EDITOR
			UnityEngine.Application.SetStackTraceLogType(LogType.Log, stack_log_type_log);
			UnityEngine.Application.SetStackTraceLogType(LogType.Warning, stack_log_type_warning);
			UnityEngine.Application.SetStackTraceLogType(LogType.Error, stack_log_type_error);
#endif
		}
		
		#endregion

		#region Log RichText

		public static void LogBold(object message)
		{
			Log("<b>" + message + "</b>");
		}

		public static void LogItalic(object message)
		{
			Log("<i>" + message + "</i>");
		}

		public static void LogColor(object message, string color)
		{
			Log("<color=" + color + ">" + message + "</color>");
		}

		#endregion

		public static void Log(object message)
		{
			if (IsEnabled)
			{
				if (mInstance != null && mInstance.mLogMessages != null)
				{
					mInstance.mLogMessages.Enqueue(new LogMessage()
						{
							MessageType = LogMessageType.Log,
							Text = ApplyStyle(message)
#if UNITY_EDITOR
							+ "\n\n" + new System.Diagnostics.StackTrace().ToString(),
#endif
						});
				}
				else
				{
					UnityEngine.Debug.Log(ApplyStyle(message));
				}
			}
		}
		
		public static void LogFormat(string format, params object[] args)
		{
			if (IsEnabled)
			{
				if (mInstance != null && mInstance.mLogMessages != null)
				{
					mInstance.mLogMessages.Enqueue(new LogMessage() { MessageType = LogMessageType.Log, Text = ApplyStyle(string.Format(format, args)) });
				}
				else
				{
					UnityEngine.Debug.LogFormat(format, args);
				}
			}
		}
		
		public static void LogWarning(object message)
		{
			if (IsEnabled)
			{
				if (mInstance != null && mInstance.mLogMessages != null)
				{
					mInstance.mLogMessages.Enqueue(new LogMessage() { MessageType = LogMessageType.Warning, Text = ApplyStyle(message) });
				}
				else
				{
					UnityEngine.Debug.Log(ApplyStyle(message));
				}
			}
		}
		
		public static void LogError(object message)
		{
			if (IsEnabled)
			{
				if (mInstance != null && mInstance.mLogMessages != null)
				{
					mInstance.mLogMessages.Enqueue(new LogMessage() { MessageType = LogMessageType.Error, Text = ApplyStyle(message) });
				}
				else
				{
					UnityEngine.Debug.Log(ApplyStyle(message));
				}
			}
		}

		private static string ApplyStyle(object message)
		{
			var log = (FontSize != DEFAULT_FONT_SIZE) ?
				"<size=" + FontSize.ToString()+ ">" + message + "</size>" :
				message;
			
			return $"<i>{System.DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss")} UTC</i> {log}";
		}
	}
}