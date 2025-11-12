using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HsBgAI.Tools;

namespace HsBgAI.Patchers
{
    public class MainHubPatcher
    {
        /// <summary>
        /// 显示介绍弹窗
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoginManager), "ShowIntroPopups")]
        public static void PatchShowIntroPopups()
        {
            HsBgAI.loginCompleted = true;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"ShowIntroPopups ## loginCompleted => {HsBgAI.loginCompleted}");
        }

        /// <summary>
        /// 重连到游戏
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ReconnectMgr), "StartReconnectingToGame")]
        public static void PatchStartReconnectingToGame()
        {
            HsBgAI.loginCompleted = true;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"StartReconnectingToGame ## loginCompleted => {HsBgAI.loginCompleted}");
        }

        /// <summary>
        /// 连接超时
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ReconnectMgr), "OnGameReconnectTimeout")]
        public static void PatchOnGameReconnectTimeout()
        {
            HsBgAI.loginCompleted = true;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"OnGameReconnectTimeout ## loginCompleted => {HsBgAI.loginCompleted}");
        }

        /// <summary>
        /// 炉石不可用
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetCache), "OnHearthstoneUnavailable")]
        public static void PatchOnHearthstoneUnavailable()
        {
            HsBgAI.loginCompleted = true;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"OnHearthstoneUnavailable ## loginCompleted => {HsBgAI.loginCompleted}");
        }

        /// <summary>
        /// 添加账号暂封错误
        /// </summary>
        /// <param name="parms"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Error), nameof(Error.AddFatal), new Type[] { typeof(ErrorParams) })]
        public static void PatchAddFatal(ref ErrorParams parms)
        {
            try
            {
                if (string.IsNullOrEmpty(parms?.m_message)) return;

                var msg = parms.m_message;
                if (msg.Contains("你的账号已被暂封"))
                {
                    HsBgAI.temporarilyClosed = true;
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"AddFatal ## temporarilyClosed => {HsBgAI.temporarilyClosed}");

                    Utils.SaveErrorJsonData();
                    Thread.Sleep(11451);
                    Utils.Quit(114514);
                }
            }
            catch (Exception ex)
            {
                LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
            }
        }
    }
}