using System;
using System.Collections.Generic;
using Godot;

namespace addons.SubDataInspector
{
    [Tool]
    public class StructContainerPropertyEditor : MemberPropertyEditor
    {
        MarginContainer marginContainer;
        VBoxContainer propertyContainer;

        readonly List<MemberPropertyEditor> children = new List<MemberPropertyEditor>();

        public override event Action<MemberPropertyEditor> ValueChanged;

        public override bool Handles(Type t)
        {
            return ReflectionDataHelper.IsCustomStruct(t);
        }

        protected override void OnAttach(MemberPropertyEditor parent, IInspectorResourceProvider resources)
        {
            if (parent != null) parent.AddChildEditor(this);
            Label = "[" + MemberInfo.Name + "]";
            marginContainer = new MarginContainer();
            marginContainer.AddConstantOverride("margin_left", 15);
            marginContainer.AddConstantOverride("margin_top", 4);
            marginContainer.AddConstantOverride("margin_bottom", 4);
            AddChild(marginContainer);
            SetBottomEditor(marginContainer);
            propertyContainer = new VBoxContainer();
            propertyContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            marginContainer.AddChild(propertyContainer);
            // SetBottomEditor(propertyContainer);
            foreach (var member in ReflectionDataHelper.ResolveHandledMembers(ContentType, ReflectionDataHelper.IsSupportedNestedContent))
            {
                var t = ReflectionDataHelper.GetContentType(member);
                if (t == null) continue;
                var propEditor = resources.FindPropertyEditor(t);
                if (propEditor == null)
                {
                    GD.PushError($"Property Editor not found for {t}");
                    continue;
                }
                propEditor = propEditor.Duplicate() as MemberPropertyEditor;
                if (ReflectionDataHelper.IsStruct(member))
                {
                    propEditor.Attach(this, resources, ReflectionDataHelper.GetMemberContent(member, Target), member);
                }
                else
                {
                    propEditor.Attach(this, resources, Target, member);
                }
                propEditor.ValueChanged += OnPropEditorValueChanged;
                children.Add(propEditor);
            }
        }

        public override void AddChildEditor(MemberPropertyEditor child)
        {
            propertyContainer.AddChild(child);
        }

        void OnFoldoutToggled(bool toggleState)
        {
            propertyContainer.Visible = toggleState;
        }

        public override void Detach()
        {
            foreach (var child in children)
            {
                child.ValueChanged -= OnPropEditorValueChanged;
            }
        }

        void OnPropEditorValueChanged(MemberPropertyEditor sender)
        {
            ValueChanged?.Invoke(this);
        }

        public override object GetValue()
        {
            for (int i = 0; i < children.Count; i++)
            {
                ReflectionDataHelper.SetContentValue(children[i].MemberInfo, Target, children[i].GetValue());
            }
            return Target;
        }
    }
}