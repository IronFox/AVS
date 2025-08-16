using System.Reflection.Emit;

namespace AVS.Util;

/// <summary>
/// Static helpers for dealing with code instructions
/// </summary>
public static class CodeInstructionExtensions
{
    /// <summary>
    /// Converts a label to string
    /// </summary>
    public static string ToStr(this Label label)
    {
        return $"Label{{{label.GetHashCode()}}}";
    }
    
    /// <summary>
    /// Converts instruction artifacts to readable strings
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static string ObjectToStr(this object? o)
    {
        switch (o)
        {
            case null:
                return "<null>";
            case Label label:
                return label.ToStr();
            default:
                return $"'{o}'";
        }
    }    
}