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
			if (padding != 0)
				base.Write(new byte[padding], 0, padding);
		}

		public void WriteBlob(byte[] blob)
		{
			base.Write((int)blob.Length);
			base.Write(blob, 0, blob.Length);			
			int padding = 4 - (blob.Length % 4);
			if( padding != 0 )
				base.Write(new byte[padding], 0, padding);
		}
	}
}
