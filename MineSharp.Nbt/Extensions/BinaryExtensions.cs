using DeepSlate.Nbt.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSlate.Nbt
{
	public static class BinaryExtensions
	{
		public static NbtDocument ReadNbtDocument(this BinaryReader reader)
		{
			return new NbtReader(reader).Read();
		}

		public static void WriteNbtDocument(this BinaryWriter writer, NbtDocument nbt)
		{
			new NbtWriter(writer).Write(nbt);
		}
	}
}
