using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dennis.BinaryUtils;
using MineSharp.Nbt.Exceptions;

namespace MineSharp.Nbt.Entities
{
	public class NbtTag<T> : NbtTag
	{
		public override NbtTagType TagType => GetTagType<T>();

		public T? Value;

		public NbtTag()
		{

		}

		public NbtTag(T value)
			: this(null, value) { }

		public NbtTag(string? tagName, T value)
			: base(tagName)
		{
			_name = tagName;
			Value = value;
		}

		/// <summary> Creates a copy of given NbtByte tag. </summary>
		/// <param name="other"> Tag to copy. May not be <c>null</c>. </param>
		public NbtTag(NbtTag<T> other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			Name = other.Name;
			Value = other.Value;
		}

		public override object Clone()
		{
			return new NbtTag<T>(this);
		}

		internal override void ReadTag(BinaryReader readStream)
		{
			Value = readStream.Read<T>();
		}

		internal override void WriteData(BinaryWriter writeStream)
		{
			if(Value == null) throw new ArgumentNullException(nameof(Value));
			writeStream.Write(Value); // Write<T>
		}

		internal override void WriteTag(BinaryWriter writeStream)
		{
			if (Name == null) throw new NbtFormatException("Name is null");
			writeStream.Write((byte)TagType);
			writeStream.Write(Name);
			WriteData(writeStream);
		}

		internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
		{
			base.PrettyPrint(sb, indentString, indentLevel);
			sb.Append(Value);
		}

		public static implicit operator NbtTag<T>(T value) => new NbtTag<T>(value);
		public static implicit operator T?(NbtTag<T> nbt) => nbt.Value;
	}
}
