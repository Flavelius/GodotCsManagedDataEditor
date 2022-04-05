using System;
using Godot;
using Godot.Collections;
using Object = Godot.Object;

namespace ManagedResourceEditor
{
	[Tool]
	public abstract class ManagedDataStorage: Resource, IManagedDataSerializable
	{
		[Export]
		Dictionary<string, string> serializedData = new Dictionary<string, string>();
		[Export]
		Dictionary<string, Object> serializedReferences = new Dictionary<string, Object>();

		readonly System.Collections.Generic.Dictionary<string, object> cache = new System.Collections.Generic.Dictionary<string, object>();

		Guid IManagedDataSerializable.SetResourceReference<T>(T item)
		{
			foreach (var kv in serializedReferences)
			{
				if (kv.Value == item) return Guid.Parse(kv.Key);
			}
			var k = Guid.NewGuid();
			serializedReferences[k.ToString()] = item;
			return k;
		}

		bool IManagedDataSerializable.GetResourceReference<T>(Guid guid, out T result)
		{
			if (serializedReferences.TryGetValue(guid.ToString(), out var refVal))
			{
				if (!(refVal is T t))
				{
					GD.PushError($"Serialized Reference is not of requested type: {guid} -> {typeof(T)}, actual type: {refVal.GetType().Name}");
					result = null;
					return false;
				}
				result = t;
				return true;
			}
			result = null;
			return false;
		}

		protected void Store<T>(string fieldName, T value)
		{
			if (!Engine.EditorHint) cache[fieldName] = value;
			else
			{
				var data = ReflectionDataHelper.SerializeMember(value, this);
				if (string.IsNullOrEmpty(data)) return;
				serializedData[fieldName] = data;
				if (!string.IsNullOrEmpty(ResourcePath))
				{
					ResourceSaver.Save(ResourcePath, this);
					ResourceLoader.Load(ResourcePath).TakeOverPath(ResourcePath); //force refresh cache ?
				}
			}
		}

		protected bool TryRestore<T>(string fieldName, out T item)
		{
			if (Engine.EditorHint)
			{
				if (serializedData.TryGetValue(fieldName, out var eData)) return ReflectionDataHelper.TryDeserializeMember(eData, out item, this);
				item = default;
				return false;
			}
			if (cache.TryGetValue(fieldName, out var result))
			{
				if (result is T t)
				{
					item = t;
					return true;
				}
				item = default;
				return false;
			}
			if (serializedData.TryGetValue(fieldName, out var data) && ReflectionDataHelper.TryDeserializeMember(data, out item, this))
			{
				cache[fieldName] = item;
				return true;
			}
			item = default;
			return false;
		}
	
	}
}
