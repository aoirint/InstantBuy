using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

using System;
using System.Security.Cryptography;
using Unity.Netcode;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace InstantBuyOnlyCompany
{
    /// <summary>
    /// 插件加载
    /// </summary>
    [BepInPlugin(modGUID, modName, modVersion)]
    public class InstantBuyOnlyCompany : BaseUnityPlugin
    {
        private const string modGUID = "com.aoirint.InstantBuyOnlyCompany";
        private const string modName = "InstantBuyOnlyCompany";
        private const string modVersion = "0.1.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public ConfigEntry<float> offset;
        public ConfigEntry<string> ignored_item;

        public static InstantBuyOnlyCompany Instance;


        // 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            offset = Config.Bind<float>("InstantBuyOnlyCompany Config",
                                        "offset",
                                        0.2f,
                                        "Controls the offset of where purchased items are generated.");

            ignored_item = Config.Bind<string>("InstantBuyOnlyCompany Config",
                                        "ignored_item",
                                        "-1,",
                                        "Numbers are separated by commas, e.g. -1,0,1,2    -1 is used as a placeholder, please go to the mod introduction page in the ThunderStore to check which number corresponds to which item.");

            harmony.PatchAll();
            Logger.LogInfo($"Plugin {modGUID} {modVersion} is loaded!");

            
        }

        /// <summary>
        /// 瞬间生成购买物品 （客机仍然会保留买物品的数量）
        /// </summary>
        [HarmonyPatch(typeof(Terminal))]
        internal class Terminal_Patch
        {
            private static List<int> instantItems;

            static bool IsInstantBuyAllowed() {
                var roundManager = RoundManager.Instance;
                if (roundManager == null) {
                    return false;
                }

                var startOfRound = StartOfRound.Instance;
                if (startOfRound == null) {
                    return false;
                }

                // Current selected level in orbit / Current landed level
                var currentLevel = roundManager.currentLevel;
                if (currentLevel == null) {
                    return false;
                }

                var isCompanySelectedOrLanded = currentLevel.sceneName == "CompanyBuilding";
                var inShipPhase = startOfRound.inShipPhase;

                // Only allow in Company and not in ship phase (orbit)
                return isCompanySelectedOrLanded && !inShipPhase;
            }

            [HarmonyPatch("SyncGroupCreditsClientRpc")]
            [HarmonyPrefix]
            public static void Prefix(Terminal __instance, int newGroupCredits, ref int numItemsInShip)
            {
                if (StartOfRound.Instance.localPlayerController==null) return;
                NetworkManager networkManager = StartOfRound.Instance.localPlayerController.NetworkManager;
                if (!networkManager.IsServer) return;


                List<int> boughtItems = __instance.orderedItemsFromTerminal;
                List<int> ignoredItem_list = InstantBuyOnlyCompany.Instance.ignored_item.Value.Trim(',').Split(',').Select(int.Parse).ToList();
                if (! IsInstantBuyAllowed()) {
                    // Disable instant buy (Ignore all items)
                    ignoredItem_list = boughtItems.ToList();
                }
                instantItems = boughtItems.Where(item => !ignoredItem_list.Contains(item)).ToList();

                // 同步在途物品数量
                numItemsInShip = __instance.orderedItemsFromTerminal.Count - instantItems.Count;

                // Logger.LogInfo("钱 瞬间购买列表物品数:" + instantItems.Count);
            }

            [HarmonyPatch("SyncGroupCreditsClientRpc")]
            [HarmonyPostfix]
            public static void Postfix(Terminal __instance, int newGroupCredits, int numItemsInShip)
            {
                if (StartOfRound.Instance.localPlayerController == null) return;
                NetworkManager networkManager = StartOfRound.Instance.localPlayerController.NetworkManager;
                if (!networkManager.IsServer) return;


                List<int> boughtItems = __instance.orderedItemsFromTerminal;
                List<int> ignoredItem_list = InstantBuyOnlyCompany.Instance.ignored_item.Value.Trim(',').Split(',').Select(int.Parse).ToList();
                if (! IsInstantBuyAllowed()) {
                    // Disable instant buy (Ignore all items)
                    ignoredItem_list = boughtItems.ToList();
                }
                instantItems = boughtItems.Where(item => !ignoredItem_list.Contains(item)).ToList();


                // 生成物品

                Vector3 spawn_pos = StartOfRound.Instance.insideShipPositions[10].position;
                spawn_pos.z += 1.5f;

                foreach (int itemIndex in instantItems)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.buyableItemsList[itemIndex].spawnPrefab,
                        new Vector3(spawn_pos.x + UnityEngine.Random.Range(-InstantBuyOnlyCompany.Instance.offset.Value, InstantBuyOnlyCompany.Instance.offset.Value), spawn_pos.y,
                        spawn_pos.z + UnityEngine.Random.Range(-InstantBuyOnlyCompany.Instance.offset.Value, InstantBuyOnlyCompany.Instance.offset.Value)), Quaternion.identity, StartOfRound.Instance.propsContainer);
                    gameObject.GetComponent<GrabbableObject>().fallTime = 0f;
                    gameObject.GetComponent<GrabbableObject>().isInShipRoom = true;
                    gameObject.GetComponent<GrabbableObject>().transform.parent = GameObject.Find("/Environment/HangarShip").transform;
                    gameObject.GetComponent<NetworkObject>().Spawn(false);
                    // InstantBuyOnlyCompany.Logger.LogInfo("已完成实例化: " + gameObject.GetComponent<GrabbableObject>().itemProperties.itemName);
                }

                __instance.orderedItemsFromTerminal = boughtItems.Where(item => ignoredItem_list.Contains(item)).ToList();
                // InstantBuyOnlyCompany.Logger.LogInfo("正常退出");
            }
        }


        /*[HarmonyPatch(typeof(Terminal))]
        internal class Terminal_Patch
        {
            private static List<int> instantItems;


            [HarmonyPatch("BuyItemsServerRpc")]
            [HarmonyPrefix]
            public static void Prefix(Terminal __instance, ref int[] boughtItems, int newGroupCredits, ref int numItemsInShip)
            {
                Logger.LogInfo("进入了");

                // 如果非服务器，则不要管
                if (__instance != StartOfRound.Instance.localPlayerController) return;
                var protectedInternalField = typeof(PlayerControllerB).GetField("__rpc_exec_stage", BindingFlags.Instance | BindingFlags.NonPublic);
                int value = (int)protectedInternalField.GetValue(__instance);
                if (value != 1) return;

                Logger.LogInfo("我是server");

                // 过滤出需要瞬间生成的物品类
                List<int> ignoredItem_list = InstantBuyOnlyCompany.Instance.ignored_item.Value.Trim(',').Split(',').Select(int.Parse).ToList();
                instantItems = boughtItems.Where(item => !ignoredItem_list.Contains(item)).ToList();

                // 修改同步给客机的在途物品数量
                numItemsInShip = __instance.orderedItemsFromTerminal.Count + boughtItems.Length - instantItems.Count;

                // 生成需要瞬间生成的物品
                Vector3 spawn_pos = StartOfRound.Instance.insideShipPositions[10].position;
                spawn_pos.z += 1.5f;
                foreach (int itemIndex in instantItems)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.buyableItemsList[itemIndex].spawnPrefab,
                        new Vector3(spawn_pos.x + UnityEngine.Random.Range(-InstantBuyOnlyCompany.Instance.offset.Value, InstantBuyOnlyCompany.Instance.offset.Value), spawn_pos.y,
                        spawn_pos.z + UnityEngine.Random.Range(-InstantBuyOnlyCompany.Instance.offset.Value, InstantBuyOnlyCompany.Instance.offset.Value)), Quaternion.identity, StartOfRound.Instance.propsContainer);
                    gameObject.GetComponent<GrabbableObject>().fallTime = 0f;
                    gameObject.GetComponent<GrabbableObject>().isInShipRoom = true;
                    gameObject.GetComponent<GrabbableObject>().transform.parent = GameObject.Find("/Environment/HangarShip").transform;
                    gameObject.GetComponent<NetworkObject>().Spawn(false);
                }

                // 更新一下买的东西的列表，改成剩下没有瞬间生成的物品列表
                boughtItems = (int[]) boughtItems.Where(item => ignoredItem_list.Contains(item));
            }

            
        }*/
    }
}
