using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    internal class SerializeCodeEmitter : CodeEmitter {
        const string NullElementMessage = "Null elements not allowed inside a list.";

        // NbtCompound.Add(NbtTag)
        static readonly MethodInfo NbtCompoundAddMethod =
            typeof(NbtCompound).GetMethod("Add", new[] { typeof(NbtTag) });

        // NbtList.Add(NbtTag)
        static readonly MethodInfo NbtListAddMethod =
            typeof(NbtList).GetMethod("Add", new[] { typeof(NbtTag) });

        // new NbtCompound(string)
        static readonly ConstructorInfo NbtCompoundCtor =
            typeof(NbtCompound).GetConstructor(new[] { typeof(string) });

        // new NbtList(string, NbtTagType)
        static readonly ConstructorInfo NbtListCtor =
            typeof(NbtList).GetConstructor(new[] { typeof(string), typeof(NbtTagType) });


        readonly ParameterExpression varRootTag;
        readonly ParameterExpression argTagName;
        readonly ParameterExpression argValue;
        readonly NbtCompiler.CallResolver callResolver;


        public override ParameterExpression ReturnValue {
            get { return varRootTag; }
        }


        public SerializeCodeEmitter([NotNull] ParameterExpression argTagName, [NotNull] ParameterExpression argValue,
                                    [NotNull] NbtCompiler.CallResolver callResolver) {
            if (argTagName == null) throw new ArgumentNullException("argTagName");
            if (argValue == null) throw new ArgumentNullException("argValue");
            if (callResolver == null) throw new ArgumentNullException("callResolver");
            varRootTag = Expression.Parameter(typeof(NbtCompound), "rootTag");
            this.argTagName = argTagName;
            this.argValue = argValue;
            this.callResolver = callResolver;
        }


        public override Expression GetPreamble() {
            // varRootTag = new NbtCompound(argTagName);
            return Expression.Assign(varRootTag, Expression.New(NbtCompoundCtor, argTagName));
        }


        public override Expression HandlePrimitiveOrEnum(string tagName, PropertyInfo property) {
            // Find a mapping from PropertyType to closest NBT equivalent
            Type convertedType = GetConvertedType(property.PropertyType);

            // property getter
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // create a new instance of the appropriate tag
            Expression newTagExpr = MakeNbtTagCtor(convertedType,
                                                   Expression.Constant(tagName, typeof(string)),
                                                   getPropertyExpr);

            return Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr);
        }


        // Generates an expression that creates an NbtTag for given property of a directly-mappable types.
        // Directly-mappable types are: primitives, enums, byte[], int[], and string.
        public override Expression HandleDirectlyMappedType(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            // declare a local var, which will hold the property's value
            ParameterExpression varValue = Expression.Parameter(property.PropertyType);

            // Fallback path, in case value is null and NullPolicy is InsertDefaults
            Expression defaultVal = Expression.Constant(SerializationUtil.GetDefaultValue(property.PropertyType));
            // varRootTag.Add( new NbtTag(tagName, <defaultVal>) );
            Expression defaultValExpr =
                Expression.Call(varRootTag,
                                NbtCompoundAddMethod,
                                MakeNbtTagCtor(property.PropertyType,
                                               Expression.Constant(tagName, typeof(string)),
                                               defaultVal));

            // varRootTag.Add( new NbtTag(tagName, <varValue>) );
            Expression makeTagExpr =
                Expression.Call(varRootTag,
                                NbtCompoundAddMethod,
                                MakeNbtTagCtor(property.PropertyType,
                                               Expression.Constant(tagName, typeof(string)),
                                               varValue));

            // Getter for the property value
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // generate the appropriate enclosing expressions, depending on NullPolicy
            return NbtCompiler.MakeNullHandler(varValue, getPropertyExpr, selfPolicy,
                                               makeTagExpr, defaultValExpr, MakePropertyNullMessage(property));
        }


        public override Expression HandleINbtSerializable(string tagName, PropertyInfo property) {
            // TODO: handle NullPolicy
            MethodInfo serializeMethod = property.PropertyType.GetMethod("Serialize", new[] { typeof(string) });
            Expression propValue = Expression.MakeMemberAccess(argValue, property);
            Expression newTagExpr = Expression.Call(propValue, serializeMethod, Expression.Constant(tagName));
            return Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr);
        }


        public override Expression HandleIList(string tagName, PropertyInfo property, Type iListImpl,
                                               NullPolicy selfPolicy, NullPolicy elementPolicy) {
            Type elementType = iListImpl.GetGenericArguments()[0];
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
            return SerializeIList(getPropertyExpr, elementType,
                                  property.PropertyType, tagName,
                                  selfPolicy, elementPolicy, MakePropertyNullMessage(property),
                                  expr => Expression.Call(varRootTag, NbtCompoundAddMethod, expr));
        }


        public override Expression HandleNbtTag(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            // Add tag directly to the list
            return MakeNbtTagHandler(property, tagName, selfPolicy, expr => expr);
        }


        public override Expression HandleNbtFile(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            // Add NbtFile's root tag directly to the list
            PropertyInfo rootTagProp = typeof(NbtFile).GetProperty("RootTag");
            return MakeNbtTagHandler(property,
                                     tagName,
                                     selfPolicy,
                                     expr => Expression.MakeMemberAccess(expr, rootTagProp));
        }


        public override Expression HandleCompoundObject(string tagName, PropertyInfo property, NullPolicy selfPolicy) {
            return MakeNbtTagHandler(property, tagName, selfPolicy,
                                     expr => callResolver.MakeCall(property.PropertyType, tagName, expr));
        }


        public override Expression HandleStringIDictionary(string tagName, PropertyInfo property, Type iDictImpl,
                                                           NullPolicy selfPolicy, NullPolicy elementPolicy) {
            Type elementType = iDictImpl.GetGenericArguments()[1];
            // find type of KeyValuePair<,> that the enumerator will return
            Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(iDictImpl.GetGenericArguments());

            // locate IDictionary.GetEnumerable()
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(kvpType);
            Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(kvpType);
            MethodInfo getEnumeratorImpl =
                SerializationUtil.GetGenericInterfaceMethodImpl(
                    property.PropertyType, enumerableType, "GetEnumerator", Type.EmptyTypes);
            MethodInfo moveNextMethod = enumeratorType.GetMethod("MoveNext");

            ParameterExpression varIDict = Expression.Parameter(property.PropertyType, "iDict");
            ParameterExpression varCompTag = Expression.Parameter(typeof(NbtCompound), "compTag");
            ParameterExpression varEnumerator = Expression.Parameter(enumeratorType, "enumerator");

            // property getter
            Expression getIDictExpr = Expression.MakeMemberAccess(argValue, property);
            LabelTarget loopBreak = Expression.Label(typeof(void));

            Expression forEachElementExpr = null;

            Expression makeTagExpr =
                Expression.Block(
                    new[] { varIDict, varCompTag, varEnumerator },
                    Expression.Assign(varEnumerator, Expression.Call(varIDict, getEnumeratorImpl)),
                    Expression.Loop(
                        Expression.IfThenElse(Expression.Call(varEnumerator, moveNextMethod),
                                              Expression.Block(forEachElementExpr),
                                              Expression.Break(loopBreak)),
                        loopBreak)
                    );

            // default value (in case of NullPolicy.InsertDefault): new NbtCompound(tagName)
            Expression defaultValExpr = Expression.New(NbtCompoundCtor, Expression.Constant(tagName));


            string nullMessage = string.Format("Null elements are not allowed inside {0}.{1}",
                                               property.DeclaringType.Name, property.Name);
            return NbtCompiler.MakeNullHandler(varIDict, getIDictExpr, selfPolicy,
                                               makeTagExpr, defaultValExpr, nullMessage);
        }


        // Creates expression that handles a property directly convertible to an NbtTag object.
        // Value of <property> should be convertible by <conversionFunc> to an expression that evaluates to an NbtTag object.
        // At run time, if the value is not null, the NbtTag is added to <varRootTag> (which is an NbtCompound).
        // Otherwise if value is null,
        // a) If NullPolicy=Error, a NullReferenceException is thrown
        // b) If NullPolicy=Ignore, nothing happens
        // c) If NullPolicy=InsertDefault, an empty NbtCompound tag is added to <varRootTag>
        [NotNull]
        Expression MakeNbtTagHandler([NotNull] PropertyInfo property, [NotNull] string tagName, NullPolicy selfPolicy,
                                     [NotNull] Func<ParameterExpression, Expression> conversionFunc) {
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
            return NbtCompiler.MakeNullHandler(varValue, getPropertyExpr, selfPolicy,
                                               makeTagExpr, defaultValExpr,
                                               MakePropertyNullMessage(property));
        }


        // Creates serialization code for properties with value type that is an array or an IList<T>.
        // For byte[] and int[], use SerializePropertyDirectly(...) instead -- it's more efficient.
        [NotNull]
        Expression SerializeIList([NotNull] Expression getIListExpr, [NotNull] Type elementType,
                                  [NotNull] Type listType, [CanBeNull] string tagName,
                                  NullPolicy selfPolicy, NullPolicy elementPolicy,
                                  [NotNull] string nullMessage,
                                  [NotNull] Func<Expression, Expression> processTagExpr) {
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

            if (elementType.IsPrimitive || elementType.IsEnum) {
                //=== Serializing arrays/lists of primitives and enums ===
                // tag.Add( new NbtTag(null, <getElementExpr>) );
                handleOneElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType, NbtCompiler.NullStringExpr, getElementExpr));
            } else if (SerializationUtil.IsDirectlyMappedType(elementType)) {
                //=== Serializing arrays/lists of directly-mapped reference types (byte[], int[], string) ===

                // declare a local var, which will hold the property's value
                ParameterExpression varValue = Expression.Parameter(elementType, "value");

                // Add conversion/casting logic, if needed
                getElementExpr = MakeConversionToDirectType(elementType, getElementExpr);

                // Primary path, in case element value is not null:
                // listTag.Add(new NbtTag(null, <getElementExpr>));
                Expression addElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType, NbtCompiler.NullStringExpr, getElementExpr));

                // Fallback path, in case element value is null and elementPolicy is InsertDefaults:
                // Add a default-value tag to the list: listTag.Add(new NbtTag(null, <default>))
                Expression defaultElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType,
                                                   NbtCompiler.NullStringExpr,
                                                   Expression.Constant(SerializationUtil.GetDefaultValue(elementType))));

                // generate the appropriate enclosing expressions, depending on NullPolicy
                handleOneElementExpr = NbtCompiler.MakeNullHandler(varValue,
                                                                   getElementExpr,
                                                                   elementPolicy,
                                                                   addElementExpr,
                                                                   defaultElementExpr,
                                                                   NullElementMessage);
            } else {
                //=== Serializing arrays/lists of everything else ===

                // Check if this is an IList-of-ILists
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(elementType, typeof(IList<>));
                bool elementIsIList = (iListImpl != null);
                getElementExpr = Expression.Convert(getElementExpr, elementType);

                if (elementIsIList) {
                    Type subElementType = iListImpl.GetGenericArguments()[0];
                    handleOneElementExpr =
                        SerializeIList(getElementExpr, subElementType,
                                       elementType, null, elementPolicy, elementPolicy, NullElementMessage,
                                       subListExpr => Expression.Call(varListTag, NbtListAddMethod, subListExpr));
                } else {
                    // Get NbtSerialize<T> method for elementType
                    Expression makeElementTagExpr = callResolver.MakeCall(elementType, null, getElementExpr);

                    // declare a local var, which will hold the element's value
                    ParameterExpression varElementValue = Expression.Parameter(elementType, "elementValue");

                    // Primary path, adds the newly-made Compound tag to our list
                    Expression addSerializedCompoundExpr =
                        Expression.Call(varListTag, NbtListAddMethod, makeElementTagExpr);

                    // Fallback path, in case element's value is null and NullPolicy is InsertDefaults
                    Expression addEmptyCompoundExpr =
                        Expression.Call(varListTag,
                                        NbtListAddMethod,
                                        Expression.New(NbtCompoundCtor, NbtCompiler.NullStringExpr));

                    // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
                    handleOneElementExpr =
                        NbtCompiler.MakeNullHandler(varElementValue, getElementExpr, elementPolicy,
                                                    addSerializedCompoundExpr, addEmptyCompoundExpr, NullElementMessage);
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
            return NbtCompiler.MakeNullHandler(varIList, getIListExpr, selfPolicy,
                                               makeTagExpr, defaultValExpr, nullMessage);
        }


        // Creates an NbtTag constructor for given tag name and value expressions.
        // valueType must be a primitive or an enum. Casting and conversion are added as needed.
        [NotNull]
        static NewExpression MakeNbtTagCtor([NotNull] Type valueType, [NotNull] Expression tagNameExpr,
                                            [NotNull] Expression tagValueExpr) {
            if (!SerializationUtil.IsDirectlyMappedType(valueType)) {
                throw new ArgumentException("Given type must be primitive, enum, string, byte[], or int[]", "valueType");
            }

            // Add conversion logic, if needed
            tagValueExpr = MakeConversionToDirectType(valueType, tagValueExpr);

            // Find an NbtTag subtype for given type. Given type must be primitive or enum.
            // For example: byte -> NbtByte; int[] -> NbtIntArray, etc 
            Type elementTagType = SerializationUtil.TypeToTagMap[tagValueExpr.Type];

            // Find appropriate constructor
            ConstructorInfo tagCtor = elementTagType.GetConstructor(new[] { typeof(string), tagValueExpr.Type });
            // ReSharper disable once AssignNullToNotNullAttribute -- constructor will never be null
            return Expression.New(tagCtor, tagNameExpr, tagValueExpr);
        }


        // Perform any necessary conversion to go from tagValueExpr to closest directly-mapped value type
        [NotNull]
        static Expression MakeConversionToDirectType([NotNull] Type valueType, [NotNull] Expression tagValueExpr) {
            // Add casting/conversion, if needed
            Type convertedType = GetConvertedType(valueType);

            // boxed values returned by Array.GetValue() needs to be cast to bool first
            if (valueType != tagValueExpr.Type) {
                tagValueExpr = Expression.Convert(tagValueExpr, valueType);
            }

            if (valueType == typeof(bool)) {
                // Special handling for booleans: (<tagValueExpr> ? (byte)1 : (byte)0)
                return Expression.Condition(tagValueExpr,
                                            Expression.Constant((byte)1),
                                            Expression.Constant((byte)0));
            } else if (valueType != convertedType) {
                // special handling (casting) for enums and sbyte/ushort/char/uint/ulong/decimal
                return Expression.Convert(tagValueExpr, convertedType);
            } else {
                return tagValueExpr;
            }
        }


        // Finds an NBT primitive that is closest to the given type.
        // If given type must is not a primitive or enum, then the original type is returned.
        // For example: bool -> byte; char -> short, etc
        [NotNull]
        static Type GetConvertedType([NotNull] Type rawType) {
            if (rawType == null) throw new ArgumentNullException("rawType");
            if (rawType.IsEnum) {
                rawType = Enum.GetUnderlyingType(rawType);
            }

            Type convertedType;
            if (!SerializationUtil.PrimitiveConversionMap.TryGetValue(rawType, out convertedType)) {
                convertedType = rawType;
            }

            return convertedType;
        }


        // Finds a NbtTagType for given value type.
        // NbtTagType.Compound is returned for any value type that is not a primitive/enum/array/IList<T>
        // For example: int -> NbtTagType.Int; List<string> -> NbtTagType.List; etc
        static NbtTagType GetNbtTagType([NotNull] Type rawValueType) {
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


        // Generate a message for a NullReferenceException to be thrown if given property's value is null
        [NotNull]
        static string MakePropertyNullMessage([NotNull] PropertyInfo property) {
            return string.Format("Property {0}.{1} cannot be null.",
                                 // ReSharper disable once PossibleNullReferenceException
                                 property.DeclaringType.Name,
                                 property.Name);
        }
    }
}
