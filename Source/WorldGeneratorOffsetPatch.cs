using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProceduralRoads;

/// <summary>
/// Transpiler patch that extends the offset range used in WorldGenerator.
/// 
/// Vanilla uses Random.Range(-10000, 10000) for m_offset0-4, limiting worlds
/// to a 40k×40k region of the infinite Perlin noise space.
/// 
/// This patch expands the range, allowing access to terrain configurations
/// that no vanilla seed can produce.
/// 
/// Can be extracted to a standalone mod.
/// </summary>
[HarmonyPatch(typeof(WorldGenerator), MethodType.Constructor, typeof(World))]
public static class WorldGeneratorOffsetPatch
{
    /// <summary>
    /// Enable/disable this patch. Set to false to use vanilla behavior.
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// The extended offset range. Vanilla is 10000.
    /// Setting to 50000 gives 5× the range (120k×120k accessible region).
    /// </summary>
    public static int OffsetRange = 50000;

    /// <summary>
    /// Transpiler that modifies the Random.Range(-10000, 10000) calls
    /// to use the extended OffsetRange instead.
    /// </summary>
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        int patchCount = 0;

        // We're looking for the pattern:
        //   ldc.i4 -10000
        //   ldc.i4 10000
        //   call UnityEngine.Random.Range(int, int)
        //
        // And replacing the constants with our extended range.

        var randomRangeMethod = AccessTools.Method(
            typeof(UnityEngine.Random), 
            nameof(UnityEngine.Random.Range), 
            new[] { typeof(int), typeof(int) });

        for (int i = 0; i < codes.Count - 2; i++)
        {
            // Check for ldc.i4 -10000
            if (!IsLoadConstant(codes[i], -10000))
                continue;

            // Check for ldc.i4 10000
            if (!IsLoadConstant(codes[i + 1], 10000))
                continue;

            // Check for call to Random.Range(int, int)
            if (codes[i + 2].opcode != OpCodes.Call)
                continue;

            if (codes[i + 2].operand is not MethodInfo method || method != randomRangeMethod)
                continue;

            // Found the pattern - replace with dynamic values

            // Replace first constant with: Enabled ? -OffsetRange : -10000
            codes[i] = new CodeInstruction(OpCodes.Call, 
                AccessTools.Method(typeof(WorldGeneratorOffsetPatch), nameof(GetMinOffset)));

            // Replace second constant with: Enabled ? OffsetRange : 10000  
            codes[i + 1] = new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(WorldGeneratorOffsetPatch), nameof(GetMaxOffset)));

            patchCount++;
        }

        if (patchCount > 0)
        {
            ProceduralRoadsPlugin.ProceduralRoadsLogger.LogDebug(
                $"WorldGeneratorOffsetPatch: Patched {patchCount} Random.Range calls");
        }

        return codes;
    }

    /// <summary>
    /// Returns the minimum offset value based on whether the patch is enabled.
    /// </summary>
    public static int GetMinOffset()
    {
        return Enabled ? -OffsetRange : -10000;
    }

    /// <summary>
    /// Returns the maximum offset value based on whether the patch is enabled.
    /// </summary>
    public static int GetMaxOffset()
    {
        return Enabled ? OffsetRange : 10000;
    }

    /// <summary>
    /// Postfix to log the actual offset values used.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix(WorldGenerator __instance)
    {
        // Use reflection to read the private offset fields
        var t = typeof(WorldGenerator);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        
        float offset0 = (float)t.GetField("m_offset0", flags)!.GetValue(__instance)!;
        float offset1 = (float)t.GetField("m_offset1", flags)!.GetValue(__instance)!;
        float offset2 = (float)t.GetField("m_offset2", flags)!.GetValue(__instance)!;
        float offset3 = (float)t.GetField("m_offset3", flags)!.GetValue(__instance)!;
        float offset4 = (float)t.GetField("m_offset4", flags)!.GetValue(__instance)!;
        
        ProceduralRoadsPlugin.ProceduralRoadsLogger.LogInfo(
            $"WorldGenerator offsets: [{offset0:F0}, {offset1:F0}, {offset2:F0}, {offset3:F0}, {offset4:F0}] " +
            $"(range: {(Enabled ? OffsetRange : 10000)})");
    }

    /// <summary>
    /// Helper to check if an instruction loads a specific integer constant.
    /// Handles various ldc.i4 variants.
    /// </summary>
    private static bool IsLoadConstant(CodeInstruction instruction, int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4)
            return instruction.operand is int i && i == value;

        if (instruction.opcode == OpCodes.Ldc_I4_S)
            return instruction.operand is sbyte s && s == value;

        // Handle ldc.i4.0 through ldc.i4.8 and ldc.i4.m1
        if (instruction.opcode == OpCodes.Ldc_I4_M1 && value == -1) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_0 && value == 0) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_1 && value == 1) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_2 && value == 2) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_3 && value == 3) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_4 && value == 4) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_5 && value == 5) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_6 && value == 6) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_7 && value == 7) return true;
        if (instruction.opcode == OpCodes.Ldc_I4_8 && value == 8) return true;

        return false;
    }
}
