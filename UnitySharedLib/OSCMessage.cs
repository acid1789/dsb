using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitySharedLib
{
	public class OSCMessage
	{
		public string Address;
		public object[] Args;
		public string From;

		public OSCMessage(string path, params object[] args)
		{
			Address = path;
			Args = args;
		}

		public byte[] ToArray()
		{
			MemoryStream ms = new MemoryStream();
			OSCWriter oscw = new OSCWriter(ms);
			oscw.Write(Address);
			if (Args != null)
			{
				string args = EncodeArgTypes(Args);
				oscw.Write(args);

				foreach (object Arg in Args)
				{
					if (Arg == null || Arg is bool)
						continue;
					if (Arg is double && double.IsPositiveInfinity((double)Arg))
						continue;

					if (Arg is int || Arg is char)
						oscw.Write((int)Arg);
					else if (Arg is float)
						oscw.Write((float)Arg);
					else if (Arg is string)
						oscw.Write((string)Arg);
					else if (Arg is byte[])
						oscw.WriteBlob((byte[])Arg);
					else if (Arg is Int64)
						oscw.Write((Int64)Arg);
					else if (Arg is double)
						oscw.Write((double)Arg);
					else if (Arg is UInt32)
						oscw.Write((uint)Arg);
				}
			}
			return ms.ToArray();
		}

		public static OSCMessage FromData(byte[] data, int dataLength, string from)
		{
			if (data[0] == '#')
			{
				// Bundle
				throw new NotImplementedException();
			}
			else
			{
				// Message
				OSCReader oscr = new OSCReader(new MemoryStream(data));
				string address = oscr.ReadString();
				OSCTypeTag[] argTypes = oscr.ReadTags();
				object[] args = ReadArgs(argTypes, oscr);
				OSCMessage msg = new OSCMessage(address, args);
				msg.From = from;
				return msg;
			}
		}

		static object[] ReadArgs(OSCTypeTag[] argTypes, OSCReader oscr)
		{
			if (argTypes.Length > 0)
			{
				List<object> args = new List<object>();
				foreach (OSCTypeTag argType in argTypes)
				{
					switch (argType)
					{
						case OSCTypeTag.Int32: args.Add(oscr.ReadInt32()); break;
						case OSCTypeTag.Float: args.Add(oscr.ReadSingle()); break;
						case OSCTypeTag.String: args.Add(oscr.ReadString()); break;
						case OSCTypeTag.Blob:
							{
								int size = oscr.ReadInt32();
								byte[] array = oscr.ReadBytes(size);
								int remainder = size % 4;
								if (remainder != 0)
									oscr.ReadBytes(remainder);
								args.Add(array);
							}
							break;
						case OSCTypeTag.Int64: args.Add(oscr.ReadInt64()); break;
						case OSCTypeTag.Time: throw new NotImplementedException();
						case OSCTypeTag.Double: args.Add(oscr.ReadDouble()); break;
						case OSCTypeTag.StringAlt: args.Add(oscr.ReadString()); break;
						case OSCTypeTag.Character: args.Add((char)oscr.ReadInt32()); break;
						case OSCTypeTag.Color: args.Add(oscr.ReadUInt32()); break;
						case OSCTypeTag.Midi: throw new NotImplementedException();
						case OSCTypeTag.True: args.Add(true); break;
						case OSCTypeTag.False: args.Add(false); break;
						case OSCTypeTag.Nil: args.Add(null); break;
						case OSCTypeTag.Infinitum: args.Add(double.PositiveInfinity); break;
						case OSCTypeTag.ArrayBegin:
						case OSCTypeTag.ArrayEnd:
							throw new NotImplementedException();

					}
				}
				return args.ToArray();
			}
			return null;
		}

		static string EncodeArgTypes(object[] args)
		{
			if (args == null || args.Length < 1)
				return null;

			string argTypes = ",";
			foreach (object arg in args)
			{
				if (arg == null)
					argTypes += 'N';
				else if (arg is int)
					argTypes += 'i';
				else if (arg is float)
					argTypes += 'f';
				else if (arg is string)
					argTypes += 's';
				else if (arg is byte[])
					argTypes += 'b';
				else if (arg is Int64)
					argTypes += 'h';
				else if (arg is double)
				{
					double d = (double)arg;
					argTypes += double.IsPositiveInfinity(d) ? 'I' : 'd';
				}
				else if (arg is char)
					argTypes += 'c';
				else if (arg is UInt32)
					argTypes += 'r';
				else if (arg is bool)
				{
					bool a = (bool)arg;
					argTypes += a ? 'T' : 'F';
				}
				else
					throw new Exception(string.Format("arg of type {0] is not supported", arg.GetType().Name));
			}
			return argTypes;
		}
	}
}
