using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace addons.SubDataInspector
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

        static Dictionary<Type, Type> memberEditors;

        static void EnsureMemberEditorsInitialized()
        {
            if (memberEditors != null) return;
            var types = Assembly.GetAssembly(typeof(MemberPropertyEditor)).GetTypes().Where(t => t.IsClass && !t.IsAbstract);
            memberEditors = new Dictionary<Type, Type>();
            var baseType = typeof(MemberPropertyEditor<>);
            foreach (var t in types)
            {
                var parentType = FindGenericParent(t, baseType);
                if (parentType == null) continue;
                var genericArgument = parentType.GetGenericArguments()[0];
                if (!memberEditors.ContainsKey(genericArgument)) memberEditors.Add(genericArgument, t);
            }
            Type FindGenericParent(Type testType, Type searchType)
            {
                if (testType == null || testType == typeof(object))
                {
                    return null;
                }
                if (testType.IsGenericType && testType.GetGenericTypeDefinition() == searchType)
                {
                    return testType;
                }
                return FindGenericParent(testType.BaseType, searchType);
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
            return true;
        }

        public static IEnumerable<Type> GetGenericMemberEditorTypes()
        {
            EnsureMemberEditorsInitialized();
            return memberEditors.Values;
        }

        public static Type GetGenericMemberEditorType(Type type)
        {
            EnsureMemberEditorsInitialized();
            return memberEditors.TryGetValue(type, out var result) ? result : null;
        }

        public static Type GetContentType(MemberInfo m)
        {
            if (m is FieldInfo fi) return fi.FieldType;
            if (m is PropertyInfo pi) return pi.PropertyType;
            return null;
        }

        public static object GetMemberContent(MemberInfo m, object parent)
        {
            if (m is FieldInfo f) return f.GetValue(parent);
            if (m is PropertyInfo p) return p.GetValue(parent);
            return null;
        }

        public static bool IsGodotType(Type t) => t.Assembly == Assembly.GetAssembly(typeof(Node));

        public static bool IsGodotReference(MemberInfo m)
        {
            if (m is FieldInfo f) return typeof(Reference).IsAssignableFrom(f.FieldType);
            if (m is PropertyInfo p) return typeof(Reference).IsAssignableFrom(p.PropertyType);
            return false;
        }

        public static bool IsStruct(Type t) => t.IsValueType && !t.IsEnum && !t.IsPrimitive;
        
        public static bool IsStruct(MemberInfo m)
        {
            if (m is FieldInfo f) return IsStruct(f.FieldType);
            if (m is PropertyInfo p) return IsStruct(p.PropertyType);
            return false;
        }

        public static bool IsCustomStruct(Type t) => IsStruct(t) && !IsGodotType(t);

        public static bool IsSupportedRootContent(MemberInfo m)
        {
            if (m.GetCustomAttribute<CustomExportAttribute>() == null) return false;
            Type t;
            if (m is FieldInfo f) t = f.FieldType;
            else if (m is PropertyInfo p) t = p.PropertyType;
            else return false;
            return IsCustomStruct(t);
        }

        public static bool IsSupportedNestedContent(MemberInfo m)
        {
            if (m.GetCustomAttribute<CustomExportAttribute>() == null && m.GetCustomAttribute<ExportAttribute>() == null) return false;
            Type t;
            if (m is FieldInfo f) t = f.FieldType;
            else if (m is PropertyInfo p) t = p.PropertyType;
            else return false;
            if (IsCustomStruct(t)) return true;
            if (GetGenericMemberEditorType(t) != null) return true;
            return false;
        }

        public static void SetContentValue(MemberInfo m, object toTarget, object value)
        {
            if (m is FieldInfo f) f.SetValue(toTarget, value);
            if (m is PropertyInfo p) p.SetValue(toTarget, value);
        }

        class StoredReferenceResolver : IReferenceResolver
        {
            readonly IManagedDataSerializable dataStorage;

            public StoredReferenceResolver(IManagedDataSerializable dataStorage)
            {
                this.dataStorage = dataStorage;
            }

            public object ResolveReference(object context, string reference)
            {
                return dataStorage.SerializedData.Contains(reference) ? dataStorage.SerializedData[reference] : null;
            }

            public string GetReference(object context, object value)
            {
                foreach (DictionaryEntry entry in dataStorage.SerializedData)
                {
                    if (entry.Value == value) return entry.Key as string;
                }
                return string.Empty;
            }

            public bool IsReferenced(object context, object value)
            {
                foreach (DictionaryEntry entry in dataStorage.SerializedData)
                {
                    if (entry.Value == value) return true;
                }
                return false;
            }

            public void AddReference(object context, string reference, object value)
            {
                dataStorage.SerializedData[reference] = value;
            }
        }

        class CustomSerializationContractResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var defaultMembers = base.GetSerializableMembers(objectType);
                defaultMembers.AddRange(GetMissingMembers(objectType, defaultMembers));
                IEnumerable<MemberInfo> GetMissingMembers(Type type, List<MemberInfo> alreadyAdded)
                {
                    return type.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(o => o.GetCustomAttribute<CustomExportAttribute>() != null
                        && !alreadyAdded.Contains(o));
                }
                return defaultMembers;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var p = base.CreateProperty(member, MemberSerialization.OptOut);
                if (member.GetCustomAttribute<CustomExportAttribute>() == null)
                {
                    p.Ignored = false;
                    p.HasMemberAttribute = true;
                }
                p.Writable = CanWriteMember(member);
                p.Readable = CanReadMember(member);
                p.IsReference = IsGodotReference(member);
                return p;
            }
        }

        public static void RestoreCustomExports<T>(T target) where T : IManagedDataSerializable
        {
            foreach (var member in ResolveHandledMembers(target.GetType(), IsSupportedRootContent))
            {
                var key = member.Name;
                if (!target.SerializedData.Contains(key))
                {
                    GD.PushWarning($"{key}'s data not found in SerializedData storage (on {target}), member will be uninitialized (undefined)");
                    continue;
                }
                var settings = new JsonSerializerSettings
                {
                    ReferenceResolverProvider = () => new StoredReferenceResolver(target),
                    Formatting = Formatting.None,
                    ContractResolver = new CustomSerializationContractResolver(),
                };
                var data = target.SerializedData[key] as string ?? string.Empty;
                if (member is FieldInfo f) f.SetValue(target, JsonConvert.DeserializeObject(data, f.FieldType, settings));
                else if (member is PropertyInfo p) p.SetValue(target, JsonConvert.DeserializeObject(data, p.PropertyType, settings));
            }
        }

        public static void StoreCustomExports<T>(T target) where T : IManagedDataSerializable
        {
            target.SerializedData.Clear();
            foreach (var member in ResolveHandledMembers(target.GetType(), IsSupportedRootContent))
            {
                var key = member.Name;
                var settings = new JsonSerializerSettings
                {
                    ReferenceResolverProvider = () => new StoredReferenceResolver(target),
                    Formatting = Formatting.None,
                    ContractResolver = new CustomSerializationContractResolver()
                };
                if (member is FieldInfo f)
                {
                    var value = f.GetValue(target);
                    var serializedValue = JsonConvert.SerializeObject(value, settings);
                    target.SerializedData[key] = serializedValue;
                }
                else if (member is PropertyInfo p)
                {
                    var value = p.GetValue(target);
                    var serializedValue = JsonConvert.SerializeObject(value, settings);
                    target.SerializedData[key] = serializedValue;
                }
            }
        }
    }
}