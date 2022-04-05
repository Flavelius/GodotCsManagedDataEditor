using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	public abstract class ToggleableMemberPropertyEditor : MemberPropertyEditor
	{
		int GetSemiUniqueID()
		{
			return Container.GetType().GetHashCode() ^ MemberInfo.Name.GetHashCode() ^ CollectionIndex;
		}

		protected bool ToggledState => foldBox.Pressed;

		CheckBox foldBox;
		const string FoldBoxSignal = "toggled";

		public CheckBox FoldToggle => foldBox;

		protected virtual void OnFoldedStateChanged(bool state)
		{
			Resources.InspectorFoldStates[GetSemiUniqueID()] = state;
		}

		protected override void SetupContentEditor()
		{
			foldBox = new CheckBox();
			var rightIcon = Resources.GetIcon("GuiTreeArrowRight");
			var downIcon = Resources.GetIcon("GuiTreeArrowDown");
			foldBox.AddIconOverride("checked", downIcon);
			foldBox.AddIconOverride("unchecked", rightIcon);
			foldBox.SizeFlagsVertical = (int)SizeFlags.ShrinkCenter;
			foldBox.Align = Button.TextAlign.Center;
			foldBox.Text = GetDefaultLabelText();
			foldBox.Pressed = Resources.InspectorFoldStates.TryGetValue(GetSemiUniqueID(), out var state) && state;
			HorizontalContainer.AddConstantOverride("separation", 0);

			foldBox.Connect(FoldBoxSignal, this, nameof(OnFoldedStateChanged));
			AddContentEditorInline(foldBox);
			foldBox.GetParent().MoveChild(foldBox, 0);
			Label.QueueFree();
		}

		public override void TearDown()
		{
			if (foldBox.IsConnected(FoldBoxSignal, this, nameof(OnFoldedStateChanged))) foldBox.Disconnect(FoldBoxSignal, this, nameof(OnFoldedStateChanged));
			foldBox.QueueFree();
			base.TearDown();
		}
	}
}
