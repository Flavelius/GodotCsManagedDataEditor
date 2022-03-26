using System;

namespace addons.SubDataInspector
{
    public interface IInspectorResourceProvider
    {
        MemberPropertyEditor FindPropertyEditor(Type t);
    }
}