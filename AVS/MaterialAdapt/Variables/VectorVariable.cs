using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt.Variables
{

    internal readonly struct VectorVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Vector;


        public Vector4 Value { get; }
        public string Name { get; }

        public VectorVariable(Material m, string n)
        {
            Value = m.GetVector(n);
            Name = n;
        }

        public void SetTo(RootModController rmc, Material m, MaterialLog logConfig, string? materialName)
        {
            using var log = SmartLog.LazyForAVS(rmc);
            try
            {
                var old = m.GetVector(Name);
                if (old == Value)
                    return;
                logConfig.LogMaterialVariableSet(log, Type, Name, old, Value, m, materialName);
                m.SetVector(Name, Value);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to set {Type} {Name} ({Value}) on {materialName ?? m.NiceName()}", ex);
            }
        }
    }

}
