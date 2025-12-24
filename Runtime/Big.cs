using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;

namespace Noya.BigNumbers
{
	/// <summary>
	/// Represents a scientific-notation number between 1 and 9.999e+4294967295 (<see cref="TYPE_LIMIT"/>).
	/// </summary>
	/// <remarks>A <see cref="Big"/> can never be lower than 1.0.</remarks>
	[Serializable]
	public struct Big : IEquatable<Big>
	{
		/// <summary>
		/// Represents the maximum allowable difference between exponents for performing certain arithmetic operations on <see cref="Big"/> numbers.
		/// If the difference between the exponents of two numbers exceeds this value, an operation may be skipped or simplified to avoid
		/// unnecessary computation involving insignificant contributions from smaller numbers.
		/// </summary>
		private const uint MIN_EXPONENT_DIFFERENCE = 16;
		/// <summary>
		/// String representing the highest value a <see cref="Big"/> can have.
		/// </summary>
		public const string TYPE_LIMIT = "9.999e+4294967295";
		
		public float Base;
		public uint Exponent;
		
		public static Big MaxValue => new(9.999f, uint.MaxValue);
		public static Big MinValue => 1u;

		
		public Big(float baseValue, uint exponentValue = 0u)
		{
			if (exponentValue == uint.MaxValue && baseValue >= 10)
			{
				throw new ExceededBigException();
			}
			
			Base = baseValue;
			Exponent = exponentValue;
			
			if (Base == 0)
			{
				Exponent = 0;
			}
			
			// Simplify the values so that Base is always between [1..9.99] (exclusive) unless [0.1e0..0.9e0]
			while (Base >= 10)
			{
				Base /= 10f;
				Exponent++;
			}
			
			while (Base is > 0f and < 1f && Exponent > 1)
			{
				Base *= 10f;
				Exponent--;
			}
		}
		
		public static bool TryParse(string value, out Big result)
		{
			result = new Big(0f, 0u);

			// Negative bases are not allowed
			if (string.IsNullOrEmpty(value) || value[0] == '-')
				return false;

			value = value.Replace(" ", string.Empty).ToLower();
			
			int eIndex = value.IndexOf('e');
			
			// If the number doesn't have an e, it might be a simple float
			if (eIndex == -1)
			{
				if (float.TryParse(value, out float baseFloatSole))
				{
					result = new Big(baseFloatSole, 0u);
					return true;
				}

				return false;
			}

			string baseString = value[..eIndex];
			string exponentString = value[(eIndex + 1)..];

			// Negative exponents are not allowed
			if (!string.IsNullOrEmpty(exponentString) && exponentString.Length > 0 && exponentString[0] == '-')
			{
				return false;
			}

			if (!float.TryParse(baseString, out float baseFloat) || !uint.TryParse(exponentString, out uint exponent))
			{
				return false;
			}
			
			result = new Big(baseFloat, exponent);
			return true;
		}
		
		public override string ToString()
		{
			return Exponent <= 3
				? $"{Base * Math.Pow(10, Exponent):N0}"
				: $"{Base:F2}e{Exponent}";
		}

