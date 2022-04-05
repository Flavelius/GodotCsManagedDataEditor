using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class CollectionPropertyEditor : ToggleableMemberPropertyEditor
	{
		Type innerType;
		MarginContainer marginContainer;
		VBoxContainer collectionContainer;

		readonly List<(MemberPropertyEditor, Button)> children = new List<(MemberPropertyEditor, Button)>();
		
		Button addButton;
		const string ButtonSignal = "pressed";

		public Type InnerType => innerType;
		public MarginContainer MarginContainer => marginContainer;
		public VBoxContainer CollectionContainer => collectionContainer;
		public List<(MemberPropertyEditor, Button)> Children => children;

		public override event Action<MemberPropertyEditor, object> ValueChanged;

		public override bool Handles(Type t)
		{
			if (t.IsArray) return true;
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return true;
			return false;
		}

		protected override void OnFoldedStateChanged(bool state)
		{
			base.OnFoldedStateChanged(state);
			collectionContainer.Visible = state;
		}

		protected override void SetupContentEditor()
		{
			base.SetupContentEditor();
			marginContainer = new MarginContainer();
			marginContainer.AddConstantOverride("margin_left", FoldoutMargin);
			AddChild(marginContainer);
			addButton = new Button();
			addButton.Text = "+";
			addButton.ActionMode = BaseButton.ActionModeEnum.Release;
			addButton.Connect(ButtonSignal, this, nameof(OnAddClicked));
			HorizontalContainer.AddChild(addButton);
			innerType = ContentType.IsArray ? ContentType.GetElementType() : ContentType.GetGenericArguments()[0];
			if (innerType == null) throw new Exception($"Cannot retrieve inner collection type for: {MemberInfo.Name}");
			collectionContainer = new VBoxContainer();
			collectionContainer.Name = "Collection Items";
			collectionContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			marginContainer.AddChild(collectionContainer);
			collectionContainer.Visible = ToggledState;
			RebuildEditorContent(Container);
		}

		void OnAddClicked()
		{
			GD.Print("Adding collection item");
			var target = (IList)GetMemberValue();
			if (ContentType.IsArray)
			{
				var newTarget = Array.CreateInstance(innerType, target.Count + 1);
				for (int i = 0; i < target.Count; i++)
				{
					newTarget.SetValue(target[i], i);
				}
				SetMemberValue(newTarget);
				ValueChanged?.Invoke(this, newTarget);
			}
			else
			{
				target.Add(Activator.CreateInstance(innerType));
				ValueChanged?.Invoke(this, target);
			}
		}

		void OnPropEditorValueChanged(MemberPropertyEditor sender, object value)
		{
			var target = (IList)GetMemberValue();
			target[sender.CollectionIndex] = value;
			ValueChanged?.Invoke(this, target);
		}

		public override void RebuildEditorContent(object container)
		{
			RemoveChildren();
			Container = container;
			var target = (IList)GetMemberValue();
			var pendingRebuild = false;
			if (target == null)
			{
				if (ContentType.IsArray) target = Array.CreateInstance(innerType, 0);
				else target = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(innerType));
				SetMemberValue(target);
				pendingRebuild = true;
			}
			if (target.Count > 0)
			{
				var template = Resources.FindPropertyEditor(innerType);
				if (template == null)
				{
					GD.PushError($"Property Editor not found for {innerType}");
					return;
				}
				for (int i = 0; i < target.Count; i++)
				{
					var propEditor = (MemberPropertyEditor)template.Duplicate();
					collectionContainer.AddChild(propEditor);
					propEditor.Create(Resources, target, MemberInfo, this, i);
					propEditor.ValueChanged += OnPropEditorValueChanged;
					var removeButton = new Button();
					removeButton.SizeFlagsHorizontal = (int)SizeFlags.ShrinkCenter;
					removeButton.Text = "X";
					removeButton.Connect(ButtonSignal, this, nameof(OnRemoveButtonClicked), new Godot.Collections.Array(i));
					propEditor.HorizontalContainer.AddChild(removeButton);
					children.Add((propEditor, removeButton));
				}
			}
			if (pendingRebuild) ValueChanged?.Invoke(this, target);
		}

		void OnRemoveButtonClicked(int removedIndex)
		{
			var collection = (IList)GetMemberValue();
			if (ContentType.IsArray)
			{
				var newTarget = Array.CreateInstance(innerType, collection.Count - 1);
				var currentIndex = 0;
				for (int newIndex = 0; newIndex < newTarget.Length; newIndex++, currentIndex++)
				{
					if (currentIndex == removedIndex) currentIndex++;
					newTarget.SetValue(collection[currentIndex], newIndex);
				}
				SetMemberValue(newTarget);
				ValueChanged?.Invoke(this, newTarget);
			}
			else
			{
				collection.RemoveAt(removedIndex);
				ValueChanged?.Invoke(this, collection);
			}
		}

		void RemoveChildren()
		{
			foreach (var (editor, btn) in children)
			{
				if (btn.IsConnected(ButtonSignal, this, nameof(OnRemoveButtonClicked))) btn.Disconnect(ButtonSignal, this, nameof(OnRemoveButtonClicked));
				btn.QueueFree();
				editor.TearDown();
				editor.ValueChanged -= OnPropEditorValueChanged;
				editor.QueueFree();
			}
			children.Clear();
		}

		public override void TearDown()
		{
			RemoveChildren();
			if (addButton.IsConnected(ButtonSignal, this, nameof(OnAddClicked))) addButton.Disconnect(ButtonSignal, this, nameof(OnAddClicked));
			addButton.QueueFree();
			collectionContainer.QueueFree();
			marginContainer.QueueFree();
			base.TearDown();
		}
	}
}
