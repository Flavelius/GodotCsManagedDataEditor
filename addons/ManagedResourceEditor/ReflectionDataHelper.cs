using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ManagedResourceEditor
{
	public static class ReflectionDataHelper
	{
		public static IEnumerable<MemberInfo> ResolveHandledMembers(Type t, Predicate<MemberInfo> predicate)
		{
			var baseMembers = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property);
			foreach (var memberInfo in baseMembers)
			{
				if (CanReadMember(memberInfo) && CanWriteMember(memberInfo) && predicate(memberInfo)) yield return memberInfo;
			}
		}

		static List<MemberPropertyEditor> memberEditors;

		static void EnsureMemberEditorsInitialized()
		{
			if (memberEditors != null) return;
			var types = Assembly.GetAssembly(typeof(MemberPropertyEditor)).GetTypes().Where(t => t.IsClass && !t.IsAbstract);
			memberEditors = new List<MemberPropertyEditor>();
			var baseType = typeof(MemberPropertyEditor);
			foreach (var t in types)
			{
				if (baseType.IsAssignableFrom(t))
				{
					if (memberEditors.Find(m => m.GetType() == t) == null)
					{
						memberEditors.Add((MemberPropertyEditor)Activator.CreateInstance(t));
					}
				}
			}
		}

		static bool CanReadMember(MemberInfo member)
		{
			if (member is PropertyInfo property)
			{
				return property.CanRead;
			}
			return true;
		}

		static bool CanWriteMember(MemberInfo member)
		{
			if (member is PropertyInfo property)
			{
				return property.CanWrite;
			}
			if (member is FieldInfo field)
			{
				return !field.IsInitOnly;
			}
			return false;
		}

		public static IEnumerable<MemberPropertyEditor> GetMemberPropertyEditors()
		{
			EnsureMemberEditorsInitialized();
			return memberEditors;
		}

		public static Type GetContentType(MemberInfo m)
		{
			if (m is FieldInfo fi) return fi.FieldType;
			if (m is PropertyInfo pi) return pi.PropertyType;
			return null;
		}

		public static object GetMemberContent(MemberInfo m, object obj)
		{
			if (m is FieldInfo f) return f.GetValue(obj);
			if (m is PropertyInfo p) return p.GetValue(obj);
			return null;
		}

		public static void SetMemberContent(MemberInfo m, object toTarget, object value)
		{
			if (m is FieldInfo f) f.SetValue(toTarget, value);
			if (m is PropertyInfo p) p.SetValue(toTarget, value);
		}

		public static bool IsCustomStruct(Type t) => t.IsValueType && !t.IsEnum && !t.IsPrimitive && t.Assembly != Assembly.GetAssembly(typeof(Node));

		public static bool IsManagedClass(Type t) => t.IsClass && !t.IsAbstract && t.Assembly != Assembly.GetAssembly(typeof(Node));

		static bool HasExportAttribute(MemberInfo memberInfo, bool includeGodots)
		{
			if (includeGodots) return memberInfo.IsDefined(typeof(ExportAttribute)) || memberInfo.IsDefined(typeof(ExportCustomAttribute));
			return memberInfo.IsDefined(typeof(ExportCustomAttribute));
		}

		static bool IsSupportedSerializableContent(Type tType)
		{
			if (IsCustomStruct(tType) || IsManagedClass(tType)) return true;
			if (tType.IsArray)
			{
				var innerType = tType.GetElementType();
				return IsCustomStruct(innerType) || IsManagedClass(innerType);
			}
			if (tType.IsGenericType && tType.GetGenericTypeDefinition() == typeof(List<>))
			{
				var innerType = tType.GetGenericArguments()[0];
				return IsCustomStruct(innerType) || IsManagedClass(innerType);
			}
			return false;
		}

		static bool IsEditableContent(MemberInfo m)
		{
			Type t;
			if (m is FieldInfo f) t = f.FieldType;
			else if (m is PropertyInfo p) t = p.PropertyType;
			else return false;
			if (GetMemberPropertyEditor(t) != null) return true;
			return false;
			MemberPropertyEditor GetMemberPropertyEditor(Type type)
			{
				EnsureMemberEditorsInitialized();
				for (int i = 0; i < memberEditors.Count; i++)
				{
					if (memberEditors[i].Handles(type)) return memberEditors[i];
				}
				return null;
			}
		}

		public static bool IsEditableRootContent(MemberInfo m)
		{
			if (!HasExportAttribute(m, false)) return false;
			return IsEditableContent(m);
		}

		public static bool IsEditableNestedContent(MemberInfo m)
		{
			if (!HasExportAttribute(m, true)) return false;
			return IsEditableContent(m);
		}

		class CustomSerializationContractResolver : DefaultContractResolver
		{
			public static readonly CustomSerializationContractResolver Instance = new CustomSerializationContractResolver();
			public class ResourceRefConverter : JsonConverter
			{
				readonly IManagedDataSerializable refStorage;
				
				public ResourceRefConverter(IManagedDataSerializable refStorage)
				{
					this.refStorage = refStorage;
				}

				public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
				{
					var guidProp = value.GetType().GetProperty("Guid");
					writer.WriteValue(((Guid)guidProp.GetValue(value)).ToString());
				}

				public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
				{
					var guidValue = reader.Value as string ?? string.Empty;
					var guid = string.IsNullOrEmpty(guidValue) ? Guid.Empty : Guid.TryParse(guidValue, out var val) ? val : Guid.Empty;
					var resType = objectType.GetGenericArguments()[0];
					var constructionType = typeof(ResourceReference<>).MakeGenericType(resType);
					if (guid != Guid.Empty && refStorage.GetResourceReference<Resource>(guid, out var obj) && obj.GetType() == resType)
					{
						return Activator.CreateInstance(constructionType, guid, obj);
					}
					return Activator.CreateInstance(constructionType, guid);
				}

				public override bool CanConvert(Type objectType)
				{
					return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ResourceReference<>);
				}
			}

			protected override List<MemberInfo> GetSerializableMembers(Type objectType)
			{
				var result = objectType.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
					.Where(m => m is FieldInfo || m is PropertyInfo)
					.Where(m => IsSupportedSerializableContent(GetContentType(m)) || HasExportAttribute(m, true)).ToList();
				return result;
			}

			protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
			{
				var prop =  base.CreateProperty(member, memberSerialization);
				var isSerializable = HasExportAttribute(member, true);
				prop.ShouldSerialize = o => isSerializable;
				prop.ShouldDeserialize = prop.ShouldSerialize;
				prop.Readable = CanReadMember(member);
				prop.Writable = CanWriteMember(member);
				return prop;
			}
		}

		public static string SerializeMember<TItem>(TItem item, IManagedDataSerializable owner)
		{
			if (!IsSupportedSerializableContent(typeof(TItem)))
			{
				GD.PushError($"Type: {typeof(TItem).Name} is not supported to be serialized");
				return null;
			}
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				ContractResolver = CustomSerializationContractResolver.Instance,
				Converters = new JsonConverter[] {new CustomSerializationContractResolver.ResourceRefConverter(owner)}
			};
			return JsonConvert.SerializeObject(item, settings);
		}

		public static bool TryDeserializeMember<TItem>(string data, out TItem item, IManagedDataSerializable owner)
		{
			if (string.IsNullOrEmpty(data))
			{
				item = default;
				return false;
			}
			if (!IsSupportedSerializableContent(typeof(TItem)))
			{
				GD.PushError($"Type: {typeof(TItem).Name} is not supported to be deserialized");
				item = default;
				return false;
			}
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				ContractResolver = CustomSerializationContractResolver.Instance,
				Converters = new JsonConverter[] {new CustomSerializationContractResolver.ResourceRefConverter(owner)}
			};
			try
			{
				item = JsonConvert.DeserializeObject<TItem>(data, settings);
				return true;
			}
			catch (Exception ex)
			{
				GD.PushError(ex.Message);
				item = default;
				return false;
			}
		}
	}
}
