using System;

namespace ManagedResourceEditor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public abstract class InspectorModifierAttribute: Attribute
	{
		public abstract void ApplyModifications(MemberPropertyEditor editor);
	}
}
