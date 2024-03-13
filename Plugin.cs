//Using (ici on importe des bibliothèques utiles)
global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using UnityEngine;
global using System;
global using System.IO;
global using UnhollowerRuntimeLib;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO.Compression;
global using System.Net.Http;
global using System.Threading.Tasks;
global using System.Linq;
global using UnityEngine.UI;

namespace GibsonTemplateMod
{
    [BepInPlugin("PlaceHereGUID", "GibsonTemplateMod", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();

            //Ajouter ici toute vos class MonoBehaviour pour quelle soit active dans le jeu
            //Format: ClassInjector.RegisterTypeInIl2Cpp<NomDeLaClass>(); 
            ClassInjector.RegisterTypeInIl2Cpp<AutoRLGL>();
            ClassInjector.RegisterTypeInIl2Cpp<TouchingGround>();

            Harmony.CreateAndPatchAll(typeof(Plugin));

            //Ici on créer un fichier log.txt situé dans le dossier GibsonTemplateMod
            Utility.CreateFolder(Variables.mainFolderPath, Variables.logFilePath);
            Utility.CreateFile(Variables.logFilePath, Variables.logFilePath);
            Utility.ResetFile(Variables.logFilePath, Variables.logFilePath);

            Utility.CreateFile(Variables.botWhiteListFilePath, Variables.logFilePath);

            //Ici on load tout ce  qui  est relatif a l'affichage
            Variables.DisplayDataCallbacks = new System.Collections.Generic.Dictionary<string, System.Func<string>>();

            DisplayFunctions.CreateDisplayFile(Variables.displayFilePath);
            DisplayFunctions.LoadMenuLayout();
            DisplayFunctions.RegisterDefaultCallbacks();

            Utility.SetConfigFile(Variables.configFilePath);
        }

        //Cette class permet de récupérer des variables de base ne pas toucher sauf pour rajouter d'autres variables a Update
        public class Basics : MonoBehaviour
        {
            public Text text;
            float elapsedServerUpdate, elapsedClientUpdate, elapsedMenuUpdate;
            bool init, initTouchingGround, isReady;
            void Update()
            {
                float elapsedTime = Time.deltaTime;
                elapsedServerUpdate += elapsedTime;
                elapsedClientUpdate += elapsedTime;
                elapsedMenuUpdate += elapsedTime;

                if (!init)
                {
                    Utility.ReadConfigFile(Variables.configFilePath);
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

                if (Input.GetKeyDown(Variables.menuKey))
                {
                    Variables.menuTrigger = !Variables.menuTrigger;
                }

                if (elapsedMenuUpdate >= Variables.displayFrameRate)
                {
                    text.text = Variables.menuTrigger ? DisplayFunctions.FormatLayout() : "";
                    elapsedMenuUpdate = 0f; // reset the timer
                }

                if (Variables.clientObject != null && !initTouchingGround)
                {
                    Variables.clientObject.AddComponent<TouchingGround>();
                    initTouchingGround = true;
                }

                if (Variables.isClientBot && Variables.clientBody != null && Variables.modeId == 0 && !isReady)
                {
                    Utility.PressLobbyButton();
                    isReady = true;
                }
            }

            //Ceci mets a jour les données relative au Client(fonctionne uniquement si le client a un Rigidbody (en vie))
            void BasicUpdateClient()
            {
                Variables.clientBody = ClientData.GetClientBody();
                if (Variables.clientBody == null) return;

                Variables.clientObject = ClientData.GetClientObject();
                Variables.clientMovement = ClientData.GetClientMovement();
                Variables.clientInventory = ClientData.GetClientInventory();
                Variables.clientStatus = PlayerStatus.Instance;
            }

            //Ceci mets a jour les données relative au Server
            void BasicUpdateServer()
            {
                Variables.chatBoxInstance = ChatBox.Instance;
                Variables.gameManager = GameData.GetGameManager();
                Variables.lobbyManager = GameData.GetLobbyManager();
                Variables.steamManager = GameData.GetSteamManager();
                Variables.mapId = GameData.GetMapId();
                Variables.modeId = GameData.GetModeId();
                Variables.gameState = GameData.GetGameState();
                Variables.activePlayers = Variables.gameManager.activePlayers;
                Variables.playersList = Variables.gameManager.activePlayers.entries.ToList();
                if (Variables.gameState != Variables.lastGameState)
                    Variables.lastGameState = Variables.gameState;
            }
        }

        public class TouchingGround : MonoBehaviour
        {
            void OnCollisionStay(Collision collision)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Object"))
                {
                    Variables.grounded = true;
                }
            }
            void OnCollisionExit(Collision collision)
            {
                Variables.grounded = false;
            }
        }

        public class AutoRLGL : MonoBehaviour
        {
            Vector3 safePos = new Vector3(0, 0, 145), closestPlayerPos, playerPos;
            float distanceToSafePos, distanceToClosestPlayer;
            PlayerManager closestPlayer;
            bool isMad, isMadSet;
            float elapsed;
            void Update()
            {
                elapsed += Time.deltaTime;

                if (Variables.gameState == "Playing" && Variables.modeId == 1 && Variables.isClientBot)
                {
                    playerPos = Variables.clientBody.transform.position;
                    closestPlayer = BotFunctions.FindClosestPlayer();

                    if (closestPlayer != null) 
                    {
                        closestPlayerPos = closestPlayer.transform.position;
                    }

                    distanceToSafePos = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(safePos.x, safePos.z));
                    distanceToClosestPlayer = Vector3.Distance(closestPlayerPos, playerPos);

                    if (IsStatueScanning() && playerPos.z < 128)
                    {
                        if (elapsed > 0.5f)
                            BotFunctions.Combat(closestPlayerPos, 1f);

                        isMadSet = false;
                        isMad = false;
                    }
                    else
                    {
                        elapsed = 0f;
                        if (!isMadSet && closestPlayer != null)
                        {
                            isMadSet = true;
                            System.Random random = new();
                            if (random.NextDouble() < 0.5f)
                            {
                                isMad = true;
                            }
                        }

                        if (isMad && closestPlayer.transform.position.z + 5 > playerPos.z && distanceToClosestPlayer > 2)
                        {
                            BotFunctions.MoveBotToTarget(closestPlayerPos, 10f);
                        }
                        else if (distanceToSafePos > 5)
                            BotFunctions.MoveBotToTarget(safePos, 9f);

                        BotFunctions.LookAtTarget(closestPlayerPos);
                    }
                }
            }

            private bool IsStatueScanning()
            {
                GameObject statue = GameObject.Find("Statue");
                if (statue != null)
                {
                    return (statue.GetComponent<MonoBehaviourPublicQuSiQuTrSiheQuLawhQuUnique>().field_Private_Quaternion_2 == Quaternion.identity);
                }
                return false;   
            }
        }

