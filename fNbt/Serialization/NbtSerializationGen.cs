using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    public delegate NbtTag NbtSerialize<T>(string tagName, T value);


    public class NbtSerializationGen {
        // NbtCompound.Add(NbtTag)
        static readonly MethodInfo NbtCompoundAddMethod =
            typeof(NbtCompound).GetMethod("Add", new[] { typeof(NbtTag) });

        // NbtList.Add(NbtTag)
        static readonly MethodInfo NbtListAddMethod =
            typeof(NbtList).GetMethod("Add", new[] { typeof(NbtTag) });

        // new ArgumentNullException(string)
        static readonly ConstructorInfo ArgNullConstructor =
            typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) });

        // new NbtCompound(string)
        static readonly ConstructorInfo NbtCompoundConstructor =
            typeof(NbtCompound).GetConstructor(new[] { typeof(string) });

        // new NbtList(string,NbtTagType)
        static readonly ConstructorInfo NbtListConstructor =
            typeof(NbtList).GetConstructor(new[] { typeof(string), typeof(NbtTagType) });


        // Generates specialized methods for serializing objects of given Type to NBT
        [NotNull]
        public static NbtSerialize<T> CreateSerializerForType<T>() {
            // Define function arguments
            ParameterExpression argTagName = Expression.Parameter(typeof(string), "tagName");
            ParameterExpression argValue = Expression.Parameter(typeof(T), "value");

            // Define return value
            ParameterExpression varRootTag = Expression.Parameter(typeof(NbtCompound), "varRootTag");
            LabelTarget returnTarget = Expression.Label(typeof(NbtTag));

            // Create property serializers
            List<Expression> propSerializersList = MakePropertySerializers(typeof(T), argValue, varRootTag);
            Expression serializersExpr;
            if (propSerializersList.Count == 0) {
                serializersExpr = Expression.Empty();
            } else if (propSerializersList.Count == 1) {
                serializersExpr = propSerializersList[0];
            } else {
                serializersExpr = Expression.Block(propSerializersList);
            }

            // Construct the method body
            BlockExpression method = Expression.Block(
                new[] { varRootTag },

                // if( argValue == null )
                Expression.IfThen(
                    Expression.ReferenceEqual(argValue, Expression.Constant(null)),
                    //  throw new ArgumentNullException("value");
                    Expression.Throw(Expression.New(ArgNullConstructor, Expression.Constant("value")))),

                // varRootTag = new NbtCompound(argTagName);
                Expression.Assign(varRootTag, Expression.New(NbtCompoundConstructor, argTagName)),

                // (run the generated serializing code)
                serializersExpr,

                // return varRootTag;
                Expression.Return(returnTarget, varRootTag, typeof(NbtTag)),
                Expression.Label(returnTarget, Expression.Constant(null, typeof(NbtTag))));

            // compile
            return Expression.Lambda<NbtSerialize<T>>(method, argTagName, argValue).Compile();
        }


        // Produces a list of expressions that, together, do the job of
        // producing all necessary NbtTags and adding them to the "varRootTag" compound tag.
        [NotNull]
        static List<Expression> MakePropertySerializers(Type type,
                                                        ParameterExpression argValue,
                                                        ParameterExpression varRootTag) {
            var expressions = new List<Expression>();

            foreach (PropertyInfo property in GetSerializableProperties(type)) {
                // read tag name
                Attribute[] nameAttributes = Attribute.GetCustomAttributes(property, typeof(TagNameAttribute));
                string tagName;
                if (nameAttributes.Length != 0) {
                    tagName = ((TagNameAttribute)nameAttributes[0]).Name;
                } else {
                    tagName = property.Name;
                }

                // read IgnoreOnNull attribute
                Attribute ignoreOnNullAttribute =
                    Attribute.GetCustomAttribute(property, typeof(IgnoreOnNullAttribute));
                bool ignoreOnNull = (ignoreOnNullAttribute != null);

                Type propType = property.PropertyType;

                // simple serialization for primitive types
                if (propType.IsPrimitive) {
                    Expression newTagExpr = MakeTagForPrimitiveType(tagName, argValue, property);
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize reference types that map directly to NBT tag types
                if (propType == typeof(string) ||
                    propType == typeof(byte[]) ||
                    propType == typeof(int[])) {
                    Expression serializeStripPropExpr =
                        SerializePropertyDirectly(argValue, varRootTag, property, ignoreOnNull, tagName);
                    expressions.Add(serializeStripPropExpr);
                    continue;
                }

                // check if this type can handle its own serialization
                if (propType.IsAssignableFrom(typeof(INbtSerializable))) {
                    MethodInfo serializeMethod = propType.GetMethod("Serialize", new[] { typeof(string) });
                    Expression propValue = Expression.MakeMemberAccess(argValue, property);
                    Expression newTagExpr = Expression.Call(propValue, serializeMethod, Expression.Constant(tagName));
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize IList<>
                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>)) {
                    // TODO
                    continue;
                }

                // Skip serializing NbtTag properties
                if (propType.IsAssignableFrom(typeof(NbtTag))) {
                    // TODO
                    continue;
                }

                // Skip serializing NbtFile properties
                if (propType == typeof(NbtFile)) {
                    // TODO
                    continue;
                }

                // TODO: treat property as a compound tag
            }
            return expressions;
        }


        static Expression SerializeIListOfPrimitives(ParameterExpression argValue,
                                                     ParameterExpression varRootTag,
                                                     PropertyInfo property,
                                                     string tagName) {
            // Declare locals
            ParameterExpression varIList = Expression.Parameter(property.PropertyType);
            ParameterExpression varListTag = Expression.Parameter(typeof(NbtList));
            ParameterExpression varIndex = Expression.Parameter(typeof(int));

            // Find getter for this IList
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // Find getter for the item indexer
            InterfaceMapping listImplMap = property.PropertyType.GetInterfaceMap(typeof(IList<>));
            MethodInfo iListItemGetter = typeof(IList<>).GetProperty("Item").GetGetMethod();
            int itemGetterIndex = Array.IndexOf(listImplMap.InterfaceMethods, iListItemGetter);
            MethodInfo itemGetterImpl = listImplMap.TargetMethods[itemGetterIndex];
            Expression getElementExpr = Expression.Call(varIList, itemGetterImpl, varIndex);

            // Find getter for IList<>.Count
            MethodInfo iListCountGetter = typeof(IList<>).GetProperty("Count").GetGetMethod();
            int countGetterIndex = Array.IndexOf(listImplMap.InterfaceMethods, iListCountGetter);
            MethodInfo countGetterImpl = listImplMap.TargetMethods[countGetterIndex];

            // Find the correct NbtTag type for elements
            Type elementType = property.PropertyType.GetGenericArguments()[0];
            Type convertedType;
            if (!PrimitiveConversionMap.TryGetValue(elementType, out convertedType)) {
                convertedType = elementType;
            }
            Type elementTagType = TypeToTagMap[convertedType];

            // Handle element type conversion
            if (elementType == typeof(bool)) {
                // special handling for bool-to-byte
                getElementExpr = Expression.Condition(Expression.IsTrue(getElementExpr),
                                                      Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (elementType != convertedType) {
                // special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                getElementExpr = Expression.Convert(getElementExpr, convertedType);
            }

            // create "new NbtTag(...)" expression
            ConstructorInfo tagConstructor = elementTagType.GetConstructor(new[] { typeof(string), convertedType });
            Expression constructElementTagExpr = Expression.New(tagConstructor,
                                                                Expression.Constant(null, typeof(string)),
                                                                getElementExpr);

            // Arrange tag construction in a loop
            LabelTarget loopBreak = Expression.Label(typeof(void));
            Expression mainLoop = 
                // while (true)
                Expression.Loop(
                    Expression.Block(
                        // if (i==0) break;
                        Expression.IfThen(
                            Expression.Equal(varIndex, Expression.Constant(0)),
                            Expression.Break(loopBreak)),

                        // tag.Add( new NbtTag(...) )
                        Expression.Call(varListTag, NbtListAddMethod, constructElementTagExpr),

                        // i--
                        Expression.Decrement(varIndex)
                        ),
                    loopBreak);

            // Package everything together into a neat block, with locals
            return Expression.Block(
                new[] { varIList, varListTag, varIndex },

                // IList<> varList = argValue.ThisProperty;
                Expression.Assign(varIList, getPropertyExpr),

                // NbtList varTag = new NbtList(tagName, elementTagType);
                Expression.Assign(
                    varListTag,
                    Expression.New(NbtListConstructor, Expression.Constant(tagName), Expression.Constant(elementTagType))),

                // int varIndex = varList.Count - 1;
                Expression.Assign(
                    varIndex,
                    Expression.Subtract(
                        Expression.Call(varIList, countGetterImpl),
                        Expression.Constant(1))),

                // (fill the list tag)
                mainLoop,

                // varRootTag.Add( varListTag );
                Expression.Call(varRootTag, NbtCompoundAddMethod, varListTag));
        }


        // Creates a NbtString tag for given string property.
        // If the property's value is null, we either...
        // 1) Skip creating this tag (if ignoreOnNull is true), or
        // 2) Create a tag with empty-string value
        [NotNull]
        static Expression SerializePropertyDirectly(ParameterExpression argValue,
                                                    ParameterExpression varRootTag,
                                                    PropertyInfo property,
                                                    bool ignoreOnNull,
                                                    string tagName) {
            // locate the getter for this property
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            Type tagType = TypeToTagMap[property.PropertyType];
            ConstructorInfo tagConstructor = tagType.GetConstructor(new[] { typeof(string), property.PropertyType });

            if (ignoreOnNull) {
                // declare a local var, which will hold the property's value
                ParameterExpression varValue = Expression.Parameter(property.PropertyType);

                return Expression.Block(
                    // var varValue = argValue.ThisProperty;
                    new[] { varValue },
                    Expression.Assign(varValue, getPropertyExpr),

                    // if(varValue != null)
                    (Expression)Expression.IfThen(
                        Expression.Not(Expression.ReferenceEqual(varValue, Expression.Constant(null))),

                        // varRootTag.Add( new NbtString(tagName, varValue) );
                        Expression.Call(
                            varRootTag, NbtCompoundAddMethod,
                            Expression.New(tagConstructor, Expression.Constant(tagName), varValue))));
            } else {
                Expression defaultVal = Expression.Constant(GetDefaultValue(property.PropertyType));

                // varRootTag.Add( new NbtString(tagName, varValue.ThisProperty ?? defaultVal) )
                return Expression.Call(
                    varRootTag, NbtCompoundAddMethod,
                    Expression.New(
                        tagConstructor, Expression.Constant(tagName),
                        Expression.Coalesce(getPropertyExpr, defaultVal)));
            }
        }


        // Gets default value for directly-mapped reference types, to substitute a null
        static object GetDefaultValue(Type type) {
            if (type == typeof(string)) {
                return String.Empty;
            } else if (type == typeof(int[])) {
                return new int[0];
            } else if (type == typeof(byte[])) {
                return new byte[0];
            } else {
                throw new ArgumentException();
            }
        }


        // mapping of directly-usable types to their NbtTag subtypes
        static readonly Dictionary<Type, Type> TypeToTagMap = new Dictionary<Type, Type> {
            { typeof(byte), typeof(NbtByte) },
            { typeof(short), typeof(NbtShort) },
            { typeof(int), typeof(NbtInt) },
            { typeof(long), typeof(NbtLong) },
            { typeof(float), typeof(NbtFloat) },
            { typeof(double), typeof(NbtDouble) },
            { typeof(byte[]), typeof(NbtByteArray) },
            { typeof(int[]), typeof(NbtIntArray) },
            { typeof(string), typeof(NbtString) }
        };


        // mapping of convertible value types to directly-usable primitive types
        static readonly Dictionary<Type, Type> PrimitiveConversionMap = new Dictionary<Type, Type> {
            { typeof(bool), typeof(byte) },
            { typeof(sbyte), typeof(byte) },
            { typeof(ushort), typeof(short) },
            { typeof(char), typeof(short) },
            { typeof(uint), typeof(int) },
            { typeof(ulong), typeof(long) },
            { typeof(decimal), typeof(double) }
        };


        // Creates a tag constructor for given primitive-type property
        [NotNull]
        static NewExpression MakeTagForPrimitiveType(string tagName, Expression argValue, PropertyInfo property) {
            // check if conversion is necessary
            Type convertedType;
            if (!PrimitiveConversionMap.TryGetValue(property.PropertyType, out convertedType)) {
                convertedType = property.PropertyType;
            }

            // find the tag constructor
            Type tagType = TypeToTagMap[convertedType];
            ConstructorInfo tagConstructor = tagType.GetConstructor(new[] { typeof(string), convertedType });

            // add a cast, if needed
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
            if (property.PropertyType == typeof(bool)) {
                // special handling for bool-to-byte
                getPropertyExpr = Expression.Condition(Expression.IsTrue(getPropertyExpr),
                                                       Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (property.PropertyType != convertedType) {
                // special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                getPropertyExpr = Expression.Convert(getPropertyExpr, convertedType);
            }

            // create a new instance of the appropriate tag
            return Expression.New(tagConstructor, Expression.Constant(tagName), getPropertyExpr);
        }


        // Get a list of all serializable (readable, non-ignored, instance) properties for given type
        [NotNull]
        static IEnumerable<PropertyInfo> GetSerializableProperties(Type type) {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(p => !Attribute.GetCustomAttributes(p, typeof(NbtIgnoreAttribute)).Any())
                       .Where(p => p.CanRead)
                       .ToArray();
        }
    }
}
