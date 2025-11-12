using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HsBgAI.Tools;

namespace HsBgAI.Patchers
{
    public class PatchManager
    {
        public static List<Harmony> AllHarmony = new List<Harmony>();
        public static List<string> AllHarmonyName = new List<string>();

        /// <summary>
        /// 加载补丁
        /// </summary>
        /// <param name="loadType"></param>
        public static void LoadPatch(Type loadType)
        {
            try
            {
                Harmony harmony;
                int harmonyCount;
                harmony = Harmony.CreateAndPatchAll(loadType);
                harmonyCount = harmony.GetPatchedMethods().Count();
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"{loadType.Name} => Patched {harmonyCount} methods");
                AllHarmony.Add(harmony);
                AllHarmonyName.Add(loadType.Name);
            }
            catch (Exception ex)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"{loadType.Name} => {ex.Message} \n{ex.InnerException}");
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"{PluginInfo.PLUGIN_GUID} patch failed!");
                Thread.Sleep(11451);
                Utils.Quit(114514);
            }
        }

        /// <summary>
        /// 卸载补丁
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool UnPatch(string name)
        {
            for (int i = 0; i < AllHarmonyName.Count; i++)
            {
                if (AllHarmonyName[i] == name)
                {
                    AllHarmony[i].UnpatchSelf();
                    AllHarmonyName.Remove(AllHarmonyName[i]);
                    AllHarmony.Remove(AllHarmony[i]);
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"Unpatched {name}!");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 加载所有补丁
        /// </summary>
        public static void PatchAll()
        {
            LoadPatch(typeof(MainHubPatcher));
            LoadPatch(typeof(RewardXpPatcher));
        }

        /// <summary>
        /// 卸载所有补丁
        /// </summary>
        public static void UnPatchAll()
        {
            for (int i = 0; i < AllHarmony.Count; i++)
            {
                AllHarmony[i].UnpatchSelf();
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"Unpatched {AllHarmonyName[i]}!");
            }
            AllHarmony.Clear();
            AllHarmonyName.Clear();
        }
    }
}