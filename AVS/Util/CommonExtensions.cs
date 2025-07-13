using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS.Util
{
    public static class CommonExtensions
    {
        /// <summary>
        /// Converts a float into a string using the universal decimal sign (.)
        /// </summary>
        /// <param name="f">Float to convert</param>
        /// <returns>Converted float</returns>
        public static string ToStr(this float f)
        {
            return f.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a string into a float using the universal decimal sign (.)
        /// </summary>
        /// <param name="s">String to try parse</param>
        /// <param name="f">Resulting float</param>
        /// <returns>True on success</returns>
        public static bool ToFloat(this string s, out float f)
        {
            return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out f);
        }
    }
}
