using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UnitySharedLib
{
	class OSCReader : BinaryReader
	{
		public OSCReader(Stream input) : base(input)
		{
		}

		public override string ReadString()
		{
			string str = "";

			bool readMore = true;
			while (readMore)
			{
				byte[] block = ReadBytes(4);
				for (int i = 0; i < 4; i++)
				{
					if (block[i] == 0)
					{
						readMore = false;
						break;
					}
					else
						str += (char)block[i];
				}
			}

			return str;
		}

		public OSCTypeTag[] ReadTags()
		{
			List<OSCTypeTag> tags = new List<OSCTypeTag>();

			bool readMore = true;
			while (readMore)
			{
				if ((BaseStream.Length - BaseStream.Position) >= 4)
				{
					byte[] block = ReadBytes(4);
					for (int i = 0; i < 4; i++)
					{
						if (block[i] == 0)
						{
							readMore = false;
							break;
						}
						else
						{
							switch ((char)block[i])
							{
								case ',': continue;
								case 'i': tags.Add(OSCTypeTag.Int32); break;
								case 'f': tags.Add(OSCTypeTag.Float); break;
								case 's': tags.Add(OSCTypeTag.String); break;
								case 'b': tags.Add(OSCTypeTag.Blob); break;
								case 'h': tags.Add(OSCTypeTag.Int64); break;
								case 't': tags.Add(OSCTypeTag.Time); break;
								case 'd': tags.Add(OSCTypeTag.Double); break;
								case 'S': tags.Add(OSCTypeTag.StringAlt); break;
								case 'c': tags.Add(OSCTypeTag.Character); break;
								case 'r': tags.Add(OSCTypeTag.Color); break;
								case 'm': tags.Add(OSCTypeTag.Midi); break;
								case 'T': tags.Add(OSCTypeTag.True); break;
								case 'F': tags.Add(OSCTypeTag.False); break;
								case 'N': tags.Add(OSCTypeTag.Nil); break;
								case 'I': tags.Add(OSCTypeTag.Infinitum); break;
								case '[': tags.Add(OSCTypeTag.ArrayBegin); break;
								case ']': tags.Add(OSCTypeTag.ArrayEnd); break;
								default:
									throw new Exception("Unknown tag type: " + (char)block[i]);
							}
						}
					}
				}
				else
					break;
			}

			return tags.ToArray();
		}
	}
}
