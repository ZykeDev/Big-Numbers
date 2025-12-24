using System.Runtime.CompilerServices;

namespace Noya.BigNumbers
{
	/// <summary>
	/// A collection of common math functions for <see cref="Big"/> types.
	/// </summary>
	public static class Mathb
	{
		/// <summary>
		/// Returns the max between two <see cref="Big"/>s.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big Max(Big a, Big b) => a >= b ? a : b;
		
		/// <summary>
		/// Returns the min between two <see cref="Big"/>s.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big Min(Big a, Big b) => a <= b ? a : b;
		
		/// <summary>
		/// Clamps the passed <see cref="Big"/> value between a min and a max (both inclusive).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Big Clamp(Big value, Big min, Big max)
		{
			if (value < min) value = min;
			else if (value > max) value = max;
			return value;
		}
	}
}
