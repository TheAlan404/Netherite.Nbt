using System;
using System.Linq.Expressions;
using System.Reflection;

namespace fNbt.Serialization {
    internal enum MissingPolicyAttribute {
        Error,
        UseDefault
    }


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


    class DeserializeCodeEmitter : CodeEmitter {
        public override ParameterExpression ReturnValue {
            get { throw new NotImplementedException(); }
        }

        public override Expression GetPreamble() {
            throw new NotImplementedException();
        }


        public override Expression HandlePrimitiveOrEnum(string tagName, PropertyInfo property) {
            throw new NotImplementedException();
        }


        public override Expression HandleDirectlyMappedType(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            throw new NotImplementedException();
        }


        public override Expression HandleINbtSerializable(string tagName, PropertyInfo property) {
            throw new NotImplementedException();
        }


        public override Expression HandleIList(string tagName, PropertyInfo property, Type iListImpl, NullPolicy selfPolicy,
                                               NullPolicy elementPolicy) {
            throw new NotImplementedException();
        }


        public override Expression HandleNbtTag(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            throw new NotImplementedException();
        }


        public override Expression HandleNbtFile(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            throw new NotImplementedException();
        }


        public override Expression HandleCompoundObject(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            throw new NotImplementedException();
        }
    }
}