        //Plusieurs hook plus ou moins utile...

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdate(SteamManager __instance)
        {
            //Mets a jour le steamId du client des le lancement du jeu
            if (Variables.clientIdSafe == 0)
            {
                Variables.clientId = (ulong)__instance.field_Private_CSteamID_0;
                Variables.clientIdSafe = Variables.clientId;

                Utility.ReadBotWhiteList(Variables.botWhiteList, Variables.botWhiteListFilePath);

                Variables.isClientBot = !Variables.botWhiteList.Contains(Variables.clientId);
            }

            if (Variables.isClientBot)
            {
                Application.targetFrameRate = Variables.exeFrameRate;
            }
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Update))]
        [HarmonyPostfix]
        public static void GameModeUpdate(GameMode __instance)
        {
        }

        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.GameRequestToSpawn))]
        [HarmonyPrefix]
        public static void ServerHandleGameRequestToSpawn(ulong __0)
        {
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Init))]
        [HarmonyPostfix]
        public static void GameModeInit()
        {
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckGameOver))]
        [HarmonyPrefix]
        public static void GameLoopCheckGameOver()
        {
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.StartGames))]
        [HarmonyPrefix]
        public static void GameLoopStartGames()
        {
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.NextGame))]
        [HarmonyPrefix]
        public static void GameLoopNextGame()
        {
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendWinner))]
        [HarmonyPrefix]
        public static void ServerSendSendWinner()
        {
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.RestartLobby))]
        [HarmonyPrefix]
        public static void GameLoopRestartLobby()
        {
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDied))]
        [HarmonyPostfix]
        public static void GameManagerPlayerDied(ulong __0, ulong __1)
        {
        }


        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject menuObject = new GameObject();
            Text text = menuObject.AddComponent<Text>();
            text.font = (Font)Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;
            text.raycastTarget = false;
            text.fontSize = 18;

            Basics basics = menuObject.AddComponent<Basics>();
            basics.text = text;

            //Ici aussi ajouter toute vos class MonoBehaviour pour quelle soit active dans le jeu
            //Format: NomDeLaClass nomDeLaClass = menuObject.AddComponent<NomDeLaClass>();
            AutoRLGL autoRLGL = menuObject.AddComponent<AutoRLGL>();
            TouchingGround touchingGround = menuObject.AddComponent<TouchingGround>();

            menuObject.transform.SetParent(__instance.transform);
            menuObject.transform.localPosition = new UnityEngine.Vector3(menuObject.transform.localPosition.x, -menuObject.transform.localPosition.y, menuObject.transform.localPosition.z);
            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.pivot = new UnityEngine.Vector2(0, 1);
            rt.sizeDelta = new UnityEngine.Vector2(1920, 1080);
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