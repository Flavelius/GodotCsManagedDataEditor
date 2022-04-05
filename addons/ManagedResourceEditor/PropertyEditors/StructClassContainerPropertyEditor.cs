using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class StructClassContainerPropertyEditor : ToggleableMemberPropertyEditor
	{
		MarginContainer marginContainer;
		VBoxContainer propertyContainer;

		readonly List<MemberPropertyEditor> children = new List<MemberPropertyEditor>();

		public MarginContainer MarginContainer => marginContainer;
		public VBoxContainer PropertyContainer => propertyContainer;
		public List<MemberPropertyEditor> Children => children;

		public override event Action<MemberPropertyEditor, object> ValueChanged;

		public override bool Handles(Type t)
		{
			return ReflectionDataHelper.IsCustomStruct(t) || ReflectionDataHelper.IsManagedClass(t);
		}

		protected override void OnFoldedStateChanged(bool state)
		{
			base.OnFoldedStateChanged(state);
			propertyContainer.Visible = state;
		}

		protected override void SetupContentEditor()
		{
			base.SetupContentEditor();
			marginContainer = new MarginContainer();
			marginContainer.AddConstantOverride("margin_left", FoldoutMargin);
			AddChild(marginContainer);
			propertyContainer = new VBoxContainer();
			propertyContainer.Name = "Struct Properties";
			propertyContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			marginContainer.AddChild(propertyContainer);
			propertyContainer.Visible = ToggledState;
			var mVal = GetMemberValue();
			if (mVal == null) //case of class
			{
				mVal = FormatterServices.GetUninitializedObject(ContentType);
				SetMemberValue(mVal);
			}
			foreach (var member in ReflectionDataHelper.ResolveHandledMembers(ContentType, ReflectionDataHelper.IsEditableNestedContent))
			{
				var t = ReflectionDataHelper.GetContentType(member);
				if (t == null) continue;
				var propEditor = Resources.FindPropertyEditor(t);
				if (propEditor == null)
				{
					GD.PushError($"Property Editor not found for {t}");
					continue;
				}
				propEditor = (MemberPropertyEditor)propEditor.Duplicate();
				propertyContainer.AddChild(propEditor);
				propEditor.Create(Resources, mVal, member, this);
				propEditor.ValueChanged += OnPropEditorValueChanged;
				children.Add(propEditor);
			}
		}

		public override void RebuildEditorContent(object container)
		{
			Container = container;
			var obj = GetMemberValue();
			for (int i = 0; i < children.Count; i++)
			{
				children[i].RebuildEditorContent(obj);
			}
		}

		void RemoveChildren()
		{
			foreach (var child in children)
			{
				child.TearDown();
				child.ValueChanged -= OnPropEditorValueChanged;
				child.QueueFree();
			}
			children.Clear();
		}

		public override void TearDown()
		{
			RemoveChildren();
			propertyContainer.QueueFree();
			marginContainer.QueueFree();
			base.TearDown();
		}

		void OnPropEditorValueChanged(MemberPropertyEditor sender, object value)
		{
			var mVal = GetMemberValue();
			ReflectionDataHelper.SetMemberContent(sender.MemberInfo, mVal, value);
			ValueChanged?.Invoke(this, mVal);
		}
	}
}
