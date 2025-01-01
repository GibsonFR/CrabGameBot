using static GibsonBot.BombTagConstants;
using static GibsonBot.BombTagManager;
using static GibsonBot.BombTagUtility;
using static GibsonBot.PathFindingUtility;
using static GibsonBot.SnowballsUtility;

namespace GibsonBot
{
    internal class BombTagConstants
    {
        public const int BOMB_TAG_MODE_ID = 6;
        public const float MAX_DISTANCE_DIRECT_PATH = 200f;
        public const float UPDATE_INTERVAL = 0.5f;
    }
    public class BombTagManager : MonoBehaviour
    {
        private bool isBombTagMode = false;
        private Vector3 playerPos = Vector3.zero;
        private PlayerManager closestGoodBadPlayer = null;
        private Vector3 closestGoodBadPlayerPos = Vector3.zero;
        private Vector3 safePos = Vector3.zero;
        private float distanceToGoodBadPlayer = 0f;
        private float distanceToSafePos = 0f;
        private bool isTag = false;
        private float elapsedUpdate = 0f;
        public static GameModeBombTag gamemodeBombTagManager;
        private bool init = false;

        void Awake()
        {
            isBombTagMode = LobbyManager.Instance.gameMode.id == BOMB_TAG_MODE_ID;

            if (!isBombTagMode) return;
        }
        void Update()
        {
            if (!isBombTagMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedUpdate += Time.deltaTime;

            if (!init)
            {
                init = true;
                gamemodeBombTagManager = GameManager.Instance.GetComponent<GameModeBombTag>();
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
                safePos = FindNodeAwayFromThreat(playerPos, closestGoodBadPlayerPos, 20f);
                distanceToSafePos = Vector3.Distance(safePos, playerPos);
                if (distanceToSafePos > 3f) MoveWithPathFinding(safePos, playerPos);
            }
        }
    }

    internal class BombTagUtility
    {
        public static PlayerManager FindNearestGoodBadPlayer(bool isTag, Vector3 playerPos)
        {

            if (gamemodeBombTagManager == null) return null;
            Il2CppSystem.Collections.Generic.List<ulong> badPlayersId = gamemodeBombTagManager.field_Private_List_1_UInt64_0;

            if (!isTag)
            {
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

                foreach (var playerId in allPlayersId)
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
