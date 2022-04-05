using System;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
    [Tool]
    public class StringPropertyEditor: MemberPropertyEditor
    {
        public override event Action<MemberPropertyEditor, object> ValueChanged;

        LineEdit input;
        const string InputSignal = "text_entered";

        public LineEdit Input => input;

        public override bool Handles(Type t) => t == typeof(string);

        protected override void SetupContentEditor()
        {
	        input = new LineEdit();
	        input.GrowHorizontal = GrowDirection.End;
            input.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            AddContentEditorInline(input);
            RebuildEditorContent(Container);
        }

        public override void RebuildEditorContent(object container)
        {
	        Container = container;
	        if (input.IsConnected(InputSignal, this, nameof(OnTextChanged))) input.Disconnect(InputSignal, this, nameof(OnTextChanged));
	        input.Text = GetMemberValue() as string;
	        input.Connect(InputSignal, this, nameof(OnTextChanged));
        }

        void OnTextChanged(string val)
        {
	        ValueChanged?.Invoke(this,val);
        }

        public override void TearDown()
        {
	        if (input.IsConnected(InputSignal, this, nameof(OnTextChanged))) input.Disconnect(InputSignal, this, nameof(OnTextChanged));
            input.QueueFree();
            base.TearDown();
        }
    }
}