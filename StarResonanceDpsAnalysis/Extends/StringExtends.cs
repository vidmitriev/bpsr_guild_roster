﻿using System.Runtime.CompilerServices;

namespace StarResonanceDpsAnalysis.Extends
{
    public static class StringExtends
    {
        #region ToInt()

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIntEx(this string? str)
        {
            return Convert.ToInt32(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this string? str, int def = 0)
        {
            if (TryToInt(str, out int result))
            {
                return result;
            }

            return def;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToInt(this string? str, out int result)
        {
            return int.TryParse(str, out result);
        }

        #endregion

        #region ToDouble()

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDoubleEx(this string? str)
        {
            return Convert.ToDouble(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDouble(this string? str, double def = 0.0)
        {
            if (TryToDouble(str, out double result))
            {
                return result;
            }
            return def;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToDouble(this string? str, out double result)
        {
            return double.TryParse(str, out result);
        }

        #endregion
    }
}
