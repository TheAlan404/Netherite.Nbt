using System.Collections.Generic;

namespace LibNbt.Tags {
    interface INbtTagList {
        List<NbtTag> Tags { get; }

        T Get<T>( int tagIdx ) where T : NbtTag;
    }
}