		public static implicit operator Big(float floating) => new(floating);
		public static implicit operator Big(int integer) => new(integer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big operator +(Big a, Big b)
		{
			int exponentDifference = (int)a.Exponent - (int)b.Exponent;
			
			// If the exponent difference is higher than an arbitrary value, don't bother adding up
			// the numbers because one of the two would have such a minor influence on the other
			if (Math.Abs(exponentDifference) > MIN_EXPONENT_DIFFERENCE)
			{
				return a.Exponent > b.Exponent ? a : b;
			}
			
			// > 0 means a > b
			// < 0 means a < b
			return exponentDifference switch
			{
				> 0 => new Big(a.Base + b.Base / Mathf.Pow(10f, exponentDifference), a.Exponent),
				< 0 => new Big(a.Base / Mathf.Pow(10f, Math.Abs(exponentDifference)) + b.Base, b.Exponent),
				var _ => new Big(a.Base + b.Base, b.Exponent)
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big operator -(Big a, Big b)
		{
			// A Big can never be lower than 1e0
			if (a <= b)
				return MinValue;
			
			int exponentDifference = (int)a.Exponent - (int)b.Exponent;
			
			// If the exponent difference is higher than an arbitrary value, don't bother adding up the numbers
			// because one of the two would have such a minor influence on the other
			if (Math.Abs(exponentDifference) > MIN_EXPONENT_DIFFERENCE)
			{
				return a.Exponent > b.Exponent ? a : b;
			}

			// > 0 means a > b
			// < 0 means a < b
			return exponentDifference switch
			{
				> 0 => new Big(a.Base - b.Base / Mathf.Pow(10f, exponentDifference), a.Exponent),
				< 0 => new Big(a.Base / Mathf.Pow(10f, Math.Abs(exponentDifference)) - b.Base, b.Exponent),
				var _ => new Big(a.Base - b.Base, b.Exponent)
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big operator *(Big a, Big b)
		{
			return new Big(a.Base * b.Base, a.Exponent + b.Exponent);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big operator /(Big a, Big b)
		{
			// Prevent division-by-zero
			if (b.Base == 0) throw new DivideByZeroException();
			
			// Prevent divisions that would lead to values < 1
			if (a.Exponent < b.Exponent) throw new NumberSmallerThanOneException();
			
			return new Big(a.Base / b.Base, a.Exponent - b.Exponent);
		}

		public static Big operator +(Big a, uint b) => a + new Big(b);
		public static Big operator -(Big a, uint b) => a - new Big(b);
		public static Big operator *(Big a, float b) => new(a.Base * b, a.Exponent);
		public static Big operator /(Big a, float b) => new(a.Base / b, a.Exponent);
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		public static bool operator ==(Big a, Big b) => a.Exponent == b.Exponent && a.Base == b.Base;
		public static bool operator !=(Big a, Big b) => !(a == b);
		public static bool operator >(Big a, Big b) => a.Exponent != b.Exponent ? a.Exponent > b.Exponent : a.Base > b.Base;
		public static bool operator <(Big a, Big b) => a.Exponent != b.Exponent ? a.Exponent < b.Exponent : a.Base < b.Base;
		public static bool operator >=(Big a, Big b) => a.Exponent != b.Exponent ? a.Exponent >= b.Exponent : a.Base >= b.Base;
		public static bool operator <=(Big a, Big b) => a.Exponent != b.Exponent ? a.Exponent <= b.Exponent : a.Base <= b.Base;

		// ReSharper disable once Unity.BurstLoadingManagedType
		public override bool Equals(object obj) => obj is Big other && Equals(other);
		public bool Equals(Big other) => Base.Equals(other.Base) && Exponent == other.Exponent;
		public override int GetHashCode() => HashCode.Combine(Base, Exponent);
	}

	[CustomPropertyDrawer(typeof(Big))]
	public class BigIntDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Tell Unity that the following properties belong together.
			EditorGUI.BeginProperty(position, label, property);

			// Draw the label
			position = EditorGUI.PrefixLabel(position, label);

			// Get the internal serialized properties
			SerializedProperty baseProperty = property.FindPropertyRelative("Base");
			SerializedProperty exponentProperty = property.FindPropertyRelative("Exponent");

			// Determine the current value string to display

			string currentStringValue = exponentProperty.uintValue > 0 
				? $"{baseProperty.floatValue:F2}e{exponentProperty.uintValue}" 
				: baseProperty.floatValue.ToString("F2");

			// Create the text field, setting the input string
			string newStringValue = EditorGUI.TextField(position, currentStringValue);

			// Check if the user modified the string
			if (newStringValue != currentStringValue)
			{
				// Attempt to parse the new input string using the static TryParse method
				if (Big.TryParse(newStringValue, out Big parsedBigInt))
				{
					// If parsing is successful, update the serialized fields.
					// This ensures the data is saved correctly in the Scriptable Object.
					baseProperty.floatValue = parsedBigInt.Base;
					exponentProperty.uintValue = parsedBigInt.Exponent;
				}
				// If parsing fails, the input is left in the text field, allowing the user to correct it.
			}

			EditorGUI.EndProperty();
		}
	}

	
	public class NumberSmallerThanOneException : Exception
	{
		public NumberSmallerThanOneException() : base("Number is smaller than 1.") { }
	}

	public class ExceededBigException : Exception
	{
		public ExceededBigException() : base($"Exceeded {nameof(Big)} type limit ({Big.TYPE_LIMIT}).") { }
	}
}
