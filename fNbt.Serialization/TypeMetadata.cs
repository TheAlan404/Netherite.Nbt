using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fNbt.Serialization {
    internal class TypeMetadata {
        public TypeMetadata(Type contractType) {
            Properties =
                contractType.GetProperties()
                            .Where(p => !Attribute.GetCustomAttributes((MemberInfo)p, typeof(NbtIgnoreAttribute)).Any())
                            .ToArray();
            PropertyTagNames = new Dictionary<PropertyInfo, string>();

            foreach (PropertyInfo property in Properties) {
                // read tag name
                Attribute[] nameAttributes = Attribute.GetCustomAttributes(property, typeof(TagNameAttribute));
                string tagName;
                if (nameAttributes.Length != 0) {
                    tagName = ((TagNameAttribute)nameAttributes[0]).Name;
                } else {
                    tagName = property.Name;
                }
                PropertyTagNames.Add(property, tagName);

                // read IgnoreOnNull attribute
                var nullPolicyAttr =
                    (NullPolicyAttribute)Attribute.GetCustomAttribute(property, typeof(NullPolicyAttribute));
                if (nullPolicyAttr != null) {
                    if (nullPolicyAttr.SelfPolicy != NullPolicy.Default) {
                        if (NullPolicies == null) {
                            NullPolicies = new Dictionary<PropertyInfo, NullPolicy>();
                        }
                        NullPolicies.Add(property, nullPolicyAttr.SelfPolicy);
                    }
                    if (nullPolicyAttr.ElementPolicy != NullPolicy.Default) {
                        if (ElementNullPolicies == null) {
                            ElementNullPolicies = new Dictionary<PropertyInfo, NullPolicy>();
                        }
                        ElementNullPolicies.Add(property, nullPolicyAttr.ElementPolicy);
                    }
                }
            }
        }

        public readonly PropertyInfo[] Properties;
        public readonly Dictionary<PropertyInfo, string> PropertyTagNames;
        public readonly Dictionary<PropertyInfo, NullPolicy> NullPolicies;
        public readonly Dictionary<PropertyInfo, NullPolicy> ElementNullPolicies;
    }
}