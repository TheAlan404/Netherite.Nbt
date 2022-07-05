using Dennis.BinaryUtils;
using DeepSlate.Nbt.Entities;
using DeepSlate.Nbt.Exceptions;
using System;
using System.IO;
using System.IO.Compression;

namespace DeepSlate.Nbt.Binary
{
	public class NbtReader
	{
		BinaryReader _reader;

		public NbtCompression Compression = NbtCompression.AutoDetect;

		public NbtReader(Stream stream, NbtCompression? compression = null)
			: this(new BinaryReader(stream), compression) { }

		public NbtReader(BinaryReader reader, NbtCompression? compression = null)
		{
			if (compression != null) Compression = (NbtCompression)compression;
			if (Compression == NbtCompression.AutoDetect) Compression = reader.DetectCompression();
			Stream _stream = Compression switch
			{
				NbtCompression.None => reader.BaseStream,
				NbtCompression.GZip => new GZipStream(reader.BaseStream, CompressionMode.Decompress, true),
				NbtCompression.ZLib => reader.BaseStream.GetZLibStreamForRead(),
				_ => throw new ArgumentOutOfRangeException(nameof(compression)),
			};

			_reader = new BinaryReader(_stream);
		}

		public NbtDocument Read()
		{
			return (NbtDocument)ReadCompound();
		}

		public NbtCompound ReadCompound()
		{
			NbtCompound compound = new NbtCompound();

			while (true)
			{
				NbtTagType tagType = _reader.ReadTagType();
				if (tagType == NbtTagType.End) break;
				if (tagType == NbtTagType.Unknown) throw new NotImplementedException($"Unknown tag type at position {_reader.BaseStream.Position}");

				Console.WriteLine($"reading {tagType} at {_reader.BaseStream.Position}");

				NbtTag? newTag = tagType switch
				{
					NbtTagType.Byte => new NbtByte(),
					NbtTagType.Short => new NbtShort(),
					NbtTagType.Int => new NbtInt(),
					NbtTagType.Long => new NbtLong(),
					NbtTagType.Float => new NbtFloat(),
					NbtTagType.Double => new NbtDouble(),
					NbtTagType.String => new NbtString(),
					NbtTagType.ByteArray => new NbtByteArray(),
					NbtTagType.IntArray => new NbtIntArray(),
					NbtTagType.LongArray => new NbtLongArray(),
					_ => null,
				};
				if (newTag != null)
				{
					newTag.Name = _reader.ReadSString();
					newTag.ReadTag(_reader);
					compound.Add(newTag);
					Console.WriteLine(_reader.BaseStream.Position + " " + newTag.ToString());
				}
				else
				{
					switch (tagType)
					{
						case NbtTagType.Compound:
							string cname = _reader.ReadSString();
							NbtCompound childCompound = ReadCompound();
							childCompound.Name = cname;
							compound.Add(childCompound);
							Console.WriteLine(_reader.BaseStream.Position + " " + childCompound.ToString());
							break;
						case NbtTagType.List:
							string lname = _reader.ReadSString();
							NbtList childList = ReadList();
							childList.Name = lname;
							compound.Add(childList);
							Console.WriteLine(_reader.BaseStream.Position + " " + childList.ToString());
							break;
						default:
							throw new NbtFormatException($"Unknown tag type at position {_reader.BaseStream.Position}");
					}
				}
			}

			return compound;
		}

		public NbtList ReadList()
		{
			NbtList list = new NbtList();

			list.ListType = _reader.ReadTagType();

			Console.WriteLine($"reading list of type {list.ListType} at {_reader.BaseStream.Position}");

			int length = _reader.Read<int>();
			if (length < 0)
			{
				throw new NbtFormatException($"Negative list size given at position {_reader.BaseStream.Position}");
			}

			Console.WriteLine($"list length is {length}");

			for (int i = 0; i < length; i++)
			{
				NbtTag? newTag = list.ListType switch
				{
					NbtTagType.Byte => new NbtByte(),
					NbtTagType.Short => new NbtShort(),
					NbtTagType.Int => new NbtInt(),
					NbtTagType.Long => new NbtLong(),
					NbtTagType.Float => new NbtFloat(),
					NbtTagType.Double => new NbtDouble(),
					NbtTagType.String => new NbtString(),
					NbtTagType.ByteArray => new NbtByteArray(),
					NbtTagType.IntArray => new NbtIntArray(),
					NbtTagType.LongArray => new NbtLongArray(),
					_ => null,
				};

				if (newTag != null)
				{
					newTag.ReadTag(_reader);
					list.Add(newTag);
					Console.WriteLine(_reader.BaseStream.Position + " " + newTag.ToString());
				}
				else
				{
					switch (list.ListType)
					{
						case NbtTagType.Compound:
							NbtCompound childCompound = ReadCompound();
							list.Add(childCompound);
							Console.WriteLine(_reader.BaseStream.Position + " " + childCompound.ToString());
							break;
						case NbtTagType.List:
							NbtList childList = ReadList();
							list.Add(childList);
							Console.WriteLine(_reader.BaseStream.Position + " " + childList.ToString());
							break;
						default:
							throw new NbtFormatException($"Unknown tag type at position {_reader.BaseStream.Position}");
					}
				}
			}

			return list;
		}
	}
}
