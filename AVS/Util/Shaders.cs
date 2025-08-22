using System.Collections.Generic;
using UnityEngine;

namespace AVS.Util;

/// <summary>
/// Constants and helper methods related to shaders.
/// </summary>
public static class Shaders
{
    /// <summary>
    /// Name of the (only) shader used for all vehicles.
    /// </summary>
    public static string MainShader { get; } = "MarmosetUBER";

    /// <summary>
    /// Shader keyword used if the material emits light
    /// </summary>
    public static string EmissionKeyword { get; } = "MARMO_EMISSION";

    /// <summary>
    /// Shader keyword used if the material has a specular map and accesses the environment map.
    /// </summary>
    public static string SpecmapKeyword { get; } = "MARMO_SPECMAP";

    /// <summary>
    /// Shader property that controls glow strength.
    /// </summary>
    public static string GlowField { get; } = "_GlowStrength";

    /// <summary>
    /// Shader property that controls glow strength at night.
    /// </summary>
    public static string GlowNightField { get; } = "_GlowStrengthNight";

    /// <summary>
    /// Shader property that controls emission strength.
    /// </summary>
    public static string EmissionField { get; } = "_EmissionLM";

    /// <summary>
    /// Shader property that controls emission strength as night.
    /// </summary>
    public static string EmissionNightField { get; } = "_EmissionLMNight";

    /// <summary>
    /// Shader property that controls the specular intensity.
    /// </summary>
    public static string SpecIntField { get; } = "_SpecInt";

    /// <summary>
    /// Shader property that controls color.
    /// </summary>
    public static string ColorField { get; } = "_Color";

    /// <summary>
    /// Shader property that controls the glow color.
    /// </summary>
    public static string GlowColorField { get; } = "_GlowColor";

    /// <summary>
    /// Finds and returns the main shader used for vehicles.
    /// </summary>
    /// <remarks>If the shader cannot be located, an error message is logged to the console.</remarks>
    /// <returns>The <see cref="Shader"/> instance representing the main vehicle shader,  or <see langword="null"/> if the
    /// shader cannot be found.</returns>
    public static Shader? FindMainShader()
    {
        // Find the main shader used for vehicles.
        var shader = Shader.Find(MainShader);
        if (shader.IsNull())
            Debug.LogError($"Shader '{MainShader}' not found.");
        return shader;
    }

    /// <summary>
    /// Logs the properties of the main shader to the debug console.
    /// </summary>
    /// <remarks>This method retrieves the main shader, iterates through its properties, and logs each
    /// property's name and type to the debug console. It is intended for debugging purposes to inspect the shader's
    /// properties.</remarks>
    public static void LogMainShaderPropertiesToDebug()
    {
        var shader = FindMainShader();
        if (shader.IsNull())
            return;
        for (var i = 0; i < shader.GetPropertyCount(); i++)
        {
            var propertyName = shader.GetPropertyName(i);
            Debug.Log($"Property {i}: {propertyName}, Type: {shader.GetPropertyType(i)}");
        }
    }


    /// <summary>
    /// Logs the names of all unique shaders currently in use across all loaded materials in the application.
    /// </summary>
    /// <remarks>This method scans all materials currently loaded in memory and identifies the shaders
    /// they reference. It then logs the names of these shaders to the Unity console. This can be useful for
    /// debugging or analyzing which shaders are actively being used in the application.</remarks>
    public static void LogAllShadersInUseAnywhere()
    {
        var shaderNames = new HashSet<string>();

        // Find all materials currently loaded in the game.
        var materials = Resources.FindObjectsOfTypeAll<Material>();

        foreach (var material in materials)
            if (material.shader.IsNotNull())
                // Add the shader name to the set to ensure uniqueness.
                shaderNames.Add(material.shader.name);

        // Now you have a unique list of shader names in use.
        foreach (var shaderName in shaderNames)
            Debug.Log("Shader in use: " + shaderName);
    }


    /// <summary>
    /// Applies the main shader to all materials of <see cref="MeshRenderer"/> components  in the specified <see
    /// cref="GameObject"/> and its children.
    /// </summary>
    /// <remarks>This method recursively traverses the hierarchy of the specified <see
    /// cref="GameObject"/>  and applies the main shader to all materials of any <see cref="MeshRenderer"/>
    /// components found. Hidden or inactive objects are also included in the traversal.</remarks>
    /// <param name="go">The root <see cref="GameObject"/> whose child <see cref="MeshRenderer"/> components will have their
    /// materials updated.</param>
    public static void ApplyMainShaderRecursively(GameObject go)
    {
        var shader = FindMainShader();
        go.GetComponentsInChildren<MeshRenderer>(true).ForEach(x => x.materials.ForEach(y => y.shader = shader));
    }


    /// <summary>
    /// Enables emission for the specified material using predefined day and night emission values.
    /// </summary>
    /// <remarks>This method configures the material to use emission under the "marmosetuber" shader. 
    /// It sets the emission intensity for both day and night modes and enables the necessary shader keywords. Note
    /// that not all materials may behave as expected with this configuration, particularly if they do not  require
    /// a specular map, which could result in unintended visual effects such as increased brightness or
    /// shininess.</remarks>
    /// <param name="mat">The material for which emission will be enabled. Cannot be <see langword="null"/>.</param>
    /// <param name="dayAmount">The emission intensity to apply during the day. The default value is 1.0.</param>
    /// <param name="nightAmount">The emission intensity to apply during the night. The default value is 1.0.</param>
    public static void EnableSimpleEmission(Material mat, float dayAmount = 1f, float nightAmount = 1f)
    {
        // This is the minumum requirement for emission under the marmosetuber shader.
        // No guarantees this will work well, but it's a good starting place.
        // For example, not all materials will want to use a specular map. In that case,
        // it can make a material look brighter, shinier, or more luminescent than it should be.
        mat.EnableKeyword(EmissionKeyword);
        mat.EnableKeyword(SpecmapKeyword);
        mat.SetFloat(GlowField, 0);
        mat.SetFloat(GlowNightField, 0);
        mat.SetFloat(EmissionField, dayAmount);
        mat.SetFloat(EmissionNightField, nightAmount);
    }
}