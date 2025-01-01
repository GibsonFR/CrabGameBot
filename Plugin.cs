global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using UnityEngine;
global using System;
global using System.IO;
global using UnhollowerRuntimeLib;
global using System.Collections.Generic;
global using System.Globalization;
global using System.Linq;
global using UnityEngine.UI;
global using System.Collections;
global using BepInEx.IL2CPP.Utils.Collections;
global using static GibsonBot.Variables;
global using static GibsonBot.InputState;
global using static GibsonBot.Utility;
global using static GibsonBot.GameData;




namespace GibsonBot
{
    [BepInPlugin("PlaceHereGUID", "GibsonTemplateMod", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();
            ClassInjector.RegisterTypeInIl2Cpp<TASManager>();
            ClassInjector.RegisterTypeInIl2Cpp<InputManager>();
            ClassInjector.RegisterTypeInIl2Cpp<SnowballsManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PathFindingManager>();
            ClassInjector.RegisterTypeInIl2Cpp<NodeUsageVisualizer>();

            // GameMode
            ClassInjector.RegisterTypeInIl2Cpp<SnowBrawlManager>();
            ClassInjector.RegisterTypeInIl2Cpp<DodgeBallManager>();
            ClassInjector.RegisterTypeInIl2Cpp<TileDriveManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BlockDropManager>();
            ClassInjector.RegisterTypeInIl2Cpp<DeathFromAboveManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BustlingButtonsManager>();
            ClassInjector.RegisterTypeInIl2Cpp<TheFloorIsLavaManager>();
            ClassInjector.RegisterTypeInIl2Cpp<TagManager>();
            ClassInjector.RegisterTypeInIl2Cpp<RedLightGreenLightManager>();
            ClassInjector.RegisterTypeInIl2Cpp<SteppingStonesManager>();
            ClassInjector.RegisterTypeInIl2Cpp<RaceManager>();
            ClassInjector.RegisterTypeInIl2Cpp<LightOutManager>();
            ClassInjector.RegisterTypeInIl2Cpp<KingOfTheHillManager>();
            ClassInjector.RegisterTypeInIl2Cpp<CrabFightManager>();
            ClassInjector.RegisterTypeInIl2Cpp<HideAndSeekManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BombTagManager>();
            ClassInjector.RegisterTypeInIl2Cpp<HatKingManager>();


            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony harmony = new("gibson.bot");
            harmony.PatchAll(typeof(InputPatchs));
            harmony.PatchAll(typeof(SnowballsPatch));
            harmony.PatchAll(typeof(CrabFightPatchs));

            CreateFolder(mainFolderPath, logFilePath);
            CreateFolder(configFolderPath, logFilePath);
            CreateFile(logFilePath, logFilePath);
            ResetFile(logFilePath, logFilePath);

            CreateFile(botWhiteListFilePath, logFilePath);

            DisplayDataCallbacks = [];

            DisplayFunctions.CreateDisplayFile(displayFilePath);
            DisplayFunctions.LoadMenuLayout();
            DisplayFunctions.RegisterDefaultCallbacks();

            SetConfigFile(configFilePath);

            CreateTechsFolders(techFolderPath, mapNameFilePath);
            CreateTechsFolders(tagBotTechFolderPath, mapNameFilePath);
            CreateTechsFolders(KOTHbotTechFolderPath, mapNameFilePath);
            CreateTechsFolders(raceBotTechFolderPath, mapNameFilePath);
        }

        public class Basics : MonoBehaviour
        {
            public Text text;
            float elapsedServerUpdate, elapsedClientUpdate, elapsedMenuUpdate;
            bool init, isReady;
            void Update()
            {
                float elapsedTime = Time.deltaTime;
                elapsedServerUpdate += elapsedTime;
                elapsedClientUpdate += elapsedTime;
                elapsedMenuUpdate += elapsedTime;

                if (!init)
                {
                    Utility.ReadConfigFile(configFilePath);
                    UIFunctions.SkipCinematicCamera();
                    init = true;
                }

                if (elapsedServerUpdate > 1f)
                {
                    BasicUpdateServer();
                    elapsedServerUpdate = 0f;
                }

                if (elapsedClientUpdate > 1f)
                {
                    BasicUpdateClient();
                    elapsedClientUpdate = 0f;
                }

                if (Input.GetKeyDown(menuKey))
                {
                    menuTrigger = !menuTrigger;
                }

                if (elapsedMenuUpdate >= displayFrameRate)
                {
                    text.text = menuTrigger ? DisplayFunctions.FormatLayout() : "";
                    elapsedMenuUpdate = 0f; // reset the timer
                }

                if (isClientBot && clientBody != null && modeId == 0 && !isReady)
                {
                    PressLobbyButton();
                    isReady = true;
                }
            }

            //Ceci mets a jour les données relative au Client(fonctionne uniquement si le client a un Rigidbody (en vie))
            void BasicUpdateClient()
            {
                clientBody = ClientData.GetClientBody();
                if (clientBody == null) return;

                clientObject = ClientData.GetClientObject();
                clientMovement = ClientData.GetClientMovement();
                clientInventory = ClientData.GetClientInventory();
                clientStatus = PlayerStatus.Instance;
            }

            //Ceci mets a jour les données relative au Server
            void BasicUpdateServer()
            {
                mapId = GetMapId();
                modeId = GetModeId();
            }
        }

        //Plusieurs hook plus ou moins utile...

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwake(SteamManager __instance)
        {
            if (steamManagerAwakeCheck) return;
            clientId = (ulong)__instance.field_Private_CSteamID_0;

            Utility.ReadBotWhiteList(botWhiteList, botWhiteListFilePath);
            isClientBot = !botWhiteList.Contains(clientId);
            if (isClientBot)
            {
                Application.targetFrameRate = exeFrameRate;
            }
            steamManagerAwakeCheck = true;
        }

        [HarmonyPatch(typeof(DamageVignette), nameof(DamageVignette.Damage))]
        [HarmonyPrefix]
        public static void OnDamage()
        {
            AutoTechFunctions.HandleReplayEnd();
        }


        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject plugin = new GameObject();
            Text text = plugin.AddComponent<Text>();
            text.font = (Font)Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;
            text.raycastTarget = false;
            text.fontSize = 18;

            Basics basics = plugin.AddComponent<Basics>();
            basics.text = text;

            plugin.AddComponent<InputManager>();
            plugin.AddComponent<TASManager>();
            plugin.AddComponent<SnowballsManager>();
            plugin.AddComponent<PathFindingManager>();
            plugin.AddComponent<NodeUsageVisualizer>();

            // GameMode
            plugin.AddComponent<SnowBrawlManager>();
            plugin.AddComponent<DodgeBallManager>();
            plugin.AddComponent<TileDriveManager>();
            plugin.AddComponent<BlockDropManager>();
            plugin.AddComponent<DeathFromAboveManager>();
            plugin.AddComponent<BustlingButtonsManager>();
            plugin.AddComponent<TheFloorIsLavaManager>();
            plugin.AddComponent<TagManager>();
            plugin.AddComponent<RedLightGreenLightManager>();
            plugin.AddComponent<SteppingStonesManager>();
            plugin.AddComponent<RaceManager>();
            plugin.AddComponent<LightOutManager>();
            plugin.AddComponent<KingOfTheHillManager>();
            plugin.AddComponent<CrabFightManager>();
            plugin.AddComponent<HideAndSeekManager>();
            plugin.AddComponent<BombTagManager>();
            plugin.AddComponent<HatKingManager>();

            plugin.transform.SetParent(__instance.transform);
            plugin.transform.localPosition = new(plugin.transform.localPosition.x, -plugin.transform.localPosition.y, plugin.transform.localPosition.z);
            RectTransform rt = plugin.GetComponent<RectTransform>();
            rt.pivot = new(0, 1);
            rt.sizeDelta = new(1920, 1080);
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}