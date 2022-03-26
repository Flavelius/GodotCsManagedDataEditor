using Godot;

namespace addons.SubDataInspector
{
    [Tool]
    public class Plugin : EditorPlugin
    {
        SubDataInspector inspectorPlugin;
    
        public override void _EnterTree()
        {
            inspectorPlugin = new SubDataInspector();
            GD.Print("Loaded");
            AddInspectorPlugin(inspectorPlugin);
        }

        public override void _ExitTree()
        {
            inspectorPlugin.Cleanup();
            RemoveInspectorPlugin(inspectorPlugin);
        }
    }
}
