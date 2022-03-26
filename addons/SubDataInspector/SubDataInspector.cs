using System;
using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

namespace addons.SubDataInspector
{
    [Tool]
    public class SubDataInspector : EditorInspectorPlugin, IInspectorResourceProvider
    {
        readonly List<MemberPropertyEditor> propertyEditorList = new List<MemberPropertyEditor>();
        readonly List<MemberPropertyEditor> activeEditors = new List<MemberPropertyEditor>();

        bool initialized;
        IManagedDataSerializable target;

        void EnsureInitialized()
        {
            if (initialized) return;
            var types = ReflectionDataHelper.GetGenericMemberEditorTypes();
            propertyEditorList.Add(new StructContainerPropertyEditor());
            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type) as MemberPropertyEditor;
                propertyEditorList.Add(instance);
            }
            initialized = true;
        }

        public MemberPropertyEditor FindPropertyEditor(Type t)
        {
            EnsureInitialized();
            for (int i = 0; i < propertyEditorList.Count; i++)
            {
                if (propertyEditorList[i].Handles(t)) return propertyEditorList[i];
            }
            return null;
        }

        public override bool CanHandle(Object obj)
        {
            return obj is IManagedDataSerializable;
        }

        public override bool ParseProperty(Object obj, int type, string path, int hint, string hintText, int usage)
        {
            return type == (int)Variant.Type.Dictionary && path.EndsWith(nameof(IManagedDataSerializable.SerializedData));
        }

        public override void ParseCategory(Object obj, string category)
        {
            if (category != "Script Variables") return;
            CreateSubDataEditor(obj);
        }

        void CreateSubDataEditor(Object obj)
        {
            target = obj as IManagedDataSerializable;
            var members = ReflectionDataHelper.ResolveHandledMembers(target.GetType(), ReflectionDataHelper.IsSupportedRootContent);
            foreach (var memberInfo in members)
            {
                var t = ReflectionDataHelper.GetContentType(memberInfo);
                var propEditor = FindPropertyEditor(t);
                if (propEditor == null)
                {
                    GD.PushError($"Property Editor not found for {t}");
                    continue;
                }
                propEditor = propEditor.Duplicate() as MemberPropertyEditor;
                propEditor.Attach(null, this, ReflectionDataHelper.GetMemberContent(memberInfo, obj), memberInfo);
                propEditor.ValueChanged += PropEditorOnValueChanged;
                AddPropertyEditor(memberInfo.Name, propEditor);
                activeEditors.Add(propEditor);
            }
        }

        void PropEditorOnValueChanged(MemberPropertyEditor sender)
        {
            ReflectionDataHelper.SetContentValue(sender.MemberInfo, target, sender.GetValue());
            ReflectionDataHelper.StoreCustomExports(target);
        }

        public void Cleanup()
        {
            foreach (var editor in activeEditors)
            {
                editor.Detach();
            }
        }
    }
}