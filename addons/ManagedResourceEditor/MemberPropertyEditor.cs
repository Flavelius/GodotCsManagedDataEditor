using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Godot;

namespace ManagedResourceEditor
{
	[Tool]
	public abstract class MemberPropertyEditor : VBoxContainer
	{
		public object Container { get; protected set; }
		public MemberInfo MemberInfo { get; private set; }
		public Type ContentType { get; private set; }
		public int CollectionIndex { get; private set; } = -1;
		public MemberPropertyEditor Parent { get; private set; }

		public abstract event Action<MemberPropertyEditor, object> ValueChanged;

		public abstract bool Handles(Type t);

		public HBoxContainer HorizontalContainer { get; private set; }
		public Label Label { get; private set; }

		public const int FoldoutMargin = 24;

		public IManagedResourceEditor Resources { get; private set; }

		protected object GetMemberValue()
		{
			if (Container is IList list) return list[CollectionIndex];
			return ReflectionDataHelper.GetMemberContent(MemberInfo, Container);
		}

		protected void SetMemberValue(object val)
		{
			if (Container is IList list) list[CollectionIndex] = val;
			else ReflectionDataHelper.SetMemberContent(MemberInfo, Container, val);
		}

		public void Create(IManagedResourceEditor resources, object owner, MemberInfo memberInfo, MemberPropertyEditor parent, int collectionIndex = -1)
		{
			Parent = parent;
			CollectionIndex = collectionIndex;
			Resources = resources;
			SizeFlagsVertical = (int)SizeFlags.ShrinkCenter;
			HorizontalContainer = new HBoxContainer();
			AddConstantOverride("separation", 0);
			AddChild(HorizontalContainer);
			Label = new Label();
			Label.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			HorizontalContainer.AddChild(Label);
			Container = owner;
			MemberInfo = memberInfo;
			Label.Text = GetDefaultLabelText();
			if (Container is IList)
			{
				var targetType = Container.GetType();
				targetType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments()[0];
				ContentType = targetType;
			}
			else
			{
				if (MemberInfo is FieldInfo f) ContentType = f.FieldType;
				else if (MemberInfo is PropertyInfo p) ContentType = p.PropertyType;
				else throw new NotSupportedException();
			}
			Name = "PropEditor:" + Label.Text;
			SetupContentEditor();
			foreach (var modifierAttribute in MemberInfo.GetCustomAttributes<InspectorModifierAttribute>(true))
			{
				modifierAttribute.ApplyModifications(this);
			}
		}

		public abstract void RebuildEditorContent(object container);

		protected string GetDefaultLabelText()
		{
			if (Container is IList)
			{
				return $"[{CollectionIndex}]";
			}
			var input = MemberInfo.Name;
			var builder = new StringBuilder();
			foreach (var c in input)
			{
				if (char.IsUpper(c) && builder.Length > 0) builder.Append(' ');
				builder.Append(c);
			}
			var result = builder.ToString();
			return result[0].ToString().ToUpper() + result.Substring(1);
		}

		protected abstract void SetupContentEditor();

		protected void AddContentEditorInline(Control control)
		{
			HorizontalContainer.AddChild(control);
		}

		public virtual void TearDown()
		{
			HorizontalContainer.QueueFree();
			if (IsInstanceValid(Label)) Label.QueueFree();
		}
	}
}
