using System;
using System.Reflection;
using Godot;

namespace addons.SubDataInspector
{
    [Tool]
    public abstract class MemberPropertyEditor: EditorProperty
    {
        public object Target { get; private set; }
        public MemberInfo MemberInfo { get; private set; }
        public Type ContentType { get; private set; }
        
        public abstract bool Handles(Type t);

        public abstract event Action<MemberPropertyEditor> ValueChanged;

        public void Attach(MemberPropertyEditor parent, IInspectorResourceProvider resources, object target, MemberInfo memberInfo)
        {
            Target = target;
            MemberInfo = memberInfo;
            if (MemberInfo is FieldInfo f) ContentType = f.FieldType;
            else if (MemberInfo is PropertyInfo p) ContentType = p.PropertyType;
            else throw new NotSupportedException();
            OnAttach(parent, resources);
        }

        protected abstract void OnAttach(MemberPropertyEditor parent, IInspectorResourceProvider resources);

        public virtual void AddChildEditor(MemberPropertyEditor child)
        {
            AddChild(child);
        }
        
        public abstract void Detach();

        public abstract object GetValue();
    }

    public abstract class MemberPropertyEditor<T> : MemberPropertyEditor
    {
        public sealed override bool Handles(Type t) => typeof(T) == t;

        protected T GetMemberValue()
        {
            var val = ReflectionDataHelper.GetMemberContent(MemberInfo, Target);
            if (val != null) return (T)val;
            if (typeof(T).IsValueType)
            {
                return default;
            }
            return default;
        }
    }
}