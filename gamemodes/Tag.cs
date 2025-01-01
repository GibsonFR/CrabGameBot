using static GibsonBot.TagConstants;
using static GibsonBot.TagManager;
using static GibsonBot.TagUtility;
using static GibsonBot.PathFindingUtility;

namespace GibsonBot
{
    internal class TagConstants
    {
        public const int TAG_MODE_ID = 4;
        public const float MAX_DISTANCE_DIRECT_PATH = 200f;
        public const float UPDATE_INTERVAL = 0.5f;
    }
    public class TagManager : MonoBehaviour
    {
        private bool isTagMode = false;
        private Vector3 playerPos = Vector3.zero;
        private PlayerManager closestGoodBadPlayer = null;
        private Vector3 closestGoodBadPlayerPos = Vector3.zero;
        private Vector3 safePos = Vector3.zero; 
        private float distanceToGoodBadPlayer = 0f;
        private float distanceToSafePos = 0f;
        private bool isTag = false;
        private float elapsedUpdate = 0f;
        public static GameModeTag gamemodeTagManager;
        private bool init = false;

        void Awake()
        {
            isTagMode = LobbyManager.Instance.gameMode.id == TAG_MODE_ID;

            if (!isTagMode) return;
        }
        void Update()
        {
            if (!isTagMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedUpdate += Time.deltaTime;

            if (!init)
            {
                init = true;
                gamemodeTagManager = GameManager.Instance.GetComponent<GameModeTag>();
            }

            playerPos = clientBody.transform.position;
            isTag = clientInventory.currentItem != null;

            if (elapsedUpdate >= UPDATE_INTERVAL)
            {
                closestGoodBadPlayer = FindNearestGoodBadPlayer(isTag, playerPos);
            }

            if (closestGoodBadPlayer != null)
            {
                closestGoodBadPlayerPos = closestGoodBadPlayer.transform.position;
                distanceToGoodBadPlayer = Vector3.Distance(playerPos, closestGoodBadPlayerPos);
            }

            if (isTag)
            {
                MoveWithPathFinding(closestGoodBadPlayerPos, playerPos);

                if (distanceToGoodBadPlayer <= 5f)
                {
                    clientMovement.playerCam.LookAt(closestGoodBadPlayerPos);
                    clientInventory.UseItem();
                }
            }
            else
            {
                if (!clientBody.isKinematic)
                {
                    safePos = AutoTechFunctions.FindNearestTech(mapId, playerPos, techFolderPath);
                    distanceToSafePos = Vector3.Distance(safePos, playerPos);
                    if (distanceToSafePos > 0.5f) MoveWithPathFinding(safePos, playerPos);
                }
            }
        }
    }

    internal class TagUtility
    {
        public static PlayerManager FindNearestGoodBadPlayer(bool isTag, Vector3 playerPos)
        {

            if (!isTag)
            {
                if (gamemodeTagManager == null) return null;
                Il2CppSystem.Collections.Generic.List<ulong> badPlayersId = gamemodeTagManager.field_Private_List_1_UInt64_0;

                List<PlayerManager> badPlayersList = [];

                foreach (var badPlayerId in badPlayersId)
                {
                    PlayerManager badPlayer = null;
                    if (GameManager.Instance.activePlayers.ContainsKey(badPlayerId))
                    {
                        badPlayer = GameManager.Instance.activePlayers[badPlayerId];
                        if (badPlayer != null) badPlayersList.Add(badPlayer);
                    }
                }

                PlayerManager closestBadPlayer = null;
                float shortestDistance = float.MaxValue;

                foreach (var badPlayer in badPlayersList)
                {
                    Vector3 badPlayerPos = badPlayer.transform.position;
                    float distanceToBadPlayer = Vector3.Distance(playerPos, badPlayerPos);

                    if (distanceToBadPlayer < shortestDistance)
                    {
                        closestBadPlayer = badPlayer;
                        shortestDistance = distanceToBadPlayer;
                    }
                }

                return closestBadPlayer;
            }
            else
            {
                if (gamemodeTagManager == null) return null;
                Il2CppSystem.Collections.Generic.List<ulong> badPlayersId = gamemodeTagManager.field_Private_List_1_UInt64_0;

                List<ulong> allPlayersId = [];
                List<ulong> goodPlayersId = [];
                List<PlayerManager> goodPlayerList = [];

                foreach (var player in GameManager.Instance.activePlayers)
                {
                    try
                    {
                        if (player.value == null) continue;
                        if (player.value.steamProfile.m_SteamID == clientId) continue;
                        if (player.value.dead) continue;

                        allPlayersId.Add(player.value.steamProfile.m_SteamID);                    
                    }
                    catch { }
                }

                foreach (var playerId in  allPlayersId)
                {
                    if (badPlayersId.Contains(playerId)) 
                    {
                        continue;
                    }
                    goodPlayersId.Add(playerId);
                }


                foreach (var goodPlayerId in goodPlayersId)
                {
                    PlayerManager goodPlayer = null;
                    if (GameManager.Instance.activePlayers.ContainsKey(goodPlayerId))
                    {
                        goodPlayer = GameManager.Instance.activePlayers[goodPlayerId];
                        if (goodPlayer != null)
                        {
                            goodPlayerList.Add(goodPlayer);
                        }
                    }
                }


                PlayerManager closestGoodPlayer = null;
                float shortestDistance = float.MaxValue;

                foreach (var goodPlayer in goodPlayerList)
                {
                    if (goodPlayer.steamProfile.m_SteamID == clientId) continue;
                    Vector3 goodPlayerPos = goodPlayer.transform.position;
                    float distanceToGoodPlayer = Vector3.Distance(playerPos, goodPlayerPos);

                    if (distanceToGoodPlayer < shortestDistance)
                    {
                        closestGoodPlayer = goodPlayer;
                        shortestDistance = distanceToGoodPlayer;
                    }
                }
                return closestGoodPlayer;

            }
        }
    }
}
