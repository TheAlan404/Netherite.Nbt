using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    internal struct DynamicConverter {
        readonly Type type;
        readonly ConversionOptions options;
        readonly TypeMetadata typeMetadata;


        public DynamicConverter([NotNull] Type type, [NotNull] ConversionOptions options) {
            if (type == null) throw new ArgumentNullException("type");
            if (options == null) throw new ArgumentNullException("options");
            this.type = type;
            this.options = options;
            typeMetadata = TypeMetadata.ReadTypeMetadata(type);
        }


        public NbtTag FillTag([CanBeNull] object obj, [NotNull] NbtTag tag) {
            if (tag == null) throw new ArgumentNullException("tag");
            throw new NotImplementedException();
        }


        public object FillObject([NotNull] object obj, [NotNull] NbtTag tag) {
            if (obj == null) throw new ArgumentNullException("obj");
            if (tag == null) throw new ArgumentNullException("tag");
            throw new NotImplementedException();
        }


        static NbtTag HandlePrimitiveOrEnum(string tagName, object value, Type type) {
            Type convertedType = SerializationUtil.GetConvertedType(type);


            // Native NBT types
            if (type == typeof(int)) {
                return new NbtInt(tagName, (int)value);
            } else if (type == typeof(byte)) {
                return new NbtByte(tagName, (byte)value);
            } else if (type == typeof(short)) {
                return new NbtShort(tagName, (short)value);
            } else if (type == typeof(long)) {
                return new NbtLong(tagName, (long)value);
            } else if (type == typeof(float)) {
                return new NbtFloat(tagName, (float)value);
            } else if (type == typeof(double)) {
                return new NbtDouble(tagName, (double)value);

            } else {
                // Other types convertible to native NBT types
                if (type == typeof(bool)) {
                    byte byteVal = (byte)((bool)value ? 1 : 0);
                    return new NbtByte(tagName, byteVal);
                } else if (type == typeof(sbyte)) {
                    return new NbtByte(tagName, (byte)(sbyte)value);
                } else if (type == typeof(char)) {
                    return new NbtShort(tagName, (short)(char)value);
                } else if (type == typeof(ushort)) {
                    return new NbtShort(tagName, (short)(ushort)value);
                } else if (type == typeof(uint)) {
                    return new NbtInt(tagName, (int)(uint)value);
                } else if (type == typeof(ulong)) {
                    return new NbtLong(tagName, (long)(ulong)value);
                } else if (type == typeof(decimal)) {
                    return new NbtDouble(tagName, (double)(decimal)value);
                } else {
                    throw new ArgumentException("Given type cannot be mapped to native NBT types.");
                }
            }
        }


        bool IsIgnored(PropertyInfo prop) {
            if ((options.IgnoredProperties != null) &&
                options.IgnoredProperties.Contains(prop.Name)) {
                // ignored by options
                return true;
            } else {
                // ignored by type attributes
                return (typeMetadata.IgnoredProperties != null) &&
                       typeMetadata.IgnoredProperties.Contains(prop);
            }
        }


        NullPolicy GetNullPolicy(PropertyInfo prop) {
            if (typeMetadata.NullPolicies != null) {
                NullPolicy result;
                if (typeMetadata.NullPolicies.TryGetValue(prop, out result)) {
                    return result;
                }
            }
            return options.DefaultNullPolicy;
        }


        NullPolicy GetElementNullPolicy(PropertyInfo prop) {
            if (typeMetadata.ElementNullPolicies != null) {
                NullPolicy result;
                if (typeMetadata.ElementNullPolicies.TryGetValue(prop, out result)) {
                    return result;
                }
            }
            return options.DefaultElementNullPolicy;
        }
    }
}
