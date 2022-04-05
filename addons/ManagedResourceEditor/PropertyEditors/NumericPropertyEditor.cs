using System;
using Godot;

namespace ManagedResourceEditor.PropertyEditors
{
	[Tool]
	public class NumericPropertyEditor: MemberPropertyEditor
	{
		public override event Action<MemberPropertyEditor, object> ValueChanged;

		SpinBox input;
		const string ChangeSignal = "value_changed";

		public SpinBox Input => input;

		public override bool Handles(Type t)
		{
			if (t == typeof(float)) return true;
			if (t == typeof(double)) return true;
			if (t == typeof(int)) return true;
			if (t == typeof(uint)) return true;
			if (t == typeof(short)) return true;
			if (t == typeof(ushort)) return true;
			if (t == typeof(byte)) return true;
			if (t == typeof(sbyte)) return true;
			if (t == typeof(long)) return true;
			if (t == typeof(ulong)) return true;
			return false;
		}

		protected override void SetupContentEditor()
		{
			input = new SpinBox();
			input.Rounded = ContentType != typeof(float) && ContentType != typeof(double);
			SetupInputRange();
			input.GrowHorizontal = GrowDirection.End;
			input.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
			AddContentEditorInline(input);
			RebuildEditorContent(Container);
		}

		public override void RebuildEditorContent(object container)
		{
			Container = container;
			if (input.IsConnected(ChangeSignal, this, nameof(OnValueChanged))) input.Disconnect(ChangeSignal, this, nameof(OnValueChanged));
			input.Value = Convert.ToDouble(GetMemberValue());
			input.Connect(ChangeSignal, this, nameof(OnValueChanged));
		}

		void SetupInputRange()
		{
			double min;
			double max;
			if (ContentType == typeof(float))
			{
				min = float.MinValue;
				max = float.MaxValue;
			}
			else if (ContentType == typeof(double))
			{
				min = double.MinValue;
				max = double.MaxValue;
			}
			else if (ContentType == typeof(int))
			{
				min = int.MinValue;
				max = int.MaxValue;
			}
			else if (ContentType == typeof(uint))
			{
				min = uint.MinValue;
				max = uint.MaxValue;
			}
			else if (ContentType == typeof(short))
			{
				min = short.MinValue;
				max = short.MaxValue;
			}
			else if (ContentType == typeof(ushort))
			{
				min = ushort.MinValue;
				max = ushort.MaxValue;
			}
			else if (ContentType == typeof(byte))
			{
				min = byte.MinValue;
				max = byte.MaxValue;
			}
			else if (ContentType == typeof(sbyte))
			{
				min = sbyte.MinValue;
				max = sbyte.MaxValue;
			}
			else if (ContentType == typeof(long))
			{
				min = long.MinValue;
				max = long.MaxValue;
			}
			else if (ContentType == typeof(ulong))
			{
				min = ulong.MinValue;
				max = ulong.MaxValue;
			}
			else
			{
				throw new NotSupportedException();
			}
			input.MinValue = min;
			input.MaxValue = max;
		}

		void OnValueChanged(double value)
		{
			ValueChanged?.Invoke(this, Convert.ChangeType(value, ContentType));
		}

		public override void TearDown()
		{
			if (input.IsConnected(ChangeSignal, this, nameof(OnValueChanged))) input.Disconnect(ChangeSignal, this, nameof(OnValueChanged));
			input.QueueFree();
			base.TearDown();
		}
	}
}
