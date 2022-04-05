using System;
using System.Reflection;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class EnumPropertyEditor: MemberPropertyEditor
	{
		MenuButton input;
		const string InputSignal = "index_pressed";

		public MenuButton Input => input;

		public override bool Handles(Type t)
		{
			return t.IsEnum;
		}

		public override event Action<MemberPropertyEditor, object> ValueChanged;

		Array enumValues = Array.Empty<Enum>();
		string[] enumNames;

		protected override void SetupContentEditor()
		{
			enumValues = Enum.GetValues(ContentType);
			enumNames = Enum.GetNames(ContentType);
			input = new MenuButton();
			input.GrowHorizontal = GrowDirection.End;
			input.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			AddContentEditorInline(input);
			RebuildEditorContent(Container);
		}

		public override void RebuildEditorContent(object container)
		{
			Container = container;
			if (input.GetPopup().IsConnected(InputSignal, this, nameof(OnIndexSelected))) input.GetPopup().Disconnect(InputSignal, this, nameof(OnIndexSelected));
			var popup = input.GetPopup();
			popup.Clear();
			var memberVal = GetMemberValue();
			if (ContentType.IsDefined(typeof(FlagsAttribute)))
			{
				var numericVal = Convert.ToUInt64(memberVal);
				for (int i = 0; i < enumNames.Length; i++)
				{
					popup.AddCheckItem(enumNames[i]);
					var numericItem = Convert.ToUInt64(enumValues.GetValue(i));
					popup.SetItemChecked(i, (numericVal & numericItem) == numericItem);
				}
				input.Text = memberVal.ToString();
			}
			else
			{
				var currentIndex = 0;
				for (int i = 0; i < enumValues.Length; i++)
				{
					if (enumValues.GetValue(i).Equals(memberVal))
					{
						currentIndex = i;
						break;
					}
				}
				for (int i = 0; i < enumNames.Length; i++)
				{
					popup.AddCheckItem(enumNames[i]);
					if (i == currentIndex)
					{
						popup.SetItemChecked(i, true);
						input.Text = enumNames[i];
					}
				}
			}
			input.GetPopup().Connect(InputSignal, this, nameof(OnIndexSelected));
		}

		void OnIndexSelected(int index)
		{
			var popup = input.GetPopup();
			var itemsCount = popup.GetItemCount();
			var isFlags = ContentType.IsDefined(typeof(FlagsAttribute));
			if (isFlags)
			{
				var itemValue = Convert.ToUInt64(enumValues.GetValue(index));
				var numericValue = Convert.ToUInt64(GetMemberValue());
				numericValue ^= itemValue;
				for (int i = 0; i < itemsCount; i++)
				{
					itemValue = Convert.ToUInt64(enumValues.GetValue(i));
					if (itemValue == 0 && numericValue > 0) popup.SetItemChecked(i, false); //visually disabling the usual empty/none/invalid/undefined item, even though technically not correct
					else popup.SetItemChecked(i, (numericValue & itemValue) == itemValue);
				}
				var result = Enum.ToObject(ContentType, numericValue);
				input.Text = result.ToString();
				ValueChanged?.Invoke(this, result);
			}
			else
			{
				for (int i = 0; i < itemsCount; i++)
				{
					popup.SetItemChecked(i, i == index);
				}
				var result = Enum.ToObject(ContentType, enumValues.GetValue(index));
				input.Text = result.ToString();
				ValueChanged?.Invoke(this, result);
			}
		}

		public override void TearDown()
		{
			if (input.GetPopup().IsConnected(InputSignal, this, nameof(OnIndexSelected))) input.GetPopup().Disconnect(InputSignal, this, nameof(OnIndexSelected));
			input.QueueFree();
			base.TearDown();
		}
	}
}
