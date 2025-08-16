using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

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
    /// Converts a CodeInstruction instance to a descriptive string representation.
    /// </summary>
    /// <param name="i">The CodeInstruction instance to be converted.</param>
    /// <returns>A string representation of the CodeInstruction, including its opcode, operand, and associated labels.</returns>
    public static string ToStr(this CodeInstruction i)
    {
        return
            $"Instruction: opCode='{i.opcode}', operand={i.operand.ObjectToStr()}, labels:{string.Join(", ", i.labels.Select(x => x.ToStr()))}";
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
            case CodeInstruction i:
                return i.ToStr();
            default:
                return $"'{o}'";
        }
    }
}