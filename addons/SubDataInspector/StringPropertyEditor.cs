using System;
using Godot;

namespace addons.SubDataInspector
{
    [Tool]
    public class StringPropertyEditor: MemberPropertyEditor<string>
    {
        public override event Action<MemberPropertyEditor> ValueChanged;

        LineEdit textEdit;

        protected override void OnAttach(MemberPropertyEditor parent, IInspectorResourceProvider resources)
        {
            if (parent != null) parent.AddChildEditor(this);
            Label = MemberInfo.Name;
            textEdit = new LineEdit();
            textEdit.Text = GetMemberValue();
            textEdit.Connect("text_changed", this, nameof(OnTextChanged));
            AddChild(textEdit);
        }

        void OnTextChanged(string val)
        {
            ValueChanged?.Invoke(this);
        }

        public override void Detach()
        {
            textEdit.QueueFree();
        }

        public override object GetValue()
        {
            return textEdit.Text;
        }
    }
}