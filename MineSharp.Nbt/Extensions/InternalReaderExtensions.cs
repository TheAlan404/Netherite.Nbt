using Dennis.BinaryUtils;
using DeepSlate.Nbt.Entities;
using DeepSlate.Nbt.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSlate.Nbt
{
	internal static class InternalReaderExtensions
	{
		internal static NbtTagType ReadTagType(this BinaryReader reader)
		{
			int type = reader.Read<byte>();
			if (type < 0)
			{
				throw new EndOfStreamException();
			}
			else if (type > (int)NbtTagType.LongArray)
			{
				throw new NbtFormatException($"NBT tag type out of range: {type} at position {reader.BaseStream.Position}");
			}
			return (NbtTagType)type;
		}

		internal static string ReadSString(this BinaryReader reader)
		{
			short len = reader.Read<short>();
			return Encoding.UTF8.GetString(reader.ReadBytes(len));
		}

		internal static void WriteSString(this BinaryWriter writer, string value = "")
		{
			byte[] data = Encoding.UTF8.GetBytes(value);
			writer.Write((short)data.Length);
			writer.Write(data);
		}

		// ur bot has compress

		internal static NbtCompression DetectCompression(this BinaryReader reader)
		{
			NbtCompression compression = NbtCompression.None;
			int firstByte = reader.PeekChar();
			switch (firstByte)
			{
				case -1:
					throw new EndOfStreamException();

				case (byte)NbtTagType.Compound: // 0x0A
					compression = NbtCompression.None;
					break;

				case 0x1F: // GZip magic number
					compression = NbtCompression.GZip;
					break;

				case 0x78: // ZLib header
					compression = NbtCompression.ZLib;
					break;

				default:
					throw new InvalidDataException("Could not auto-detect compression format.");
			}
			return compression;
		}

		internal static DeflateStream GetZLibStreamForRead(this Stream stream)
		{
			if (stream.ReadByte() != 0x78)
			{
				throw new InvalidDataException("Unrecognized ZLib header. Expected 0x78");
			}
			stream.ReadByte();
			return new DeflateStream(stream, CompressionMode.Decompress, true);
		}

		internal static ZLibStream GetZLibStreamForWrite(this Stream stream)
		{
			stream.WriteByte(0x78);
			stream.WriteByte(0x01);
			return new ZLibStream(stream, CompressionMode.Compress, true);
		}
	}
}
