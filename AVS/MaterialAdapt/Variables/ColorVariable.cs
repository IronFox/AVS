using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt.Variables
{

    internal readonly struct ColorVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Color;
        public Color Value { get; }
        public string Name { get; }

        public ColorVariable(Material m, string n)
        {
            Value = m.GetColor(n);
            Name = n;
        }

        /// <summary>
        /// Sets the color property of a material with the given value and logs the change.
        /// </summary>
        /// <param name="m">The material on which the color property will be set.</param>
        /// <param name="name">The name of the color property to set.</param>
        /// <param name="value">The new color value to assign to the property.</param>
        /// <param name="logConfig">The log configuration used to log the operation.</param>
        /// <param name="materialName">Optional custom material name for logging purposes.</param>
        /// <param name="rmc">Root mod controller for logging purposes</param>
        public static void Set(RootModController rmc, Material m, string name, Color value, MaterialLog logConfig, string? materialName)
        {
            using var log = SmartLog.LazyForAVS(rmc);
            try
            {
                var old = m.GetColor(name);
                if (old == value)
                    return;
                logConfig.LogMaterialVariableSet(log, ShaderPropertyType.Color, name, old, value, m, materialName);
                m.SetColor(name, value);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to set color {name} ({value}) on {materialName ?? m.NiceName()}", ex);
            }
        }

        public void SetTo(RootModController rmc, Material m, MaterialLog logConfig, string? materialName)
        {
            Set(rmc, m, Name, Value, logConfig, materialName);
        }
    }

}
