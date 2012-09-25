using System.Collections.Generic;
using JetBrains.Annotations;

namespace LibNbt.Tags {
    interface INbtTagList {
        [NotNull]
        List<NbtTag> Tags { get; }

        [CanBeNull]
        T Get<T>( int tagIndex ) where T : NbtTag;
    }
}