﻿using System;
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
		public delegate void OSCMessageHandler(OSCMessage msg);
		const int c_UDPPort = 9876;

		#region Static Member Varaibles
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

				foreach (OSCMessage msg in messages)
				{
					OSCTreeNode node = GetLeaf(msg.Address, false);
					if (node != null)
					{
						foreach (OSCMessageHandler handler in node.Handlers)
							handler(msg);
					}
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
		public static void Initialize()
		{
			if (s_Instance != null)
				throw new InvalidOperationException("OSCManager is already instantiated!");
			s_Instance = new OSCManager();

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

		public static void SendToAll(OSCMessage messageToSend)
		{
			if (s_UdpSendSocket == null)
			{
				s_UdpSendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				s_UdpSendSocket.EnableBroadcast = true;
			}

			//IPEndPoint target = new IPEndPoint(IPAddress.Broadcast, c_UDPPort);
			IPEndPoint target = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 255 }), c_UDPPort);
			int sentBytes = s_UdpSendSocket.SendTo(messageToSend.ToArray(), target);
			SharedLogger.Print(SharedLogger.MessageType.Debug, "Sent {0} bytes to broadcast port: {1}", sentBytes, c_UDPPort);
		}
		#endregion

		static void UDPReceiverThread()
		{
			try
			{				
				Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				udpSocket.Bind(new IPEndPoint(IPAddress.Any, c_UDPPort));

				EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
				byte[] incommingData = new byte[65535];
				while (true)
				{					
					int bytesReceived = udpSocket.ReceiveFrom(incommingData, ref remoteEP);
					IPEndPoint ipep = (IPEndPoint)remoteEP;
					OSCMessage msg = OSCMessage.FromData(incommingData, bytesReceived, ipep.Address.ToString());
					s_PendingMessageLock.WaitOne();
					s_PendingMessages.Add(msg);
					s_PendingMessageLock.ReleaseMutex();
				}
			}
			catch (Exception ex)
			{
				s_UDPThreadError = ex.ToString();
			}
		}


		class OSCTreeNode
		{
			public string NodeName;
			public OSCTreeNode Parent;
			public List<OSCTreeNode> Children;
			public List<OSCMessageHandler> Handlers;
		}
	}
}
