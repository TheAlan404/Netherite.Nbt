using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    public delegate NbtTag NbtSerialize<T>(string tagName, T value);


    public static class NbtSerializationGen {
        // NbtCompound.Add(NbtTag)
        static readonly MethodInfo NbtCompoundAddMethod =
            typeof(NbtCompound).GetMethod("Add", new[] { typeof(NbtTag) });

        // NbtList.Add(NbtTag)
        static readonly MethodInfo NbtListAddMethod =
            typeof(NbtList).GetMethod("Add", new[] { typeof(NbtTag) });

        // new ArgumentNullException(string)
        static readonly ConstructorInfo ArgumentNullExceptionCtor =
            typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) });

        // new NullReferenceException(string)
        static readonly ConstructorInfo NullReferenceExceptionCtor =
            typeof(NullReferenceException).GetConstructor(new[] { typeof(string) });

        // new NbtCompound(string)
        static readonly ConstructorInfo NbtCompoundCtor =
            typeof(NbtCompound).GetConstructor(new[] { typeof(string) });

        // new NbtList(string,NbtTagType)
        static readonly ConstructorInfo NbtListCtor =
            typeof(NbtList).GetConstructor(new[] { typeof(string), typeof(NbtTagType) });

        // (string)null -- used to select appropriate constructor/method overloads when creating unnamed tags
        static readonly Expression NullStringExpr = Expression.Constant(null, typeof(string));

        const string NullElementMessage = "Null elements not allowed inside a list.";


        #region Caching

        static readonly object CacheLock = new object();
        static readonly Dictionary<Type, Delegate> GlobalCache = new Dictionary<Type, Delegate>();


        [NotNull]
        public static NbtSerialize<T> GetSerializerForType<T>() {
            lock (CacheLock) {
                Delegate result;
                if (GlobalCache.TryGetValue(typeof(T), out result)) {
                    return (NbtSerialize<T>)result;
                }
                NbtSerialize<T> newResult = CreateSerializerForType<T>();
                GlobalCache.Add(typeof(T), newResult);
                return newResult;
            }
        }


        public static Delegate GetSerializerForType(Type t) {
            lock (CacheLock) {
                Delegate result;
                if (!GlobalCache.TryGetValue(t, out result)) {
                    result = CreateSerializerForType(t);
                    GlobalCache.Add(t, result);
                }
                return result;
            }
        }

        #endregion


        static readonly MethodInfo CreateSerializerForTypeInfo =
            typeof(NbtSerializationGen)
                .GetMethod("CreateSerializerForType",
                           BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static,
                           null, CallingConventions.Standard,
                           Type.EmptyTypes, null);


        [NotNull]
        static Delegate CreateSerializerForType(Type t) {
            // create a variant of CreateSerializerForType<T> for given type
            MethodInfo genericMethodToCall =
                CreateSerializerForTypeInfo.MakeGenericMethod(new[] { t });
            return (Delegate)genericMethodToCall.Invoke(null, new object[0]);
        }


        // Generates specialized methods for serializing objects of given Type to NBT
        [NotNull]
        static NbtSerialize<T> CreateSerializerForType<T>() {
            // Define function arguments
            ParameterExpression argTagName = Expression.Parameter(typeof(string), "tagName");
            ParameterExpression argValue = Expression.Parameter(typeof(T), "value");

            // Define return value
            ParameterExpression varRootTag = Expression.Parameter(typeof(NbtCompound), "rootTag");
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
                    Expression.Throw(Expression.New(ArgumentNullExceptionCtor, Expression.Constant("value")))),

                // varRootTag = new NbtCompound(argTagName);
                Expression.Assign(varRootTag, Expression.New(NbtCompoundCtor, argTagName)),

                // (run the generated serializing code)
                serializersExpr,

                // return varRootTag;
                Expression.Return(returnTarget, varRootTag, typeof(NbtTag)),
                Expression.Label(returnTarget, Expression.Constant(null, typeof(NbtTag))));

            // compile
            Expression<NbtSerialize<T>> methodLambda = Expression.Lambda<NbtSerialize<T>>(method, argTagName, argValue);
