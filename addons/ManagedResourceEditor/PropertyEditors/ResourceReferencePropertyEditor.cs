using System;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class ResourceReferencePropertyEditor : MemberPropertyEditor
	{
		EditorResourcePicker input;
		const string PickerSignal = "resource_changed";

		public override event Action<MemberPropertyEditor, object> ValueChanged;

		public EditorResourcePicker Input => input;

		public override bool Handles(Type t)
		{
			return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ResourceReference<>);
		}

		protected override void SetupContentEditor()
		{
			input = new EditorResourcePicker();
			input.BaseType = nameof(Resource);
			input.GrowHorizontal = GrowDirection.End;
			input.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			AddContentEditorInline(input);
			RebuildEditorContent(Container);
		}

		public override void RebuildEditorContent(object container)
		{
			Container = container;
			if (input.IsConnected(PickerSignal, this, nameof(OnValueChanged))) input.Disconnect(PickerSignal, this, nameof(OnValueChanged));
			var resRef = GetMemberValue();
			var propField = resRef.GetType().GetProperty("Guid");
			var guid = (Guid)propField.GetValue(resRef);
			if (guid != Guid.Empty) input.EditedResource = Resources.Target.GetResourceReference(guid, out Resource result) ? result : null;
			else input.EditedResource = null;
			input.Connect(PickerSignal, this, nameof(OnValueChanged));
		}

		void OnValueChanged(Resource value)
		{
			var genericArg = ContentType.GetGenericArguments()[0];
			var genericType = typeof(ResourceReference<>).MakeGenericType(genericArg);

			var guid = value == null ? Guid.Empty : Resources.Target.SetResourceReference(value);
			var result = Activator.CreateInstance(genericType, guid);

			ValueChanged?.Invoke(this, result);
		}

		public override void TearDown()
		{
			if (input.IsConnected(PickerSignal, this, nameof(OnValueChanged))) input.Disconnect(PickerSignal, this, nameof(OnValueChanged));
			input.QueueFree();
			base.TearDown();
		}
	}
}
