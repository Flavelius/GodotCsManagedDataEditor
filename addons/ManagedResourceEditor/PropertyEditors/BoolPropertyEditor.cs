using System;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class BoolPropertyEditor: MemberPropertyEditor
	{
		CheckBox input;
		const string ChangeSignal = "toggled";
	    
		public override event Action<MemberPropertyEditor, object> ValueChanged;

		public CheckBox Input => input;

		public override bool Handles(Type t) => t == typeof(bool);

		protected override void SetupContentEditor()
		{
			input = new CheckBox();
			input.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			AddContentEditorInline(input);
			RebuildEditorContent(Container);
		}

		public override void RebuildEditorContent(object container)
		{
			Container = container;
			if (input.IsConnected(ChangeSignal, this, nameof(OnValueChanged))) input.Disconnect(ChangeSignal, this, nameof(OnValueChanged));
			input.Pressed = Convert.ToBoolean(GetMemberValue());
			input.Connect(ChangeSignal, this, nameof(OnValueChanged));
		}

		void OnValueChanged(bool newValue)
		{
			ValueChanged?.Invoke(this, newValue);
		}

		public override void TearDown()
		{
			if (input.IsConnected(ChangeSignal, this, nameof(OnValueChanged))) input.Disconnect(ChangeSignal, this, nameof(OnValueChanged));
			input.QueueFree();
			base.TearDown();
		}
	}
}
