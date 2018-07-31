using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace UnitySharedLib
{
	public class OSCManager
	{
		public delegate bool OSCMessageHandler(OSCMessage msg);
		const int c_DefaultUdpPort = 9876;
		const string c_DefaultBroadcastAddress = "255.255.255.255";

		#region Static Member Varaibles
		static int s_UDPPort;
		static string s_BroadcastAddress;
		static OSCManager s_Instance;
		static Socket s_UdpSendSocket;
		static Thread s_UDPReceiverThread;
		static string s_UDPThreadError;
		static List<OSCMessage> s_PendingMessages;
		static Mutex s_PendingMessageLock;
		#endregion

		#region Internal Member Vairables
		OSCTreeNode _rootNode;
		#endregion

		#region Internal Interface
		private OSCManager()
		{
			_rootNode = new OSCTreeNode() { NodeName = "root" };
		}

		void UpdateInternal()
		{
			if (s_PendingMessages.Count > 0)
			{
				// Get the mail
				s_PendingMessageLock.WaitOne();
				OSCMessage[] messages = s_PendingMessages.ToArray();
				s_PendingMessages.Clear();
				s_PendingMessageLock.ReleaseMutex();

				int breakIndex = -1;
				for( int i = 0; i < messages.Length; i++ )
				{
					OSCMessage msg = messages[i];
					OSCTreeNode node = GetLeaf(msg.Address, false);					
					if (node != null)
					{
						foreach (OSCMessageHandler handler in node.Handlers)
						{
							if (!handler(msg))
								breakIndex = i + 1;
						}
					}
					if (breakIndex >= 0)
						break;
				}

				if (breakIndex >= 0)
				{
					s_PendingMessageLock.WaitOne();
					for (int i = messages.Length - 1; i >= breakIndex; i--)
						s_PendingMessages.Insert(0, messages[i]);
					s_PendingMessageLock.ReleaseMutex();
				}
			}
		}

		void ListenToAddressInternal(string address, OSCMessageHandler handler)
		{
			OSCTreeNode leaf = GetLeaf(address);
			if (!leaf.Handlers.Contains(handler))
				leaf.Handlers.Add(handler);
		}

		OSCTreeNode GetLeaf(string address, bool buildTree = true)
		{
			string[] pieces = address.ToLower().Split('/');
			if (pieces.Length < 1)
				return null;

			OSCTreeNode treeCursor = _rootNode;
			int firstPiece = (pieces[0] == "root") ? 1 : 0;
			for( int i = firstPiece; i < pieces.Length; i++ )
			{
				OSCTreeNode nextNode = null;
				if (treeCursor.Children != null)
				{
					foreach (OSCTreeNode child in treeCursor.Children)
					{
						if (child.NodeName == pieces[i])
						{
							nextNode = child;
							break;
						}
					}
				}
				if (nextNode == null)
				{
					if (!buildTree)
						return null;

					nextNode = new OSCTreeNode() { NodeName = pieces[i], Handlers = new List<OSCMessageHandler>(), Parent = treeCursor };
					if (treeCursor.Children == null)
						treeCursor.Children = new List<OSCTreeNode>();
					treeCursor.Children.Add(nextNode);
				}
				treeCursor = nextNode;
			}
			return treeCursor;
		}
		#endregion

		#region Public Static Interface
		public static void Initialize(string broadcastAddress = c_DefaultBroadcastAddress, int udpPort = c_DefaultUdpPort)
		{
			if (s_Instance != null)
				throw new InvalidOperationException("OSCManager is already instantiated!");
			s_Instance = new OSCManager();
			s_UDPPort = udpPort;
			s_BroadcastAddress = broadcastAddress;

			s_PendingMessages = new List<OSCMessage>();
			s_PendingMessageLock = new Mutex();
			s_UDPReceiverThread = new Thread(new ThreadStart(UDPReceiverThread)) { Name = "UDPReceiverThread" };
			s_UDPReceiverThread.Start();
		}

		public static void Shutdown()
		{
			if (s_UDPReceiverThread != null)
			{
				s_UDPReceiverThread.Abort();
				s_UDPReceiverThread = null;
			}
			if (s_UdpSendSocket != null)
			{
				s_UdpSendSocket.Close();
				s_UdpSendSocket = null;
			}
			s_PendingMessageLock.Close();
			s_Instance = null;
		}

		public static void Update()
		{
			s_Instance.UpdateInternal();
		}

		public static void ListenToAddress(string address, OSCMessageHandler handler)
		{
			s_Instance.ListenToAddressInternal(address, handler);
		}

		public static void SendToAll(string address, params object[] args)
		{
			SendToAll(new OSCMessage(address, args));
		}

		public static void SendToAll(OSCMessage messageToSend, int port = 0)
		{
			SendTo(messageToSend, s_BroadcastAddress, port);
		}

		public static void SendTo(OSCMessage messageToSend, string targetIP, int port = 0)
		{
			if (s_UdpSendSocket == null)
			{
				s_UdpSendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				s_UdpSendSocket.EnableBroadcast = true;
			}

			if (port == 0)
				port = s_UDPPort;
			
			IPAddress addr = IPAddress.Parse(targetIP);
			IPEndPoint target = new IPEndPoint(addr, port);
			int sentBytes = s_UdpSendSocket.SendTo(messageToSend.ToArray(), target);
			//SharedLogger.Print(SharedLogger.MessageType.Debug, "Sent {0} bytes to {1}:{2}", sentBytes, targetIP, c_UDPPort);
		}
		#endregion

		static void UDPReceiverThread()
		{
			try
			{
				UdpClient udpClient = new UdpClient();
				udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, s_UDPPort));
				
				if( s_BroadcastAddress != c_DefaultBroadcastAddress )
					udpClient.JoinMulticastGroup(IPAddress.Parse(s_BroadcastAddress));
				udpClient.EnableBroadcast = true;

				IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, s_UDPPort);
				while (true)
				{
					if (udpClient.Available > 0)
					{
						byte[] incommingData = udpClient.Receive(ref remoteEP);

						OSCMessage msg = OSCMessage.FromData(incommingData, incommingData.Length, remoteEP.Address.ToString());
						s_PendingMessageLock.WaitOne();
						s_PendingMessages.Add(msg);
						s_PendingMessageLock.ReleaseMutex();
					}
					Thread.Sleep(10);
				}
			}
			catch (Exception ex)
			{
				s_UDPThreadError = ex.ToString();
				SharedLogger.Print(SharedLogger.MessageType.Error, "UDP Receive Thread Error\n" + ex.ToString());
			}
		}


		class OSCTreeNode
		{
			public string NodeName;
			public OSCTreeNode Parent;
			public List<OSCTreeNode> Children;
			public List<OSCMessageHandler> Handlers;

			public override string ToString()
			{
				return string.Format("{0} ({1})", NodeName, Children.Count);
			}
		}
	}
}
