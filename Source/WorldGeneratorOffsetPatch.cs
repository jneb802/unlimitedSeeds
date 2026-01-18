using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace unlimitedSeeds;

/// <summary>
/// Transpiler patch that extends the offset range used in WorldGenerator.
/// 
/// Vanilla uses Random.Range(-10000, 10000) for m_offset0-4, limiting worlds
/// to a 40k×40k region of the infinite Perlin noise space.
/// 
/// This patch expands the range, allowing access to terrain configurations
/// that no vanilla seed can produce.
/// </summary>
[HarmonyPatch(typeof(WorldGenerator), MethodType.Constructor, typeof(World))]
public static class WorldGeneratorOffsetPatch
{
    public static int OffsetRange = 50000;

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        int patchCount = 0;

        var randomRangeMethod = AccessTools.Method(
            typeof(UnityEngine.Random), 
            nameof(UnityEngine.Random.Range), 
            new[] { typeof(int), typeof(int) });

        for (int i = 0; i < codes.Count - 2; i++)
        {
            if (!IsLoadConstant(codes[i], -10000))
                continue;

            if (!IsLoadConstant(codes[i + 1], 10000))
                continue;

            if (codes[i + 2].opcode != OpCodes.Call)
                continue;

            if (codes[i + 2].operand is not MethodInfo method || method != randomRangeMethod)
                continue;

            codes[i] = new CodeInstruction(OpCodes.Call, 
                AccessTools.Method(typeof(WorldGeneratorOffsetPatch), nameof(GetMinOffset)));

            codes[i + 1] = new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(WorldGeneratorOffsetPatch), nameof(GetMaxOffset)));

            patchCount++;
        }

        if (patchCount > 0)
        {
            unlimitedSeedsPlugin.Log.LogDebug(
                $"WorldGeneratorOffsetPatch: Patched {patchCount} Random.Range calls");
        }

        return codes;
    }

    public static int GetMinOffset() => -OffsetRange;

    public static int GetMaxOffset() => OffsetRange;

    [HarmonyPostfix]
    public static void Postfix(WorldGenerator __instance)
    {
        var t = typeof(WorldGenerator);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        
        float offset0 = (float)t.GetField("m_offset0", flags)!.GetValue(__instance)!;
        float offset1 = (float)t.GetField("m_offset1", flags)!.GetValue(__instance)!;
        float offset2 = (float)t.GetField("m_offset2", flags)!.GetValue(__instance)!;
        float offset3 = (float)t.GetField("m_offset3", flags)!.GetValue(__instance)!;
        float offset4 = (float)t.GetField("m_offset4", flags)!.GetValue(__instance)!;
        
        unlimitedSeedsPlugin.Log.LogInfo(
            $"WorldGenerator offsets: [{offset0:F0}, {offset1:F0}, {offset2:F0}, {offset3:F0}, {offset4:F0}] " +
            $"(range: {OffsetRange})");
    }

    private static bool IsLoadConstant(CodeInstruction instruction, int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4)
            return instruction.operand is int i && i == value;

        if (instruction.opcode == OpCodes.Ldc_I4_S)
            return instruction.operand is sbyte s && s == value;

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
