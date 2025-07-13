using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleComponents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AVS.Saving
{
    /// <summary>
    /// Color representation used for saving and loading vehicle colors.
    /// </summary>
    public class SavedColor
    {
        /// <summary>
        /// Hue, Saturation, Brightness (HSB) representation of the color.
        /// </summary>
        public string? HSB { get; set; }
        /// <summary>
        /// RGB representation of the color in hex format (e.g., #FF5733).
        /// </summary>
        public string? RGB { get; set; }

        /// <summary>
        /// Resets the local color representation from the given color.
        /// </summary>
        /// <param name="color"></param>
        public static SavedColor From(VehicleColor color)
        {
            return new SavedColor {
                RGB = '#' + ColorUtility.ToHtmlStringRGB(color.RGB),
                HSB = $"{color.HSB.x.ToStr()},{color.HSB.y.ToStr()},{color.HSB.z.ToStr()}"
            };
        }

        public void WriteTo(ref VehicleColor color)
        {
            var c = ToColor();
            if (c != null)
            {
                color = c.Value;
            }
            else
            {
                Logger.Error($"Failed to convert SavedColor to VehicleColor. RGB: {RGB}, HSB: {HSB}");
                color = VehicleColor.Default;
            }
        }

        /// <summary>
        /// Converts the local color representation to a <see cref="VehicleColor"/>.
        /// </summary>
        /// <returns></returns>
        public VehicleColor? ToColor()
        {

            if (!ColorUtility.TryParseHtmlString(RGB, out var rgb))
                return null;

            var parts = HSB?.Split(',');
            if (parts == null
                || parts.Length != 3
                || parts[0].ToFloat(out var h)
                || parts[1].ToFloat(out var s)
                || parts[2].ToFloat(out var b))
            {
                Logger.Error($"Invalid HSB format: {HSB}. HSB will not be set");
                return new VehicleColor(rgb);
            }
            return new VehicleColor(rgb, new Vector3(h, s, b));
        }
    }
}
