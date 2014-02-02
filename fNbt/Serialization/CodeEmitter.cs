using System;
using System.Linq.Expressions;
using System.Reflection;

namespace fNbt.Serialization {
    internal abstract class CodeEmitter {
        public abstract ParameterExpression ReturnValue { get; }

        public abstract Expression GetPreamble();

        public abstract Expression HandlePrimitiveOrEnum(string tagName, PropertyInfo property);

        public abstract Expression HandleDirectlyMappedType(string tagName, PropertyInfo property, NullPolicy selfPolicy);

        public abstract Expression HandleINbtSerializable(string tagName, PropertyInfo property);

        public abstract Expression HandleIList(string tagName, PropertyInfo property, Type iListImpl, NullPolicy selfPolicy, NullPolicy elementPolicy);

        public abstract Expression HandleNbtTag(string tagName, PropertyInfo property, NullPolicy selfPolicy);

        public abstract Expression HandleNbtFile(string tagName, PropertyInfo property, NullPolicy selfPolicy);

        public abstract Expression HandleCompoundObject(string tagName, PropertyInfo property, NullPolicy selfPolicy);
    }
}
