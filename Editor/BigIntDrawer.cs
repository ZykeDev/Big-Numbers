using UnityEditor;
using UnityEngine;

namespace Noya.BigNumbers
{
	[CustomPropertyDrawer(typeof(Big))]
	public class BigIntDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);

			SerializedProperty baseProperty = property.FindPropertyRelative("Base");
			SerializedProperty exponentProperty = property.FindPropertyRelative("Exponent");

			// Determine the current value string to display
			string currentStringValue = exponentProperty.uintValue > 0 
				? $"{baseProperty.floatValue:F2}e{exponentProperty.uintValue}" 
				: baseProperty.floatValue.ToString("F2");

			string newStringValue = EditorGUI.TextField(position, currentStringValue);

			if (newStringValue != currentStringValue)
			{
				if (Big.TryParse(newStringValue, out Big parsedBigInt))
				{
					// If parsing is successful, update the serialized fields
					// This ensures the data is saved correctly in the Scriptable Object
					baseProperty.floatValue = parsedBigInt.Base;
					exponentProperty.uintValue = parsedBigInt.Exponent;
				}
			}

			EditorGUI.EndProperty();
		}
	}
}
