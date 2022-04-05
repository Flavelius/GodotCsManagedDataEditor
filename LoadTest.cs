using Godot;
using System;

public class LoadTest : Node
{
	public override void _Ready()
	{
		var d = GD.Load<TestStoredData>("new_resource.tres");
		GD.Print(d.TestField);
	}
}
