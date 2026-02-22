using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Noya.BigNumbers
{
	/// <summary>
	/// Represents a scientific-notation number between 1 and 9.999e+4294967295 (<see cref="TYPE_LIMIT"/>).
	/// </summary>
	/// <remarks>A <see cref="Big"/> can never be lower than 1.0, but it can be <see cref="Zero"/> or <see cref="Infinity"/>.</remarks>
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
		public static Big Zero => new(0);
		public static float Infinity => Mathf.Infinity;

		
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big operator ^(Big a, float b)
		{
			// Zero bases always return 0
			if (a.Base == 0) return 0;

			// b * (log10(base) + exp)
			double totalLog = b * (Math.Log10(a.Base) + a.Exponent);
			uint newExp = (uint)Math.Floor(totalLog);
			
			return new Big(Mathf.Pow(10f, (float)(totalLog - newExp)), newExp);
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

		/// <summary>
		/// Converts a <see cref="Big"/> into an <see cref="int"/>.
		/// </summary>
		/// <remarks>If the realized <see cref="Big"/> would greater than <see cref="int.MaxValue"/>, returns <see cref="int.MaxValue"/>.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator int(Big a)
		{
			if (a.Exponent > Math.Log10(int.MaxValue))
				return int.MaxValue;
			
			return (int)(a.Base * Mathf.Pow(10f, a.Exponent));
		}

		/// <summary>
		/// Converts a <see cref="Big"/> into an <see cref="float"/>.
		/// </summary>
		/// <remarks>If the realized <see cref="Big"/> would greater than <see cref="float.MaxValue"/>, returns <see cref="float.MaxValue"/>.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator float(Big a)
		{
			if (a.Exponent > Math.Log10(float.MaxValue))
				return float.MaxValue;
			
			return a.Base * Mathf.Pow(10f, a.Exponent);
		}

		
		// ReSharper disable once Unity.BurstLoadingManagedType
		public override bool Equals(object obj) => obj is Big other && Equals(other);
		public bool Equals(Big other) => Base.Equals(other.Base) && Exponent == other.Exponent;
		public override int GetHashCode() => HashCode.Combine(Base, Exponent);
	}


	internal class NumberSmallerThanOneException : Exception
	{
		internal NumberSmallerThanOneException() : base("Number is smaller than 1.") { }
	}

	internal class ExceededBigException : Exception
	{
		internal ExceededBigException() : base($"Exceeded {nameof(Big)} type limit ({Big.TYPE_LIMIT}).") { }
	}
}
