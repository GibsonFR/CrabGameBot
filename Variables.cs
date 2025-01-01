namespace GibsonBot
{
    //Ici on stock les variables "globale" pour la lisibilité du code dans Plugin.cs 
    internal class Variables
    {
        //folder
        public static string assemblyFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"GibsonBot\";
        public static string configFolderPath = mainFolderPath + @"config\";

        //file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string displayFilePath = mainFolderPath + "display.txt";
        public static string configFilePath = mainFolderPath + "config.txt";
        public static string botWhiteListFilePath = mainFolderPath + "botWhiteList.txt";
        public static string KOTHbotTechFolderPath = mainFolderPath + @"KOTHBot\";
        public static string raceBotTechFolderPath = mainFolderPath + @"raceBot\";
        public static string tagBotTechFolderPath = mainFolderPath + @"tagBot\";
        public static string techFolderPath = mainFolderPath + @"techs\";
        public static string mapNameFilePath = configFolderPath + @"mapName.txt";
        public static string nodeMapFolderPath = mainFolderPath + @"nodeMap\";


        //Manager
        public static PlayerMovement clientMovement;
        public static PlayerInventory clientInventory;
        public static PlayerStatus clientStatus;

        //Dictionary
        public static Il2CppSystem.Collections.Generic.Dictionary<ulong, PlayerManager> activePlayers;
        public static Dictionary<string, System.Func<string>> DisplayDataCallbacks;

        //List
        public static List<Il2CppSystem.Collections.Generic.Dictionary<ulong, MonoBehaviourPublicCSstReshTrheObplBojuUnique>.Entry> playersList;
        public static List<ulong> botWhiteList = [];
        public static List<string> csvLines = [];

        //Rigidbody
        public static Rigidbody clientBody;

        //GameObject
        public static GameObject clientObject;

        //int
        public static int isClientCloneTagged, mapId, modeId, exeFrameRate, currentLineIndex;

        //float
        public static float displayFrameRate, elapsedFrame;

        //double
        public static double estimatedFPS;

        //ulong
        public static ulong clientId;

        //string
        public static string layout, menuKey, techName;

        //bool
        public static bool isReplayReaderInitialized, replaySafeClose, replayStop, replayActive, isReplayInit, initTech, damageTaken, menuTrigger, isClientBot, steamManagerAwakeCheck, bodyCollision;

        //Collider
        public static Collider closestCollider;

        //Vector3
        public static Vector3 closestColliderHitPoint, techPosition, clientClonePosition, otherPlayerClonePosition, clientCloneRotation;

        //Quaternion 
        public static Quaternion clientCloneQRotation;

    }
}
