using Dennis.BinaryUtils;
using MineSharp.Nbt.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MineSharp.Nbt.Entities
{
	/// <summary> A tag containing a set of other named tags. Order is not guaranteed. </summary>
	public class NbtCompound : NbtTag, IEnumerable<KeyValuePair<string, NbtTag>>
    {
		/// <summary> Type of this tag (Compound). </summary>
		public override NbtTagType TagType => NbtTagType.Compound;

        private Dictionary<string, NbtTag> _tags = new();

		public IDictionary<string, NbtTag> Tags => _tags;
		

		public NbtCompound()
		{

		}

		public NbtCompound(string tagName)
            : base(tagName)
		{

		}

		public NbtCompound(IEnumerable<NbtTag> tags)
            : this(null, tags) { }

        public NbtCompound(IDictionary<string, NbtTag> tags)
            : this(null, tags) { }

        /// <summary> Creates an NbtByte tag with the given name, containing the given tags. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="tags"> Collection of tags to assign to this tag's Value. May not be null </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>, or one of the tags is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If some of the given tags were not named, or two tags with the same name were given. </exception>
        public NbtCompound(string? tagName, IEnumerable<NbtTag> tags)
            : base(tagName)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            foreach (NbtTag tag in tags)
            {
                Add(tag);
            }
        }

        public NbtCompound(string? tagName, IDictionary<string, NbtTag> tags)
            : base(tagName)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            foreach (var kvp in tags)
            {
                Add(kvp.Key, kvp.Value);
            }
        }


        /// <summary> Creates a deep copy of given NbtCompound. </summary>
        /// <param name="other"> An existing NbtCompound to copy. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="other"/> is <c>null</c>. </exception>
        public NbtCompound(NbtCompound other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            _name = other._name;
            foreach (NbtTag tag in other._tags.Values)
            {
                Add((NbtTag)tag.Clone());
            }
        }


        /// <summary> Gets or sets the tag with the specified name. May return <c>null</c>. </summary>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <param name="tagName"> The name of the tag to get or set. Must match tag's actual name. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>; or if trying to assign null value. </exception>
        /// <exception cref="ArgumentException"> <paramref name="tagName"/> does not match the given tag's actual name;
        /// or given tag already has a Parent. </exception>
        public override NbtTag? this[string tagName]
        {

            get => Get<NbtTag>(tagName);
            set
            {
                if (value == null)
                {
                    Remove(tagName);
                    return;
                }
                Add(tagName, value);
            }
        }


        /// <summary> Gets the tag with the specified name. May return <c>null</c>. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        public T? Get<T>(string tagName) where T : NbtTag
        {
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));
            return _tags.ContainsKey(tagName) ? (T)_tags[tagName] : null;
        }


        /// <summary> Gets the tag with the specified name. May return <c>null</c>. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        public NbtTag? Get(string tagName)
        {
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));
            return _tags.ContainsKey(tagName) ? _tags[tagName] : null;
        }

        public void AddRange(IEnumerable<NbtTag> newTags)
        {
            if (newTags == null) throw new ArgumentNullException(nameof(newTags));
            foreach (NbtTag tag in newTags)
            {
                Add(tag);
            }
        }

        public void AddRange(IDictionary<string, NbtTag> newTags)
        {
            if (newTags == null) throw new ArgumentNullException(nameof(newTags));
            foreach (var kvp in newTags)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public void Add(NbtTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (tag.Name == null) throw new ArgumentException("Tag name cannot be null!");
            if (tag.Parent != null) throw new ArgumentException("A tag may only be added to one compound/list at a time.");
            if (tag == this) throw new ArgumentException("Cannot add tag to itself");
            _tags[tag.Name] = tag;
            tag.Parent = this;
        }

        public void Add(string name, NbtTag tag)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            tag.Name = name;
            Add(tag);
        }

        public bool Contains(string tagName)
        {
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));
            return _tags.ContainsKey(tagName);
        }


        /// <summary> Removes the tag with the specified name from this NbtCompound. </summary>
        /// <param name="tagName"> The name of the tag to remove. </param>
        /// <returns> true if the tag is successfully found and removed; otherwise, false.
        /// This method returns false if name is not found in the NbtCompound. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        public bool Remove(string tagName)
        {
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));
            NbtTag? tag = Get(tagName);
            if (tag == null) return false;
            tag.Parent = null;
            _tags.Remove(tagName);
            return true;
        }

        internal void RenameTag(string oldName, string newName)
        {
            if (Contains(newName)) throw new ArgumentException("Cannot rename: a tag with the name already exists in this compound.");
			if (!Contains(oldName)) throw new ArgumentException("Cannot rename: no tag found to rename.");
            NbtTag? tag = Get(oldName);
            if (tag == null) throw new ArgumentException("blame dennis");
			_tags.Remove(oldName);
            _tags.Add(newName, tag);
        }

        // -- NbtWriter and NbtReader takes care of this --
        internal override void ReadTag(BinaryReader readStream)
            => throw new NotImplementedException("NbtReader shouldn't call this");
        internal override void WriteTag(BinaryWriter writeStream)
			=> throw new NotImplementedException("NbtWriter shouldn't call this");
		internal override void WriteData(BinaryWriter writeStream)
			=> throw new NotImplementedException("NbtWriter shouldn't call this");


		/// <inheritdoc />
		public override object Clone()
        {
            return new NbtCompound(this);
        }


        internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
        {
            base.PrettyPrint(sb, indentString, indentLevel);
            sb.AppendFormat($"{_tags.Count} entries {{");
            if (_tags.Count > 0)
            {
                sb.Append('\n');
                foreach (NbtTag tag in _tags.Values)
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

		public IEnumerator<KeyValuePair<string, NbtTag>> GetEnumerator()
		{
			return Tags.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Tags).GetEnumerator();
		}

		public static implicit operator Dictionary<string, NbtTag>(NbtCompound nbt) => (Dictionary<string, NbtTag>)nbt.Tags;
        public static explicit operator NbtCompound(Dictionary<string, NbtTag> nbt) => new NbtCompound(nbt);
	}
}
