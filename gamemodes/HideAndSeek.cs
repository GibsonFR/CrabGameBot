using static GibsonBot.HideAndSeekConstants;
using static GibsonBot.HideAndSeekManager;
using static GibsonBot.HideAndSeekUtility;
using static GibsonBot.PathFindingUtility;
using static GibsonBot.SnowballsUtility;

namespace GibsonBot
{
    internal class HideAndSeekConstants
    {
        public const int HIDE_AND_SEEK_MODE_ID = 5;
        public const float MAX_DISTANCE_DIRECT_PATH = 200f;
        public const float UPDATE_INTERVAL = 0.5f;
    }
    public class HideAndSeekManager : MonoBehaviour
    {
        private bool isHideAndSeekMode = false;
        private Vector3 playerPos = Vector3.zero;
        private PlayerManager closestGoodBadPlayer = null;
        private Vector3 closestGoodBadPlayerPos = Vector3.zero;
        private Vector3 safePos = Vector3.zero;
        private float distanceToGoodBadPlayer = 0f;
        private float distanceToSafePos = 0f;
        private bool isTag = false;
        private float elapsedUpdate = 0f;
        public static GameModeHideAndSeek gamemodeHideAndSeekManager;
        private bool init = false;

        void Awake()
        {
            isHideAndSeekMode = LobbyManager.Instance.gameMode.id == HIDE_AND_SEEK_MODE_ID;

            if (!isHideAndSeekMode) return;
        }
        void Update()
        {
            if (!isHideAndSeekMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedUpdate += Time.deltaTime;

            if (!init)
            {
                init = true;
                gamemodeHideAndSeekManager = GameManager.Instance.GetComponent<GameModeHideAndSeek>();
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

    internal class HideAndSeekUtility
    {
        public static PlayerManager FindNearestGoodBadPlayer(bool isTag, Vector3 playerPos)
        {

            if (gamemodeHideAndSeekManager == null) return null;
            Il2CppSystem.Collections.Generic.List<ulong> badPlayersId = gamemodeHideAndSeekManager.seekers;
            Il2CppSystem.Collections.Generic.List<ulong> goodPlayersId = gamemodeHideAndSeekManager.hiders;

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
                List<PlayerManager> goodPlayersList = [];

                foreach (var goodPlayerId in goodPlayersId)
                {
                    PlayerManager goodPlayer = null;
                    if (GameManager.Instance.activePlayers.ContainsKey(goodPlayerId))
                    {
                        goodPlayer = GameManager.Instance.activePlayers[goodPlayerId];
                        if (goodPlayer != null) goodPlayersList.Add(goodPlayer);
                    }
                }

                PlayerManager closestGoodPlayer = null;
                float shortestDistance = float.MaxValue;

                foreach (var goodPlayer in goodPlayersList)
                {
                    Vector3 goodPlayerPos = goodPlayer.transform.position;
                    float distanceToBadPlayer = Vector3.Distance(playerPos, goodPlayerPos);

                    if (distanceToBadPlayer < shortestDistance)
                    {
                        closestGoodPlayer = goodPlayer;
                        shortestDistance = distanceToBadPlayer;
                    }
                }

                return closestGoodPlayer;

            }
        }
    }
}
