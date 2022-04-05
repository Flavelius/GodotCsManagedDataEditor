using System;
using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

namespace ManagedResourceEditor
{
    [Tool]
    public class ManagedResourceEditor : EditorInspectorPlugin, IManagedResourceEditor
    {
	    readonly List<MemberPropertyEditor> activeEditors = new List<MemberPropertyEditor>();

	    readonly EditorInterface editorInterface;
        
        public IManagedDataSerializable Target { get; private set; }
        public Dictionary<int, bool> InspectorFoldStates { get; } = new Dictionary<int, bool>();
        
        ManagedResourceEditor()
        {
        }

        public ManagedResourceEditor(EditorInterface editorInterface)
        {
	        this.editorInterface = editorInterface;
        }

        Texture IManagedResourceEditor.GetIcon(string name)
        {
	        return editorInterface.GetBaseControl().GetIcon(name, "EditorIcons");
        }

        public MemberPropertyEditor FindPropertyEditor(Type t)
        {
	        foreach (var propertyEditor in ReflectionDataHelper.GetMemberPropertyEditors())
	        {
		        if (propertyEditor.Handles(t)) return propertyEditor;
	        }
            return null;
        }

        public override bool CanHandle(Object obj)
        {
            return obj is IManagedDataSerializable;
        }

        public override bool ParseProperty(Object obj, int type, string path, int hint, string hintText, int usage)
        {
	        return type == (int)Variant.Type.Dictionary &&  path.EndsWith("serializedData") || path.EndsWith("serializedReferences");
        }
        
        public override void ParseCategory(Object obj, string category)
        {
            if (category != "Script Variables") return;
            CreateSubDataEditor(obj);
        }

        void CreateSubDataEditor(Object obj)
        {
            Target = (IManagedDataSerializable)obj;
            var members = ReflectionDataHelper.ResolveHandledMembers(Target.GetType(), ReflectionDataHelper.IsEditableRootContent);
            foreach (var memberInfo in members)
            {
                var t = ReflectionDataHelper.GetContentType(memberInfo);
                var propEditor = FindPropertyEditor(t);
                if (propEditor == null)
                {
                    GD.PushError($"Property Editor not found for {t}");
                    continue;
                }
                propEditor = (MemberPropertyEditor)propEditor.Duplicate();
                propEditor.Create(this, obj, memberInfo, null);
                propEditor.ValueChanged += PropEditorOnValueChanged;
                AddCustomControl(propEditor);
                activeEditors.Add(propEditor);
            }
        }

        void PropEditorOnValueChanged(MemberPropertyEditor sender, object value)
        {
	        ReflectionDataHelper.SetMemberContent(sender.MemberInfo, Target, value);
	        sender.RebuildEditorContent(Target);
        }

        public void Cleanup()
        {
            foreach (var editor in activeEditors)
            {
                editor.TearDown();
                editor.ValueChanged -= PropEditorOnValueChanged;
                editor.QueueFree();
            }
            activeEditors.Clear();
        }
    }
}