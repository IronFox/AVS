using AVS.Log;
using AVS.Util;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt.Variables
{

    internal readonly struct FloatVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Float;


        public float Value { get; }
        public string Name { get; }

        public FloatVariable(Material m, string n)
        {
            Value = m.GetFloat(n);
            Name = n;
        }

        public void SetTo(RootModController rmc, Material m, MaterialLog logConfig, string? materialName)
        {
            using var log = SmartLog.LazyForAVS(rmc);
            try
            {
                var old = m.GetFloat(Name);
                if (old == Value)
                    return;
                logConfig.LogMaterialVariableSet(log, Type, Name, old, Value, m, materialName);
                m.SetFloat(Name, Value);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to set {Type} {Name} ({Value.ToString(CultureInfo.InvariantCulture)}) on {materialName ?? m.NiceName()}",
                    ex);
            }
        }
    }

}
