using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dennis.BinaryUtils;
using DeepSlate.Nbt.Exceptions;

namespace DeepSlate.Nbt.Entities
{
	/// <summary> A tag containing an array of bytes. </summary>
	public sealed class NbtByteArray : NbtTag
	{
		public override NbtTagType TagType => NbtTagType.ByteArray;

		/// <summary> Value/payload of this tag (an array of bytes). Value is stored as-is and is NOT cloned.
		public byte[] Value = Array.Empty<byte>();

		public NbtByteArray()
		{

		}

		public NbtByteArray(byte[] value)
			: this(null, value) { }

		public NbtByteArray(string? tagName, byte[] value)
			: base(tagName)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			Value = (byte[])value.Clone();
		}

		public NbtByteArray(NbtByteArray other)
			: this(other.Name, other.Value) { }


		/// <summary> Gets or sets a byte at the given index. </summary>
		/// <param name="tagIndex"> The zero-based index of the element to get or set. </param>
		/// <returns> The byte at the specified index. </returns>
		/// <exception cref="IndexOutOfRangeException"> <paramref name="tagIndex"/> is outside the array bounds. </exception>
		public new byte this[int tagIndex]
		{
			get => Value[tagIndex];
			set => Value[tagIndex] = value;
		}


		internal override void ReadTag(BinaryReader readStream)
		{
			int length = readStream.Read<int>();
			if (length < 0)
			{
				throw new NbtFormatException("Negative length given in TAG_Byte_Array");
			}

			Value = readStream.ReadBytes(length);
			if (Value.Length < length)
			{
				throw new EndOfStreamException();
			}
		}

		internal override void WriteTag(BinaryWriter writeStream)
		{
			if (Name == null) throw new NbtFormatException("Name is null");
			writeStream.Write(NbtTagType.ByteArray);
			writeStream.Write(Name);
			WriteData(writeStream);
		}


		internal override void WriteData(BinaryWriter writeStream)
		{
			writeStream.Write(Value.Length);
			writeStream.Write(Value, 0, Value.Length);
		}


		/// <inheritdoc />
		public override object Clone()
		{
			return new NbtByteArray(this);
		}

		internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
		{
			base.PrettyPrint(sb, indentString, indentLevel);
			sb.Append($"[{Value.Length} bytes]");
		}

		public static implicit operator byte[](NbtByteArray nbt) => nbt.Value;
		public static implicit operator List<byte>(NbtByteArray nbt) => nbt.Value.ToList();
		public static implicit operator NbtByteArray(byte[] nbt) => new NbtByteArray(nbt);
		public static implicit operator NbtByteArray(List<byte> nbt) => new NbtByteArray(nbt.ToArray());
	}
}
