using addons.SubDataInspector;
using Godot;
using Godot.Collections;

[Tool]
public class TestNode: Node, IManagedDataSerializable
{
    [Export]
    public Dictionary SerializedData { get; set; }

    [CustomExport]
    TestStruct testField;
    
    [CustomExport]
    TestStruct testField2;

    public override void _Ready()
    {
        OnAfterDeserialize();
    }

    public void OnBeforeSerialize()
    {
        ReflectionDataHelper.StoreCustomExports(this);
    }

    public void OnAfterDeserialize()
    {
        ReflectionDataHelper.RestoreCustomExports(this);
    }
}