using Dennis.BinaryUtils;
using DeepSlate.Nbt.Exceptions;
using System;
using System.IO;
using System.Text;

namespace DeepSlate.Nbt.Entities
{
	/// <summary> A tag containing a single string. String is stored in UTF-8 encoding. </summary>
	public sealed class NbtString : NbtTag
    {
		public override NbtTagType TagType => NbtTagType.String;

		public string? Value;

		public NbtString()
		{

		}

		public NbtString(string value)
			: this(null, value) { }

		public NbtString(string? tagName, string value)
			: base(tagName)
		{
			_name = tagName;
			Value = value;
		}

		/// <summary> Creates a copy of given NbtByte tag. </summary>
		/// <param name="other"> Tag to copy. May not be <c>null</c>. </param>
		public NbtString(NbtString other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			Name = other.Name;
			Value = other.Value;
		}

		public override object Clone()
		{
			return new NbtString(this);
		}

		internal override void ReadTag(BinaryReader readStream)
		{
			ushort len = readStream.Read<ushort>();
			Value = Encoding.UTF8.GetString(readStream.ReadBytes(len));
		}

		internal override void WriteData(BinaryWriter writeStream)
		{
			if (Value == null) throw new ArgumentNullException(nameof(Value));
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
			sb.Append($"'{Value}'");
		}

		public static implicit operator string(NbtString nbt) => nbt.Value ?? "";
		public static implicit operator NbtString(string nbt) => new NbtString(nbt);
	}
}
