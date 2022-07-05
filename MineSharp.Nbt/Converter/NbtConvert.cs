using DeepSlate.Nbt.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeepSlate.Nbt
{
	/// <summary>
	/// Provides helpers for converting to Types to NbtDocument or vice-versa
	/// Works like Newtonsoft.Json
	/// </summary>
	public static class NbtConvert
	{
		#region serialize

		public static NbtDocument SerializeObject<T>(T? obj)
			=> SerializeObject(typeof(T), obj);

		public static NbtDocument SerializeObject(Type type, object? obj)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (obj == null) throw new ArgumentNullException("object");

			return (NbtDocument)SerializeAsCompound(type, obj);
		}

		

		internal static NbtCompound SerializeAsCompound(Type type, object obj)
		{
			NbtMemberSerialization memSer = type.GetCustomAttribute<NbtDocumentAttribute>()?.Serialization ?? NbtMemberSerialization.OptOut;

			NbtCompound compound = new();

			foreach (var prop in type.GetProperties().Where(p => ShouldInclude(memSer, p)))
			{
				object? value = prop.GetValue(obj);
				if(value == null) throw new ArgumentNullException($"Cant serialize '{GetName(prop)}' ({prop}) because it is null");
				if (prop.PropertyType.IsAssignableTo(typeof(NbtTag)))
				{
					NbtTag tag = (NbtTag)value;
					compound[GetName(prop)] = tag;
				} else
				{
					compound[GetName(prop)] = SerializeAsCompound(prop.PropertyType, value);
				}
			}

			foreach (var prop in type.GetFields().Where(f => ShouldInclude(memSer, f)))
			{
				object? value = prop.GetValue(obj);
				if (value == null) throw new ArgumentNullException($"Cant serialize '{GetName(prop)}' ({prop}) because it is null");
				if (prop.FieldType.IsAssignableTo(typeof(NbtTag)))
				{
					NbtTag tag = (NbtTag)value;
					compound[GetName(prop)] = tag;
				}
				else
				{
					compound[GetName(prop)] = SerializeAsCompound(prop.FieldType, value);
				}
			}

			return compound;
		}

		#endregion

		#region deserialize

		public static T DeserializeObject<T>(NbtDocument nbt)
			where T : new()
			=> (T)DeserializeObject(typeof(T), nbt);

		public static object DeserializeObject(Type type, NbtDocument nbt)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (nbt == null) throw new ArgumentNullException("object");

			object obj = InitializeType(type);

			NbtDocumentAttribute nbtprops = type.GetCustomAttribute<NbtDocumentAttribute>() ?? new();

			return DeserializeCompound(nbtprops, type, obj, nbt);
		}

		internal static object DeserializeCompound(Type type, NbtCompound compound)
			=> DeserializeCompound(type, InitializeType(type), compound);
		internal static object DeserializeCompound(Type type, object obj, NbtCompound compound)
			=> DeserializeCompound(type.GetCustomAttribute<NbtDocumentAttribute>() ?? new(), type, obj, compound);
		internal static object DeserializeCompound(NbtDocumentAttribute nbtprops, Type type, object obj, NbtCompound compound)
		{
			Console.WriteLine($"DeserializeCompound: {type}, {obj}, comp: {compound}");

			if(type == typeof(NbtTag) || type.BaseType == typeof(NbtTag) || type.BaseType?.BaseType == typeof(NbtTag))
			{
				Console.WriteLine($"!!! Basetype of {type} was NbtTag, returning obj");
				return obj;
			}

			NbtMemberSerialization memSer = nbtprops.Serialization;
			
			foreach(var kvp in compound)
			{
				if(kvp.Value.TagType == NbtTagType.Compound)
				{
					Console.WriteLine($"{kvp.Value.Path} is a compound");
					PropertyInfo? compoundProp = TryGetNbtProperty(nbtprops, type, kvp.Key);
					if (compoundProp != null)
					{
						object? compObj = DeserializeCompound(nbtprops, compoundProp.PropertyType, InitializeType(compoundProp.PropertyType), (NbtCompound)kvp.Value);
						compoundProp.SetValue(obj, Convert.ChangeType(compObj, compoundProp.PropertyType));
					}

					FieldInfo? compoundField = TryGetNbtField(nbtprops, type, kvp.Key);
					if (compoundField != null)
					{
						object? compObj = DeserializeCompound(nbtprops, compoundField.FieldType, InitializeType(compoundField.FieldType), (NbtCompound)kvp.Value);
						compoundField.SetValue(obj, Convert.ChangeType(compObj, compoundField.FieldType));
					}
					continue;
				}

				PropertyInfo? prop = TryGetNbtProperty(nbtprops, type, kvp.Key);
				if (prop != null)
				{
					prop.SetValue(obj, Convert.ChangeType(kvp.Value, prop.PropertyType));
					continue;
				}

				FieldInfo? field = TryGetNbtField(nbtprops, type, kvp.Key);
				if(field != null)
				{
					field.SetValue(obj, Convert.ChangeType(kvp.Value, field.FieldType));
					continue;
				}
			}

			/*
			foreach (var prop in type.GetProperties().Where(p => ShouldInclude(memSer, p)))
			{
				if(compound.Contains(GetName(prop)))
				{
					Console.WriteLine($"Compound contains '{GetName(prop)}' (prop='{prop.Name}') ; type = {prop.PropertyType}");
					NbtTag? tag = compound[GetName(prop)];
					try
					{
						prop.SetValue(obj, Convert.ChangeType(tag, prop.PropertyType));
					}
					catch(Exception ex)
					{
						Console.WriteLine($"{prop.PropertyType} wasnt assignable, treating it as a compound");
						Console.WriteLine($"{prop.PropertyType}: ex: {ex}");
						var comp = (NbtCompound?)compound[GetName(prop)];
						if (comp == null) throw new Exception($"component null");
						object? value = prop.GetValue(obj);
						if (value == null) value = InitializeType(prop.PropertyType);
						prop.SetValue(obj, DeserializeCompound(prop.PropertyType, value, comp));
					}
				}
			}

			foreach (var field in type.GetFields().Where(p => ShouldInclude(memSer, p)))
			{
				if (compound.Contains(GetName(field)))
				{
					Console.WriteLine($"Compound contains '{GetName(field)}' (field='{field.Name}') ; type = {field.FieldType}");
					try
					{
						field.SetValue(obj, Convert.ChangeType(compound[GetName(field)], field.FieldType));
					}
					catch(Exception ex)
					{
						Console.WriteLine($"{field.FieldType} wasnt assignable, treating it as a compound");
						Console.WriteLine($"{field.FieldType}: ex: {ex}");
						var comp = (NbtCompound?)compound[GetName(field)];
						if (comp == null) throw new Exception($"component null");
						object? value = field.GetValue(obj);
						if (value == null) value = InitializeType(field.FieldType);
						field.SetValue(obj, DeserializeCompound(field.FieldType, value, comp));
					}
				}
			}*/

			return obj;
		}

		#endregion

		#region utils

		internal static bool ShouldInclude(NbtMemberSerialization ser, MemberInfo member)
		{
			if (ser == NbtMemberSerialization.OptIn) return member.GetCustomAttribute<NbtPropertyAttribute>() != null;
			if (ser == NbtMemberSerialization.OptOut) return member.GetCustomAttribute<NbtIgnoreAttribute>() == null;
			return false;
		}

		internal static string GetName(MemberInfo member)
		{
			return member.GetCustomAttribute<NbtPropertyAttribute>()?.TagName ?? member.Name;
		}

		internal static object InitializeType(Type type)
		{
			object? obj = Activator.CreateInstance(type);
			if (obj == null) throw new Exception($"Couldn't construct {type}! Make sure you have a parameterless constructor");
			return obj;
		}

		internal static PropertyInfo? TryGetNbtProperty(NbtDocumentAttribute nbtprops, Type t, string name)
		{
			return t.GetProperties().FirstOrDefault(p => ShouldInclude(nbtprops.Serialization, p) &&
				string.Equals(name, GetName(p), nbtprops.StrictCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
				null);
		}

		internal static FieldInfo? TryGetNbtField(NbtDocumentAttribute nbtprops, Type t, string name)
		{
			return t.GetFields().FirstOrDefault(p => ShouldInclude(nbtprops.Serialization, p) &&
				string.Equals(name, GetName(p), nbtprops.StrictCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
				null);
		}

		//internal static string ToCamelCase(string s) => (char.ToLowerInvariant(s[0]) + s.Substring(1)).Replace("_", string.Empty);
		//internal static string ToTitleCase(string s) => (char.ToUpperInvariant(s[0]) + s.Substring(1)).Replace("_", string.Empty);

		#endregion
	}
}
