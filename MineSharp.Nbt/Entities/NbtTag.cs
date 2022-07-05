using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepSlate.Nbt.Entities
{
	/// <summary> Base class for different kinds of named binary tags. </summary>
	public abstract class NbtTag : ICloneable, IConvertible
	{
		internal virtual bool isRoot => false;

		/// <summary> Parent compound tag, either NbtList or NbtCompound, if any.
		/// May be <c>null</c> for detached tags. </summary>
		public NbtTag? Parent { get; internal set; }

		/// <summary> Type of this tag. </summary>
		public abstract NbtTagType TagType { get; }

		public string? CannonicalName => GetCanonicalTagName(TagType);

		/// <summary>
		/// Creates a new tag
		/// </summary>
		public NbtTag(string? tagName = null)
		{
			Name = isRoot ? "" : tagName;
		}


		/// <summary> Returns true if tags of this type have a value attached.
		/// All tags except Compound, List, and End have values. </summary>
		public bool HasValue
		{
			get
			{
				switch (TagType)
				{
					case NbtTagType.Compound:
					case NbtTagType.End:
					case NbtTagType.List:
					case NbtTagType.Unknown:
						return false;
					default:
						return true;
				}
			}
		}

		/// <summary> Name of this tag.</summary>
		/// <exception cref="ArgumentNullException"> If <paramref name="value"/> is <c>null</c>, and <c>Parent</c> tag is an NbtCompound.
		/// Name of tags inside an <c>NbtCompound</c> may not be null. </exception>
		/// <exception cref="ArgumentException"> If this tag resides in an <c>NbtCompound</c>, and a sibling tag with the name already exists. </exception>
		public string? Name
		{
			get => _name;
			set
			{
				if (_name == value)
				{
					return;
				}

				if (Parent is NbtCompound parentAsCompound)
				{
					if (value == null)
					{
						throw new ArgumentNullException(nameof(value),
														"Name of tags inside an NbtCompound may not be null.");
					}
					else if (_name != null)
					{
						parentAsCompound.RenameTag(_name, value);
					}
				}

				_name = value;
			}
		}

		internal string? _name;

		/// <summary> Gets the full name of this tag, including all parent tag names, separated by dots. 
		/// Unnamed tags show up as empty strings. </summary>
		public string Path
		{
			get
			{
				if (Parent == null || Parent.isRoot)
				{
					return Name ?? "";
				}
				if (Parent is NbtList parentAsList)
				{
					return $"{parentAsList.Path}[{parentAsList.Tags.IndexOf(this)}]";
				}
				else
				{
					return $"{Parent.Path}.{Name}";
				}
			}
		}

		internal abstract void ReadTag(BinaryReader readStream);

		//internal abstract void SkipTag(BinaryReader readStream);

		internal abstract void WriteTag(BinaryWriter writeReader);

		// WriteData does not write the tag's ID byte or the name
		internal abstract void WriteData(BinaryWriter writeStream);


		#region Shortcuts

		/// <summary> Gets or sets the tag with the specified name. May return <c>null</c>. </summary>
		/// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
		/// <param name="tagName"> The name of the tag to get or set. Must match tag's actual name. </param>
		/// <exception cref="InvalidOperationException"> If used on a tag that is not NbtCompound. </exception>
		/// <remarks> ONLY APPLICABLE TO NbtCompound OBJECTS!
		/// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
		public virtual NbtTag? this[string tagName]
		{
			get => TagType == NbtTagType.Compound ? ((NbtCompound)this)[tagName] : throw new InvalidOperationException("String indexers only work on NbtCompound tags.");
			set
			{
				if (TagType == NbtTagType.Compound)
					((NbtCompound)this)[tagName] = value;
				else throw new InvalidOperationException("String indexers only work on NbtCompound tags.");
			}
		}

		/// <summary> Gets or sets the tag at the specified index. </summary>
		/// <returns> The tag at the specified index. </returns>
		/// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
		/// <exception cref="ArgumentOutOfRangeException"> tagIndex is not a valid index in this tag. </exception>
		/// <exception cref="ArgumentNullException"> Given tag is <c>null</c>. </exception>
		/// <exception cref="ArgumentException"> Given tag's type does not match ListType. </exception>
		/// <exception cref="InvalidOperationException"> If used on a tag that is not NbtList, NbtByteArray, or NbtIntArray. </exception>
		/// <remarks> Only works on NbtList, NbtByteArray, and NbtIntArray.
		/// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
		public virtual NbtTag? this[int tagIndex]
		{
			get
			{
				return TagType switch
				{
					NbtTagType.List => ((NbtList)this)[tagIndex],
					NbtTagType.ByteArray => ((NbtByteArray)this)[tagIndex],
					NbtTagType.IntArray => ((NbtIntArray)this)[tagIndex],
					NbtTagType.LongArray => ((NbtLongArray)this)[tagIndex],
					_ => throw new InvalidOperationException($"The {TagType} tag isnt integer indexable!"),
				};
			}
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));

				switch (TagType)
				{
					case NbtTagType.List:
						((NbtList)this)[tagIndex] = value;
						break;
					case NbtTagType.ByteArray:
						((NbtByteArray)this)[tagIndex] = value;
						break;
					case NbtTagType.IntArray:
						((NbtIntArray)this)[tagIndex] = value;
						break;
					case NbtTagType.LongArray:
						((NbtLongArray)this)[tagIndex] = value;
						break;
					default:
						throw new InvalidOperationException($"The {TagType} tag isnt integer indexable!");
				};
			}
		}
		#endregion

		#region static helpers

		/// <summary> Returns a canonical (Notchy) name for the given NbtTagType,
		/// e.g. "TAG_Byte_Array" for NbtTagType.ByteArray </summary>
		/// <param name="type"> NbtTagType to name. </param>
		/// <returns> String representing the canonical name of a tag,
		/// or null of given TagType does not have a canonical name (e.g. Unknown). </returns>
		public static string GetCanonicalTagName(NbtTagType type)
		{
			return type switch
			{
				NbtTagType.Byte => "TAG_Byte",
				NbtTagType.ByteArray => "TAG_Byte_Array",
				NbtTagType.Compound => "TAG_Compound",
				NbtTagType.Double => "TAG_Double",
				NbtTagType.End => "TAG_End",
				NbtTagType.Float => "TAG_Float",
				NbtTagType.Int => "TAG_Int",
				NbtTagType.IntArray => "TAG_Int_Array",
				NbtTagType.LongArray => "TAG_Long_Array",
				NbtTagType.List => "TAG_List",
				NbtTagType.Long => "TAG_Long",
				NbtTagType.Short => "TAG_Short",
				NbtTagType.String => "TAG_String",
				NbtTagType.Unknown => "",
				_ => "",
			};
		}


		internal static NbtTagType GetTagType<T>()
		{
			return Type.GetTypeCode(typeof(T)) switch
			{
				TypeCode.Byte => NbtTagType.Byte,
				TypeCode.Double => NbtTagType.Double,
				TypeCode.Single => NbtTagType.Float,
				TypeCode.Int32 => NbtTagType.Int,
				TypeCode.Int64 => NbtTagType.Long,
				TypeCode.Int16 => NbtTagType.Short,
				TypeCode.String => NbtTagType.String,
				_ => NbtTagType.Unknown,
			};
		}

		internal static NbtTagType GetTagTypeNbt<T>()
			where T : NbtTag, new()
		{
			return new T().TagType;
		}

		internal static NbtTagType GetTagTypeNbt(Type type)
		{
			return ((NbtTag?)Activator.CreateInstance(type))?.TagType ?? NbtTagType.Unknown;
		}

		internal static Type? GeTypeofNbtTag(NbtTagType tagType)
		{
			return tagType switch
			{
				NbtTagType.Byte => typeof(NbtByte),
				NbtTagType.Short => typeof(NbtShort),
				NbtTagType.Int => typeof(NbtInt),
				NbtTagType.Long => typeof(NbtLong),
				NbtTagType.Float => typeof(NbtFloat),
				NbtTagType.Double => typeof(NbtDouble),
				NbtTagType.String => typeof(NbtString),
				NbtTagType.ByteArray => typeof(NbtByteArray),
				NbtTagType.IntArray => typeof(NbtIntArray),
				NbtTagType.LongArray => typeof(NbtLongArray),
				NbtTagType.List => typeof(NbtList),
				NbtTagType.Compound => typeof(NbtCompound),
				_ => null,
			};
		}

		#endregion

		/// <summary> Creates a deep copy of this tag. </summary>
		/// <returns> A new NbtTag object that is a deep copy of this instance. </returns>
		public abstract object Clone();

		#region Print

		/// <summary> Prints contents of this tag, and any child tags, to a string.
		/// Indents the string using multiples of the given indentation string. </summary>
		/// <returns> A string representing contents of this tag, and all child tags (if any). </returns>
		public override string ToString()
			=> ToString(DefaultIndentString);

		/// <summary> Prints contents of this tag, and any child tags, to a string.
		/// Indents the string using multiples of the given indentation string. </summary>
		/// <param name="indentString"> String to be used for indentation. </param>
		/// <returns> A string representing contents of this tag, and all child tags (if any). </returns>
		public string ToString(string indentString)
		{
			if (indentString == null) throw new ArgumentNullException(nameof(indentString));
			StringBuilder sb = new StringBuilder();
			PrettyPrint(sb, indentString, 0);
			return sb.ToString();
		}


		internal virtual void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
		{
			for (int i = 0; i < indentLevel; i++)
			{
				sb.Append(indentString);
			}
			sb.Append(CannonicalName);
			if (!string.IsNullOrEmpty(Name))
			{
				sb.Append($"(\'{Name}\')");
			}
			else
			{
				sb.Append(isRoot ? "('')" : "(None)");
			}
			sb.Append(": ");
		}

		/// <summary> String to use for indentation in NbtTag's and NbtFile's ToString() methods by default. </summary>
		public static string DefaultIndentString
		{
			get => _defaultIndentString;
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				_defaultIndentString = value;
			}
		}

		private static string _defaultIndentString = "\t";

		#endregion

		#region CASTS

		// object -> Nbt

		public static implicit operator NbtTag(byte v) => new NbtTag<byte>(v);
		public static implicit operator NbtTag(double v) => new NbtTag<double>(v);
		public static implicit operator NbtTag(float v) => new NbtTag<float>(v);
		public static implicit operator NbtTag(int v) => new NbtTag<int>(v);
		public static implicit operator NbtTag(long v) => new NbtTag<long>(v);
		public static implicit operator NbtTag(short v) => new NbtTag<short>(v);
		public static implicit operator NbtTag(string v) => new NbtString(v);
		public static implicit operator NbtTag(byte[] v) => new NbtByteArray(v);
		public static implicit operator NbtTag(int[] v) => new NbtIntArray(v);
		public static implicit operator NbtTag(long[] v) => new NbtLongArray(v);

		public static implicit operator NbtTag(List<byte> v) => new NbtByteArray(v);
		public static implicit operator NbtTag(List<int> v) => new NbtIntArray(v);
		public static implicit operator NbtTag(List<long> v) => new NbtLongArray(v);

		public static implicit operator NbtTag(List<NbtTag> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtByte> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtDouble> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtFloat> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtInt> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtLong> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtShort> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtString> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtByteArray> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtIntArray> v) => new NbtList(v);
		public static implicit operator NbtTag(List<NbtLongArray> v) => new NbtList(v);

		public static implicit operator NbtTag(List<NbtCompound> v) => new NbtList(v);

		public static implicit operator NbtTag(Dictionary<string, NbtTag> v) => new NbtCompound(v);

		public static implicit operator NbtTag(bool v) => new NbtTag<byte>((byte)(v ? 0x01 : 0x00));

		// Nbt -> object

		public static implicit operator byte(NbtTag v) => ((NbtByte)v).Value;
		public static implicit operator double(NbtTag v) => ((NbtDouble)v).Value;
		public static implicit operator float(NbtTag v) => ((NbtFloat)v).Value;
		public static implicit operator int(NbtTag v) => ((NbtInt)v).Value;
		public static implicit operator long(NbtTag v) => ((NbtLong)v).Value;
		public static implicit operator short(NbtTag v) => ((NbtShort)v).Value;
		public static implicit operator string(NbtTag v) => ((NbtString)v).Value ?? "";
		public static implicit operator byte[](NbtTag v) => ((NbtByteArray)v).Value;
		public static implicit operator int[](NbtTag v) => ((NbtIntArray)v).Value;
		public static implicit operator long[](NbtTag v) => ((NbtLongArray)v).Value;

		public static implicit operator List<byte>(NbtTag v) => ((NbtByteArray)v).Value.ToList();
		public static implicit operator List<int>(NbtTag v) => ((NbtIntArray)v).Value.ToList();
		public static implicit operator List<long>(NbtTag v) => ((NbtLongArray)v).Value.ToList();

		public static implicit operator List<NbtTag>(NbtTag v) => ((NbtList)v).Tags;
		public static implicit operator List<NbtByte>(NbtTag v) => ((NbtList<NbtByte>)v).Tags.Cast<NbtByte>().ToList();
		public static implicit operator List<NbtDouble>(NbtTag v) => ((NbtList<NbtDouble>)v).Tags.Cast<NbtDouble>().ToList();
		public static implicit operator List<NbtFloat>(NbtTag v) => ((NbtList<NbtFloat>)v).Tags.Cast<NbtFloat>().ToList();
		public static implicit operator List<NbtInt>(NbtTag v) => ((NbtList<NbtInt>)v).Tags.Cast<NbtInt>().ToList();
		public static implicit operator List<NbtLong>(NbtTag v) => ((NbtList<NbtLong>)v).Tags.Cast<NbtLong>().ToList();
		public static implicit operator List<NbtShort>(NbtTag v) => ((NbtList<NbtShort>)v).Tags.Cast<NbtShort>().ToList();
		public static implicit operator List<NbtString>(NbtTag v) => ((NbtList<NbtString>)v).Tags.Cast<NbtString>().ToList();
		public static implicit operator List<NbtByteArray>(NbtTag v) => ((NbtList<NbtByteArray>)v).Tags.Cast<NbtByteArray>().ToList();
		public static implicit operator List<NbtIntArray>(NbtTag v) => ((NbtList<NbtIntArray>)v).Tags.Cast<NbtIntArray>().ToList();
		public static implicit operator List<NbtLongArray>(NbtTag v) => ((NbtList<NbtLongArray>)v).Tags.Cast<NbtLongArray>().ToList();

		public static implicit operator List<NbtCompound>(NbtTag v) => ((NbtList<NbtCompound>)v).Tags.Cast<NbtCompound>().ToList();

		public static implicit operator Dictionary<string, NbtTag>(NbtTag nbt) => (NbtCompound)nbt;

		public static implicit operator bool(NbtTag v) => ((NbtTag<byte>)v).Value == 0x01;

		#endregion

		#region IConvertible

		public TypeCode GetTypeCode() => TypeCode.Object;

		public bool ToBoolean(IFormatProvider? provider) => (bool)this;

		public byte ToByte(IFormatProvider? provider) => (byte)this;

		public char ToChar(IFormatProvider? provider) => (char)this;

		public double ToDouble(IFormatProvider? provider) => (double)this;

		public short ToInt16(IFormatProvider? provider) => (short)this;

		public int ToInt32(IFormatProvider? provider) => (int)this;

		public long ToInt64(IFormatProvider? provider) => (long)this;


		public float ToSingle(IFormatProvider? provider) => (float)this;

		public string ToString(IFormatProvider? provider) => (string)this;

		// unsupported:

		public sbyte ToSByte(IFormatProvider? provider) => (sbyte)this;

		public ushort ToUInt16(IFormatProvider? provider) => (ushort)this;

		public uint ToUInt32(IFormatProvider? provider) => (uint)this;

		public ulong ToUInt64(IFormatProvider? provider) => (ulong)this;

		public decimal ToDecimal(IFormatProvider? provider) => (decimal)this;

		public object ToType(Type type, IFormatProvider? provider)
		{
			Console.WriteLine($"totype {type}");

			if (type.IsConstructedGenericType &&
				type.GetGenericTypeDefinition() == typeof(List<>))
			{
				Type generic = type.GetGenericArguments()[0];
				var tags = ((NbtList)this).Tags;

				// THIS IS A REALLY SHITTY WAY TO DO IT
				// BUT IT FUCKING WORKS

				// PLEASE DO TELL IF YOU HAVE ANY IDEA TO IMPROVE THIS ABOMINATION
				// even amity would be scared of this tbh
				// btw problem is we cant use Cast<>() (tried using reflection and no doesnt work out)
				// or Select => ChangeType (makes it List<object>)

				var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(generic))!;
				foreach (var tag in tags)
				{
					list.Add(Convert.ChangeType(tag, generic));
				}

				return list;
			}

			if(type.IsConstructedGenericType &&
				type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				return Convert.ChangeType(this, type.GetGenericArguments()[0]);
			}

			if(TagType == NbtTagType.Compound)
			{
				return NbtConvert.DeserializeCompound(type, (NbtCompound)this);
			}

			throw new NotImplementedException($"NbtTag cannot convert to {type}");
		}

		public DateTime ToDateTime(IFormatProvider? provider)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
