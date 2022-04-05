using Godot;
using ManagedResourceEditor;

[Tool]
public class TestStoredData: ManagedDataStorage
{
	[ExportCustom]
	public TestStruct TestField
	{
		get => TryRestore<TestStruct>(nameof(TestField), out var value) ? value : default;
		set => Store(nameof(TestField), value);
	}
}