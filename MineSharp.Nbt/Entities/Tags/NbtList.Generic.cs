using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSlate.Nbt.Entities
{
	public class NbtList<T> : NbtList
		where T : NbtTag, new()
    {
		public override NbtTagType TagType => GetTagTypeNbt<T>();

		public NbtList() { }

        public NbtList(string? tagName)
            : base(tagName) { }

		public NbtList(IEnumerable<T> tags)
			: this(null, tags) { }

		public NbtList(string? tagName, IEnumerable<T> tags)
			: base(tagName)
		{
			if (tags == null) throw new ArgumentNullException(nameof(tags));
			foreach (T tag in tags)
			{
				Add(tag);
			}
		}

		public NbtList(NbtList other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			_name = other._name;
			foreach (T tag in other.Tags)
			{
				Add((T)tag.Clone());
			}
		}

		public NbtList(NbtList<T> other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			_name = other._name;
			foreach (T tag in other.Tags)
			{
				Add((T)tag.Clone());
			}
		}

		public new T this[int tagIndex]
		{
			get => (T)Tags[tagIndex];
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

        public void AddRange(IEnumerable<T> newTags)
        {
            if (newTags == null) throw new ArgumentNullException(nameof(newTags));
            foreach (NbtTag tag in newTags)
            {
                Add(tag);
            }
        }

		public void Add(T tag)
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

		/// <inheritdoc />
		public override object Clone()
        {
            return new NbtList<T>(this);
        }

		// hehe this is why we have generic'd list

		public static implicit operator NbtList<T>(List<T> list) => new NbtList<T>((IEnumerable<T>)list);
		public static implicit operator List<T>(NbtList<T> nbtList) => new List<T>(nbtList.Tags.Cast<T>());
	}
}
