using Godot;

namespace addons.SubDataInspector
{
    /// <summary>
    /// implementing type must be marked as [Tool]
    /// </summary>
    public interface IManagedDataSerializable: ISerializationListener
    {
        //Must be [Export]ed
        Godot.Collections.Dictionary SerializedData { get; set; }
    }
}