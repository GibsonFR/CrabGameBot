using System.Collections.Generic;

namespace GibsonTemplateMod
{
    //Ici on stock les variables "globale" pour la lisibilité du code dans Plugin.cs 
    internal class Variables
    {
        //folder
        public static string assemblyFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"GibsonBot\";

        //file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string displayFilePath = mainFolderPath + "display.txt";
        public static string configFilePath = mainFolderPath + "config.txt";
        public static string botWhiteListFilePath = mainFolderPath + "botWhiteList.txt";


        //Manager
        public static GameManager gameManager;
        public static PlayerMovement clientMovement;
        public static PlayerInventory clientInventory;
        public static PlayerStatus clientStatus;
        public static LobbyManager lobbyManager;
        public static SteamManager steamManager;

        //TextBox
        public static ChatBox chatBoxInstance;

        //Dictionary
        public static Il2CppSystem.Collections.Generic.Dictionary<ulong, PlayerManager> activePlayers;
        public static System.Collections.Generic.Dictionary<string, System.Func<string>> DisplayDataCallbacks;

        //List
        public static List<Il2CppSystem.Collections.Generic.Dictionary<ulong, MonoBehaviourPublicCSstReshTrheObplBojuUnique>.Entry> playersList;
        public static List<ulong> botWhiteList = new();

        //Rigidbody
        public static Rigidbody clientBody;

        //GameObject
        public static GameObject clientObject;

        //int
        public static int mapId, modeId, exeFrameRate;

        //float
        public static float displayFrameRate;

        //ulong
        public static ulong clientId, clientIdSafe;

        //string
        public static string gameState, lastGameState, layout, menuKey;

        //bool
        public static bool menuTrigger, isClientBot;



    }
}
