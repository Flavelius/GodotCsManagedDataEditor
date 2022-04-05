using Godot;

namespace ManagedResourceEditor
{
    [Tool]
    public class Plugin : EditorPlugin
    {
        ManagedResourceEditor inspectorPlugin;
    
        public override void _EnterTree()
        {
            inspectorPlugin = new ManagedResourceEditor(GetEditorInterface());
            GD.Print($"Loaded {nameof(ManagedResourceEditor)}");
            AddInspectorPlugin(inspectorPlugin);
        }

        public override void _ExitTree()
        {
	        if (inspectorPlugin != null)
	        {
		        GD.Print($"Unloaded {nameof(ManagedResourceEditor)}");
		        inspectorPlugin.Cleanup();
		        RemoveInspectorPlugin(inspectorPlugin);
	        }
	        else
	        {
		        GD.PrintErr($"Failed Unloading {nameof(ManagedResourceEditor)}");
	        }
        }
    }
}
