using System;
using Godot;

namespace ManagedResourceEditor
{
	[Serializable]
	public struct ResourceReference<T> where T : Resource
	{
		[Export] string guid;
		[NonSerialized]
		T value;

		public Guid Guid => Guid.TryParse(guid, out var result) ? result : Guid.Empty;
		public T Value => value;

		public ResourceReference(Guid guid)
		{
			this.guid = guid.ToString();
			value = null;
		}

		public ResourceReference(Guid guid, T value)
		{
			this.guid = guid.ToString();
			this.value = value;
		}

		public static implicit operator T(ResourceReference<T> resRef) => resRef.value;
	}
}
