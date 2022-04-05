using System;
using Godot;

namespace ManagedResourceEditor
{
	/// <summary>
	/// implementing type must be marked as [Tool]
	/// </summary>
	public interface IManagedDataSerializable
	{
		Guid SetResourceReference<T>(T item) where T : Resource;
		bool GetResourceReference<T>(Guid guid, out T result) where T : Resource;
	}
}
