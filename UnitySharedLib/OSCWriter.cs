using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UnitySharedLib
{
	class OSCWriter : BinaryWriter
	{
		public OSCWriter(Stream output) : base(output)
		{
		}

		public override void Write(string value)
		{
			byte[] strBytes = Encoding.ASCII.GetBytes(value);
			base.Write(strBytes, 0, value.Length);
			base.Write((byte)0);
			int writtenSize = value.Length + 1;
			int padding = 4 - (writtenSize % 4);
			if (padding != 0 && padding != 4)
				base.Write(new byte[padding], 0, padding);
		}

		public void WriteBlob(byte[] blob)
		{
			base.Write((int)blob.Length);
			base.Write(blob, 0, blob.Length);			
			int padding = 4 - (blob.Length % 4);
			if (padding != 0 && padding != 4)
				base.Write(new byte[padding], 0, padding);
		}

		uint swap32(uint val)
		{
			return ((val & 0xFF) << 24) | ((val & 0xFF000000) >> 24) | ((val & 0xFF00) << 8) | ((val & 0xFF0000) >> 8);
		}

		public override void Write(int value)
		{
			if (BitConverter.IsLittleEndian)
				value = (int)swap32((uint)value);
			base.Write(value);
		}

		public override void Write(long value)
		{
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(value);
				Array.Reverse(bytes);
				value = BitConverter.ToInt64(bytes, 0);
			}
			base.Write(value);
		}

		public override void Write(float value)
		{
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(value);
				Array.Reverse(bytes);
				value = BitConverter.ToSingle(bytes, 0);
			}
			base.Write(value);
		}

		public override void Write(double value)
		{
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(value);
				Array.Reverse(bytes);
				value = BitConverter.ToSingle(bytes, 0);
			}
			base.Write(value);
		}
	}
}
