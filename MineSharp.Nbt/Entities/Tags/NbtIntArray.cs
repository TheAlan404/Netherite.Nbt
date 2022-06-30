using Dennis.BinaryUtils;
using MineSharp.Nbt.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MineSharp.Nbt.Entities
{
	/// <summary> A tag containing an array of signed 32-bit integers. </summary>
	public sealed class NbtIntArray : NbtTag
	{
		/// <summary> Type of this tag (ByteArray). </summary>
		public override NbtTagType TagType => NbtTagType.IntArray;

		public int[] Value = Array.Empty<int>();

		public NbtIntArray()
		{

		}

		public NbtIntArray(int[] value)
			: this(null, value) { }

		public NbtIntArray(string? tagName, int[] value)
			: base(tagName)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			Value = (int[])value.Clone();
		}

		public NbtIntArray(NbtIntArray other)
			: this(other.Name, other.Value) { }


		/// <summary> Gets or sets an integer at the given index. </summary>
		/// <param name="tagIndex"> The zero-based index of the element to get or set. </param>
		/// <returns> The integer at the specified index. </returns>
		/// <exception cref="IndexOutOfRangeException"> <paramref name="tagIndex"/> is outside the array bounds. </exception>
		public new int this[int tagIndex]
		{
			get => Value[tagIndex];
			set => Value[tagIndex] = value;
		}


		internal override void ReadTag(BinaryReader readStream)
		{
			int length = readStream.Read<int>();
			if (length < 0)
			{
				throw new NbtFormatException("Negative length given in TAG_Int_Array");
			}

			Value = new int[length];
			for (int i = 0; i < length; i++)
			{
				Value[i] = readStream.Read<int>();
			}
		}

		internal override void WriteTag(BinaryWriter writeStream)
		{
			if (Name == null) throw new NbtFormatException("Name is null");
			writeStream.Write((byte)NbtTagType.IntArray);
			writeStream.Write(Name);
			WriteData(writeStream);
		}


		internal override void WriteData(BinaryWriter writeStream)
		{
			writeStream.Write(Value.Length);
			for (int i = 0; i < Value.Length; i++)
			{
				writeStream.Write(Value[i]);
			}
		}


		/// <inheritdoc />
		public override object Clone()
		{
			return new NbtIntArray(this);
		}


		internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
		{
			base.PrettyPrint(sb, indentString, indentLevel);
			sb.AppendFormat($"[{Value.Length} ints]");
		}

		public static implicit operator int[](NbtIntArray nbt) => nbt.Value;
		public static implicit operator List<int>(NbtIntArray nbt) => nbt.Value.ToList();
		public static implicit operator NbtIntArray(int[] nbt) => new NbtIntArray(nbt);
		public static implicit operator NbtIntArray(List<int> nbt) => new NbtIntArray(nbt.ToArray());
	}
}
