using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnitySharedLib
{
	public static class SharedLogger
	{
		public enum MessageType
		{
			Debug,
			Info,
			Warning,
			Error
		}

		public delegate void MessageHandler(MessageType type, string message);

		static List<MessageHandler> s_Handlers;
		static Mutex s_HandlerLock;

		public static void ListenToMessages(MessageHandler handler)
		{
			if (s_Handlers == null)
			{
				s_Handlers = new List<MessageHandler>();
				s_HandlerLock = new Mutex();
			}

			if (!s_Handlers.Contains(handler))
			{
				s_HandlerLock.WaitOne();
				s_Handlers.Add(handler);
				s_HandlerLock.ReleaseMutex();
			}
		}

		public static void Print(MessageType type, string message, params object[] args)
		{
			if (s_Handlers != null)
			{
				string finalMsg = string.Format(message, args);
				s_HandlerLock.WaitOne();
				foreach (MessageHandler handler in s_Handlers)
				{
					handler(type, finalMsg);
				}
				s_HandlerLock.ReleaseMutex();
			}
		}
	}
}