#if DEBUG
            // When in debug mode, print the expression tree to stdout.
            // TODO: Disable before release
            PropertyInfo propInfo =
                typeof(Expression)
                    .GetProperty("DebugView",
                                 BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);

            var debugView = (string)propInfo.GetValue(methodLambda, null);
            Console.WriteLine(debugView);
#endif

            return methodLambda.Compile();
        }


        // Produces a list of expressions that, together, do the job of
        // producing all necessary NbtTags and adding them to the "varRootTag" compound tag.
        [NotNull]
        static List<Expression> MakePropertySerializers(Type type,
                                                        ParameterExpression argValue,
                                                        ParameterExpression varRootTag) {
            var expressions = new List<Expression>();

            foreach (PropertyInfo property in GetSerializableProperties(type)) {
                Type propType = property.PropertyType;

                // Check if property is self-referential
                if (propType == type) {
                    throw new NotSupportedException(
                        "Self-referential properties are not supported. " +
                        "Add NbtIgnore attribute to ignore property " + property.Name);
                }

                // read tag name
                Attribute nameAttribute = Attribute.GetCustomAttribute(property, typeof(TagNameAttribute));
                string tagName;
                if (nameAttribute != null) {
                    tagName = ((TagNameAttribute)nameAttribute).Name;
                } else {
                    tagName = property.Name;
                }

                // read NullPolicy attribute
                var ignoreOnNullAttribute =
                    (NullPolicyAttribute)Attribute.GetCustomAttribute(property, typeof(NullPolicyAttribute));
                var selfPolicy = NullPolicy.Default;
                var elementPolicy = NullPolicy.Default;
                if (ignoreOnNullAttribute != null) {
                    selfPolicy = ignoreOnNullAttribute.SelfPolicy;
                    elementPolicy = ignoreOnNullAttribute.ElementPolicy;
                }

                // simple serialization for primitive types
                if (propType.IsPrimitive || propType.IsEnum) {
                    Expression newTagExpr = MakeTagForPrimitiveType(tagName, argValue, property);
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize reference types that map directly to NBT tag types
                if (SerializationUtil.IsDirectlyMappedType(propType)) {
                    Expression serializeStripPropExpr =
                        SerializePropertyDirectly(argValue, varRootTag, property, tagName, selfPolicy);
                    expressions.Add(serializeStripPropExpr);
                    continue;
                }

                // check if this type can handle its own serialization
                if (typeof(INbtSerializable).IsAssignableFrom(propType)) {
                    MethodInfo serializeMethod = propType.GetMethod("Serialize", new[] { typeof(string) });
                    Expression propValue = Expression.MakeMemberAccess(argValue, property);
                    Expression newTagExpr = Expression.Call(propValue, serializeMethod, Expression.Constant(tagName));
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize something that implements IList<>
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(propType, typeof(IList<>));
                if (iListImpl != null) {
                    Type elementType = iListImpl.GetGenericArguments()[0];
                    Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
                    Expression newExpr = SerializeIList(getPropertyExpr, elementType, property.PropertyType,
                                                        tagName, selfPolicy, elementPolicy,
                                                        MakePropertyNullMessage(property),
                                                        expr => Expression.Call(varRootTag, NbtCompoundAddMethod, expr));
                    expressions.Add(newExpr);
                    continue;
                }

                // Skip serializing NbtTag properties
                if (typeof(NbtTag).IsAssignableFrom(propType)) {
                    // Add tag directly to the list
                    Expression newExpr =
                        MakeNbtTagHandler(argValue, varRootTag,
                                          property, tagName, selfPolicy,
                                          expr => expr);
                    expressions.Add(newExpr);
                    continue;
                }

                // Skip serializing NbtFile properties
                if (propType == typeof(NbtFile)) {
                    // Add NbtFile's root tag directly to the list
                    PropertyInfo rootTagProp = typeof(NbtFile).GetProperty("RootTag");
                    Expression newExpr =
                        MakeNbtTagHandler(argValue, varRootTag,
                                          property, tagName, selfPolicy,
                                          expr => Expression.MakeMemberAccess(expr, rootTagProp));
                    expressions.Add(newExpr);
                    continue;
                }

                // Compound expressions
                Delegate compoundSerializer = GetSerializerForType(propType);
                Expression newCompoundExpr =
                    MakeNbtTagHandler(argValue, varRootTag,
                                      property, tagName, selfPolicy,
                                      expr => Expression.Invoke(Expression.Constant(compoundSerializer),
                                                                NullStringExpr, expr));
                expressions.Add(newCompoundExpr);
            }
            return expressions;
        }


        [NotNull]
        static Expression MakeNbtTagHandler(ParameterExpression argValue, ParameterExpression varRootTag,
                                            PropertyInfo property, string tagName, NullPolicy selfPolicy,
                                            Func<ParameterExpression, Expression> conversionFunc) {
            // declare a local var, which will hold the property's value
            ParameterExpression varValue = Expression.Parameter(property.PropertyType);

            // Primary path, adds the root tag of the NbtFile
            Expression makeTagExpr = Expression.Call(varRootTag, NbtCompoundAddMethod, conversionFunc(varValue));

            // Fallback path, in case value is null and NullPolicy is InsertDefaults
            Expression defaultVal = Expression.New(NbtCompoundCtor, Expression.Constant(tagName));
            Expression defaultValExpr = Expression.Call(varRootTag, NbtCompoundAddMethod, defaultVal);

            // Getter for the property value
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
            return MakeNullHandler(varValue,
                                   getPropertyExpr, selfPolicy, makeTagExpr,
                                   defaultValExpr, MakePropertyNullMessage(property));
        }



        static Expression SerializeIList(Expression getIListExpr, Type elementType, Type listType,
                                         string tagName, NullPolicy selfPolicy, NullPolicy elementPolicy,
                                         string nullMessage,
                                         Func<Expression, Expression> processTagExpr) {
            // Declare locals
            ParameterExpression varIList = Expression.Parameter(listType, "iList");
            ParameterExpression varListTag = Expression.Parameter(typeof(NbtList), "listTag");
            ParameterExpression varLength = Expression.Parameter(typeof(int), "length");
            ParameterExpression varIndex = Expression.Parameter(typeof(int), "i");

            // Find getter for this IList
            MethodInfo countGetterImpl, itemGetterImpl;

            if (listType.IsArray) {
                // Although Array claims to implement IList<>, there is no way to retrieve
                // the interface implementation: it's handled in an unusual way by the runtime.
                // So we have to resort to getting Length/GetValue instead of Count/Item
                countGetterImpl = listType.GetProperty("Length").GetGetMethod();
                itemGetterImpl = listType.GetMethod("GetValue", new[] { typeof(int) });

            } else {
                // For non-array IList<> types, grab this.Count getter (which maps to get_Count())
                countGetterImpl = SerializationUtil.GetGenericInterfaceMethodImpl(
                    listType, typeof(ICollection<>), "get_Count", new Type[0]);
                // ...and the getter for indexer this[int], which maps to get_Item(int)
                itemGetterImpl = SerializationUtil.GetGenericInterfaceMethodImpl(
                    listType, typeof(IList<>), "get_Item", new[] { typeof(int) });
            }
            Expression getElementExpr = Expression.Call(varIList, itemGetterImpl, varIndex);
            Expression getCountExpr = Expression.Call(varIList, countGetterImpl);

            Expression handleOneElementExpr;
            Type elementTagType;

            if (elementType.IsPrimitive) {
                //=== Serializing arrays/lists of primitive types ===

                // Find the correct NbtTag type for elements
                Type convertedType = GetConvertedType(elementType);
                elementTagType = GetNbtTagSubtype(elementType);

                // Handle element type conversion
                if (elementType == typeof(bool)) {
                    // bools returned from Array.GetValue() need to be cast
                    if (getElementExpr.Type != typeof(bool)) {
                        getElementExpr = Expression.Convert(getElementExpr, typeof(bool));
                    }
                    // special handling for bool-to-byte
                    getElementExpr = Expression.Condition(getElementExpr,
                                                          Expression.Constant((byte)1), Expression.Constant((byte)0));
                } else if (elementType != convertedType || getElementExpr.Type != elementType) {
                    // Special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                    // Also, anything returned by Array.GetValue() needs to be cast
                    getElementExpr = Expression.Convert(getElementExpr, convertedType);
                }

                // create "new NbtTag(null, )" expression
                ConstructorInfo tagCtor = elementTagType.GetConstructor(new[] { typeof(string), convertedType });
                Expression constructElementTagExpr = Expression.New(tagCtor, NullStringExpr, getElementExpr);

                // tag.Add( new NbtTag(...) );
                handleOneElementExpr = Expression.Call(varListTag, NbtListAddMethod, constructElementTagExpr);

            } else if (SerializationUtil.IsDirectlyMappedType(elementType)) {
                //=== Serializing arrays/lists of directly-mapped reference types (byte[], int[], string) ===

                // declare a local var, which will hold the property's value
                ParameterExpression varValue = Expression.Parameter(elementType, "value");

                // Find the appropriate NbtTag type and constructor
                elementTagType = SerializationUtil.TypeToTagMap[elementType];
                ConstructorInfo elementTagCtor =
                    elementTagType.GetConstructor(new[] { typeof(string), elementType });

                // Primary path, in case element value is not null:
                // listTag.Add(new NbtTag(null, <getElementExpr>));
                getElementExpr = Expression.Convert(getElementExpr, elementType);
                Expression addElementExpr =
                    Expression.Call(varListTag, NbtListAddMethod,
                                    Expression.New(elementTagCtor, NullStringExpr, getElementExpr));

                // Fallback path, in case element value is null and elementPolicy is InsertDefaults:
                // Add a default-value tag to the list: listTag.Add(new NbtTag(null, <default>))
                Expression defaultElementExpr =
                    Expression.Call(varListTag, NbtListAddMethod,
                                    Expression.New(elementTagCtor, NullStringExpr,
                                                   Expression.Constant(SerializationUtil.GetDefaultValue(elementType))));

                // generate the appropriate enclosing expressions, depending on NullPolicy
                handleOneElementExpr = MakeNullHandler(varValue,
                                                       getElementExpr, elementPolicy,
                                                       addElementExpr, defaultElementExpr,
                                                       NullElementMessage);

            } else {
                //=== Serializing arrays/lists of everything else ===

                // Check if this is an IList-of-ILists
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(elementType, typeof(IList<>));
                bool elementIsIList = (iListImpl != null);
                getElementExpr = Expression.Convert(getElementExpr, elementType);

                if (elementIsIList) {
                    elementTagType = typeof(NbtList);
                    Type subElementType = iListImpl.GetGenericArguments()[0];
                    handleOneElementExpr =
                        SerializeIList(getElementExpr, subElementType, elementType,
                                       null, elementPolicy, elementPolicy, NullElementMessage,
                                       subListExpr => Expression.Call(varListTag, NbtListAddMethod, subListExpr));

                } else {
                    elementTagType = typeof(NbtCompound);

                    // Get NbtSerialize<T> method for elementType
                    Delegate compoundSerializer = GetSerializerForType(elementType);
                    Expression makeElementTagExpr =
                        Expression.Invoke(Expression.Constant(compoundSerializer), NullStringExpr, getElementExpr);

                    // declare a local var, which will hold the element's value
                    ParameterExpression varElementValue = Expression.Parameter(elementType, "elementValue");

                    // Primary path, adds the newly-made Compound tag to our list
                    Expression addSerializedCompoundExpr =
                        Expression.Call(varListTag, NbtListAddMethod, makeElementTagExpr);

                    // Fallback path, in case element's value is null and NullPolicy is InsertDefaults
                    Expression addEmptyCompoundExpr =
                        Expression.Call(varListTag, NbtListAddMethod, Expression.New(NbtCompoundCtor, NullStringExpr));

                    // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
                    handleOneElementExpr = MakeNullHandler(varElementValue,
                                                           getElementExpr, elementPolicy,
                                                           addSerializedCompoundExpr, addEmptyCompoundExpr,
                                                           NullElementMessage);
                }
            }

            // Arrange tag construction in a loop
            LabelTarget loopBreak = Expression.Label(typeof(void));
            Expression mainLoop = 
                // while (true)
                Expression.Loop(
                    Expression.Block(
                        // if (i >= length) break;
                        Expression.IfThen(
                            Expression.GreaterThanOrEqual(varIndex, varLength),
                            Expression.Break(loopBreak)),

                        // <process and add one element to the list>
                        handleOneElementExpr,

                        // ++i;
                        Expression.PreIncrementAssign(varIndex)),
                    loopBreak);

            // new NbtList(tagName, NbtTagType.*)
            Expression makeListTagExpr =
                Expression.New(NbtListCtor,
                               Expression.Constant(tagName, typeof(string)),
                               Expression.Constant(GetNbtTagType(elementType)));

            // Fallback path, in case value our IList null and NullPolicy is InsertDefaults:
            // Add an empty list to root.
            Expression defaultValExpr = processTagExpr(makeListTagExpr);

            // Primary path, in case our IList is not null:
            // Package the list-building loop into a neat block, with locals
            Expression makeTagExpr = Expression.Block(
                new[] { varListTag, varIndex, varLength },

                // NbtList listTag = new NbtList(tagName, NbtTagType.*);
                Expression.Assign(varListTag, makeListTagExpr),

                // int length = iList.Count;
                Expression.Assign(varLength, getCountExpr),

                // int i=0;
                Expression.Assign(varIndex, Expression.Constant(0)),

                // (fill the list tag)
                mainLoop,

                // rootTag.Add( listTag );
                processTagExpr(varListTag));

            // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
            return MakeNullHandler(varIList, getIListExpr, selfPolicy,
                                   makeTagExpr, defaultValExpr, nullMessage);
        }


        static Type GetConvertedType(Type rawType) {
            if (rawType.IsEnum) {
                rawType = Enum.GetUnderlyingType(rawType);
            }

            Type convertedType;
            if (!SerializationUtil.PrimitiveConversionMap.TryGetValue(rawType, out convertedType)) {
                convertedType = rawType;
            }

            return convertedType;
        }


        static Type GetNbtTagSubtype(Type rawValueType) {
            Type convertedType = GetConvertedType(rawValueType);
            return SerializationUtil.TypeToTagMap[convertedType];
        }


        static NbtTagType GetNbtTagType(Type rawValueType) {
            Type convertedType = GetConvertedType(rawValueType);

            Type directTagType;
            if (SerializationUtil.TypeToTagMap.TryGetValue(convertedType, out directTagType)) {
                return SerializationUtil.TypeToTagTypeEnum[directTagType];
            }

            Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(rawValueType, typeof(IList<>));
            if (iListImpl != null) {
                return NbtTagType.List;
            } else {
                return NbtTagType.Compound;
            }
        }


        // Creates a NbtString tag for given string property.
        // If the property's value is null, we either...
        // 1) Skip creating this tag (if ignoreOnNull is true), or
        // 2) Create a tag with empty-string value
        [NotNull]
        static Expression SerializePropertyDirectly(ParameterExpression argValue, ParameterExpression varRootTag,
                                                    PropertyInfo property,
                                                    string tagName,
                                                    NullPolicy selfPolicy) {
            // Find the appropriate NbtTag constructor
            Type tagType = SerializationUtil.TypeToTagMap[property.PropertyType];
            ConstructorInfo tagCtor = tagType.GetConstructor(new[] { typeof(string), property.PropertyType });

            // declare a local var, which will hold the property's value
            ParameterExpression varValue = Expression.Parameter(property.PropertyType);

            Expression defaultVal = Expression.Constant(SerializationUtil.GetDefaultValue(property.PropertyType));
            Expression defaultValTagCtor = Expression.New(tagCtor, Expression.Constant(tagName, typeof(string)), defaultVal);
            Expression defaultValExpr = Expression.Call(varRootTag, NbtCompoundAddMethod, defaultValTagCtor);

            // varRootTag.Add( new NbtString(tagName, varValue) );
            Expression makeTagExpr = Expression.Call(
                varRootTag, NbtCompoundAddMethod,
                Expression.New(tagCtor, Expression.Constant(tagName, typeof(string)), varValue));

            // Getter for the property value
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // generate the appropriate enclosing expressions, depending on NullPolicy
            return MakeNullHandler(varValue, getPropertyExpr, selfPolicy, makeTagExpr,
                                   defaultValExpr, MakePropertyNullMessage(property));
        }


        static string MakePropertyNullMessage(PropertyInfo prop) {
            return "Property " + prop.DeclaringType.Name + "." + prop.Name + " cannot be null.";
        }


        static Expression MakeNullHandler(ParameterExpression varValue, Expression getPropertyExpr, NullPolicy policy,
                                          Expression nonNullExpr, Expression defaultValExpr, string exceptionMessage) {
            // locate the getter for this property
            Expression ifExpr;

            if (policy == NullPolicy.Error) {
                // throw new NullReferenceException(...)
                ifExpr = Expression.IfThenElse(
                    // if (value==null) throw new NullReferenceException(exceptionMessage)
                    Expression.ReferenceEqual(varValue, Expression.Constant(null)),
                    Expression.Throw(
                        Expression.New(NullReferenceExceptionCtor, Expression.Constant(exceptionMessage))),
                    // else <nonNullExpr>
                    nonNullExpr);

            } else if (policy == NullPolicy.Ignore) {
                ifExpr = Expression.IfThen(
                    // if (value!=null) <nonNullExpr>
                    Expression.Not(Expression.ReferenceEqual(varValue, Expression.Constant(null))),
                    nonNullExpr);

            } else if (policy == NullPolicy.InsertDefault) {
                ifExpr = Expression.IfThenElse(
                    // if (value==null) throw new NullReferenceException(...)
                    Expression.ReferenceEqual(varValue, Expression.Constant(null)),
                    defaultValExpr,
                    // else <nonNullExpr>
                    nonNullExpr);

            } else {
                throw new ArgumentOutOfRangeException("Unrecognized value for NullPolicy: " + policy);
            }

            return Expression.Block(
                // var varValue = value.ThisProperty;
                new[] { varValue },
                Expression.Assign(varValue, getPropertyExpr),
                // (check if value is null, and do something)
                ifExpr);
        }


        // Creates a tag constructor for given primitive-type property
        [NotNull]
        static NewExpression MakeTagForPrimitiveType(string tagName, Expression argValue, PropertyInfo property) {
            // Find a mapping from PropertyType to closest NBT equivalent
            Type convertedType = GetConvertedType(property.PropertyType);

            // find the tag constructor
            Type tagType = SerializationUtil.TypeToTagMap[convertedType];
            ConstructorInfo tagCtor = tagType.GetConstructor(new[] { typeof(string), convertedType });

            // add a cast, if needed
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
            if (property.PropertyType == typeof(bool)) {
                // special handling for bool-to-byte
                getPropertyExpr = Expression.Condition(getPropertyExpr,
                                                       Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (property.PropertyType != convertedType) {
                // special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                getPropertyExpr = Expression.Convert(getPropertyExpr, convertedType);
            }

            // create a new instance of the appropriate tag
            return Expression.New(tagCtor, Expression.Constant(tagName, typeof(string)), getPropertyExpr);
        }


        // Get a list of all serializable (readable, non-ignored, instance) properties for given type
        [NotNull]
        static IEnumerable<PropertyInfo> GetSerializableProperties(Type type) {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(p => Attribute.GetCustomAttribute(p, typeof(NbtIgnoreAttribute)) == null)
                       .Where(p => p.CanRead)
                       .ToArray();
        }
    }
}
