using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Dennis.BinaryUtils;
using DeepSlate.Nbt.Entities;
using DeepSlate.Nbt.Exceptions;

namespace DeepSlate.Nbt.Binary
{
    public class NbtWriter
    {
        BinaryWriter _writer;

		public NbtCompression Compression = NbtCompression.None;

		public NbtWriter(Stream stream, NbtCompression? compression = null)
			: this(new BinaryWriter(stream), compression) { }

		public NbtWriter(BinaryWriter writer, NbtCompression? compression = null)
		{
			if (compression != null) Compression = (NbtCompression)compression;

			Stream _stream = Compression switch
			{
				NbtCompression.None => writer.BaseStream,
				NbtCompression.GZip => new GZipStream(writer.BaseStream, CompressionMode.Compress, true),
				NbtCompression.ZLib => writer.BaseStream.GetZLibStreamForWrite(),
				_ => throw new ArgumentOutOfRangeException(nameof(Compression)),
			};

			_writer = new BinaryWriter(_stream);
		}

		public void Write(NbtDocument nbt)
		{
			_writer.Write((byte)nbt.TagType);
			_writer.WriteSString(nbt.Name ?? "");
			WriteCompound(nbt);

			if (Compression == NbtCompression.ZLib)
			{
				// apparently zlib has a checksum at the end
				byte[] checksumBytes = BitConverter.GetBytes(((ZLibStream)_writer.BaseStream).Checksum);
				if (BitConverter.IsLittleEndian)
				{
					// Adler32 checksum is big-endian
					Array.Reverse(checksumBytes);
				}
				_writer.Write(checksumBytes);
			}
		}

		public void WriteCompound(NbtCompound compound)
		{
			foreach (var kvp in compound.Tags)
			{
				NbtTag tag = kvp.Value;
				_writer.Write((byte)tag.TagType);
				_writer.WriteSString(tag.Name ?? "");
				if (tag is NbtCompound childCompound)
				{
					WriteCompound(childCompound);
					continue;
				}
				else if (tag is NbtList childList)
				{
					WriteList(childList);
					continue;
				}
				else
				{
					tag.WriteData(_writer);
				}
			}
			_writer.Write((byte)NbtTagType.End);
		}

		public void WriteList(NbtList list)
		{
			_writer.Write((byte)list.ListType);
			_writer.Write(list.Tags.Count);
			foreach (var tag in list.Tags)
			{
				if (tag is NbtCompound childCompound)
				{
					WriteCompound(childCompound);
					continue;
				}
				else if (tag is NbtList childList)
				{
					WriteList(childList);
					continue;
				}
				else
				{
					tag.WriteData(_writer);
				}
			}
		}
    }
}
