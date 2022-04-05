using System;
using System.Collections.Generic;
using Godot;

namespace ManagedResourceEditor
{
    public interface IManagedResourceEditor
    {
	    IManagedDataSerializable Target { get; }
	    Dictionary<int, bool> InspectorFoldStates { get; }

	    MemberPropertyEditor FindPropertyEditor(Type t);
        Texture GetIcon(string name);
    }
}