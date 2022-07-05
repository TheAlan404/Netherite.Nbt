using Dennis.BinaryUtils;
using DeepSlate.Nbt.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepSlate.Nbt.Entities
{
    /// <summary> A tag containing a list of unnamed tags, all of the same kind. </summary>
    public class NbtList : NbtTag, IEnumerable<NbtTag>
	{
		/// <summary> Type of this tag (List). </summary>
		public override NbtTagType TagType => NbtTagType.List;

		public List<NbtTag> Tags = new List<NbtTag>();
        public NbtTagType ListType = NbtTagType.Unknown;

        public NbtList() { }

        public NbtList(string? tagName)
            : base(tagName) { }

		public NbtList(IEnumerable<NbtTag> tags)
			: this(null, tags) { }

		public NbtList(string? tagName, IEnumerable<NbtTag> tags)
			: base(tagName)
		{
			if (tags == null) throw new ArgumentNullException(nameof(tags));
			foreach (NbtTag tag in tags)
			{
				Add(tag);
			}
		}

		public NbtList(NbtList other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			_name = other._name;
			foreach (NbtTag tag in other.Tags)
			{
				Add((NbtTag)tag.Clone());
			}
		}

		public new NbtTag this[int tagIndex]
		{
			get => Tags[tagIndex];
			set
			{
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                else if (value.Parent != null)
                    throw new ArgumentException("A tag may only be added to one compound/list at a time.");
                else if (value == this || value == Parent)
                    throw new ArgumentException("A list tag may not be added to itself or to its child tag.");
                else if (value.Name != null)
                    throw new ArgumentException("Named tag given. A list may only contain unnamed tags.");
				if (ListType != NbtTagType.Unknown && value.TagType != ListType)
					throw new ArgumentException("Items must be of type " + ListType);
				Tags[tagIndex] = value;
				value.Parent = this;
			}
		}

        public void AddRange(IEnumerable<NbtTag> newTags)
        {
            if (newTags == null) throw new ArgumentNullException(nameof(newTags));
            foreach (NbtTag tag in newTags)
            {
                Add(tag);
            }
        }

		public void Add(NbtTag tag)
		{
			if (tag == null) throw new ArgumentNullException(nameof(tag));
			if (tag.Parent != null) throw new ArgumentException("A tag may only be added to one compound/list at a time.");
			if (tag == this) throw new ArgumentException("Cannot add tag to itself");
			if (tag.Name != null) tag.Name = null;
			if (ListType != NbtTagType.Unknown && tag.TagType != ListType)
			{
				throw new ArgumentException("Items in this list must be of type " + ListType + ". Given type: " +
											tag.TagType);
			}
			Tags.Add(tag);
			tag.Parent = this;
			if (ListType == NbtTagType.Unknown) ListType = tag.TagType;
		}

		#region Reading / Writing

		// -- NbtWriter and NbtReader takes care of this --
		internal override void ReadTag(BinaryReader readStream)
			=> throw new NotImplementedException("NbtReader shouldn't call this");
		internal override void WriteTag(BinaryWriter writeStream)
			=> throw new NotImplementedException("NbtWriter shouldn't call this");
		internal override void WriteData(BinaryWriter writeStream)
			=> throw new NotImplementedException("NbtWriter shouldn't call this");

		#endregion

		/// <inheritdoc />
		public override object Clone()
        {
            return new NbtList(this);
        }


        internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
        {
            base.PrettyPrint(sb, indentString, indentLevel);
            sb.AppendLine($"{Tags.Count} {(Tags.Count == 1 ? "entry" : "entries")}");
			for (int i = 0; i < indentLevel; i++)
			{
				sb.Append(indentString);
			}
			sb.Append($"{{");
			if (Tags.Count > 0)
            {
                sb.Append('\n');
                foreach (NbtTag tag in Tags)
                {
                    tag.PrettyPrint(sb, indentString, indentLevel + 1);
                    sb.Append('\n');
                }
                for (int i = 0; i < indentLevel; i++)
                {
                    sb.Append(indentString);
                }
            }
            sb.Append('}');
        }

		public IEnumerator<NbtTag> GetEnumerator()
		{
			return ((IEnumerable<NbtTag>)Tags).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Tags).GetEnumerator();
		}

		public static implicit operator NbtList(List<NbtTag> list) => new NbtList((IEnumerable<NbtTag>)list);
		public static implicit operator List<NbtTag>(NbtList nbtList) => new List<NbtTag>(nbtList.Tags);
	}
}
