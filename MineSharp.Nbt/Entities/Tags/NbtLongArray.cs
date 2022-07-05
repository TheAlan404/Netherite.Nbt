using Dennis.BinaryUtils;
using DeepSlate.Nbt.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepSlate.Nbt.Entities
{
    /// <summary> A tag containing an array of signed 64-bit integers. </summary>
    public sealed class NbtLongArray : NbtTag
    {
		/// <summary> Type of this tag (LongArray). </summary>
		public override NbtTagType TagType => NbtTagType.LongArray;

		/// <summary> Value/payload of this tag (an array of signed 64-bit integers). Value is stored as-is and is NOT cloned. May not be <c>null</c>. </summary>
		public long[] Value = Array.Empty<long>();

		public NbtLongArray()
		{

		}

		public NbtLongArray(long[] value)
			: this(null, value) { }

		public NbtLongArray(string? tagName, long[] value)
			: base(tagName)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			Value = (long[])value.Clone();
		}

		public NbtLongArray(NbtLongArray other)
			: this(other.Name, other.Value) { }

		/// <summary> Gets or sets a long at the given index. </summary>
		/// <param name="index"> The zero-based index of the element to get or set. </param>
		/// <returns> The long at the specified index. </returns>
		/// <exception cref="IndexOutOfRangeException"> <paramref name="index"/> is outside the array bounds. </exception>
		public new long this[int index]
		{
			get => Value[index];
			set => Value[index] = value;
		}

		internal override void ReadTag(BinaryReader readStream)
		{
			int length = readStream.Read<int>();
			if (length < 0)
			{
				throw new NbtFormatException("Negative length given in TAG_Int_Array");
			}

			Value = new long[length];
			for (int i = 0; i < length; i++)
			{
				Value[i] = readStream.Read<long>();
			}
		}

		internal override void WriteTag(BinaryWriter writeStream)
		{
			if (Name == null) throw new NbtFormatException("Name is null");
			writeStream.Write((byte)NbtTagType.LongArray);
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
			return new NbtLongArray(this);
		}


		internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
		{
			base.PrettyPrint(sb, indentString, indentLevel);
			sb.AppendFormat($"[{Value.Length} longs]");
		}

		public static implicit operator long[](NbtLongArray nbt) => nbt.Value;
		public static implicit operator List<long>(NbtLongArray nbt) => nbt.Value.ToList();
		public static implicit operator NbtLongArray(long[] nbt) => new NbtLongArray(nbt);
		public static implicit operator NbtLongArray(List<long> nbt) => new NbtLongArray(nbt.ToArray());
	}
}
