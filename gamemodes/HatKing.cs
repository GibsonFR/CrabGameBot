using static GibsonBot.HatKingConstants;
using static GibsonBot.HatKingManager;
using static GibsonBot.HatKingUtility;
using static GibsonBot.PathFindingUtility;
using static GibsonBot.SnowballsUtility;

namespace GibsonBot
{
    internal class HatKingConstants
    {
        public const int HAT_KING_MODE_ID = 10;
        public const float MAX_DISTANCE_DIRECT_PATH = 200f;
        public const float UPDATE_INTERVAL = 0.5f;
    }
    public class HatKingManager : MonoBehaviour
    {
        private bool isHatKingMode = false;
        private Vector3 playerPos = Vector3.zero;
        private PlayerManager closestGoodBadPlayer = null;
        private Vector3 closestGoodBadPlayerPos = Vector3.zero;
        private Vector3 safePos = Vector3.zero;
        private float distanceToGoodBadPlayer = 0f;
        private float distanceToSafePos = 0f;
        private bool isHat = false;
        private float elapsedUpdate = 0f;
        public static GameModeHatKing gamemodeHatKingManager;
        private bool init = false;

        void Awake()
        {
            isHatKingMode = LobbyManager.Instance.gameMode.id == HAT_KING_MODE_ID;

            if (!isHatKingMode) return;
        }
        void Update()
        {
            if (!isHatKingMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedUpdate += Time.deltaTime;

            if (!init)
            {
                init = true;
                gamemodeHatKingManager = GameManager.Instance.GetComponent<GameModeHatKing>();
            }

            playerPos = clientBody.transform.position;
            isHat = IsHat();

            if (elapsedUpdate >= UPDATE_INTERVAL)
            {
                closestGoodBadPlayer = FindNearestGoodBadPlayer(isHat, playerPos);
            }

            if (closestGoodBadPlayer != null)
            {
                closestGoodBadPlayerPos = closestGoodBadPlayer.transform.position;
                distanceToGoodBadPlayer = Vector3.Distance(playerPos, closestGoodBadPlayerPos);
            }

            if (isHat)
            {
                safePos = FindNodeAwayFromThreat(playerPos, closestGoodBadPlayerPos, 20f);
                distanceToSafePos = Vector3.Distance(safePos, playerPos);
                if (distanceToSafePos > 3f) MoveWithPathFinding(safePos, playerPos);
            }
            else
            {
                MoveWithPathFinding(closestGoodBadPlayerPos, playerPos);

                if (distanceToGoodBadPlayer <= 8f)
                {
                    clientMovement.playerCam.LookAt(closestGoodBadPlayerPos);
                    ClientSend.PunchPlayer(closestGoodBadPlayer.steamProfile.m_SteamID, (closestGoodBadPlayer.gameObject.transform.forward * -1) + new Vector3(0, 1.5f, 0));
                }
            }
        }
    }

    internal class HatKingUtility
    {
        public static bool IsHat()
        {
            return gamemodeHatKingManager.taggedPlayers.Contains(clientId);
        }
        public static PlayerManager FindNearestGoodBadPlayer(bool isHat, Vector3 playerPos)
        {

            if (gamemodeHatKingManager == null) return null;
            Il2CppSystem.Collections.Generic.List<ulong> hatPlayersId = gamemodeHatKingManager.taggedPlayers;

            if (!isHat)
            {
                List<PlayerManager> hatPlayersList = [];

                foreach (var hatPlayerId in hatPlayersId)
                {
                    PlayerManager hatPlayer = null;
                    if (GameManager.Instance.activePlayers.ContainsKey(hatPlayerId))
                    {
                        hatPlayer = GameManager.Instance.activePlayers[hatPlayerId];
                        if (hatPlayer != null) hatPlayersList.Add(hatPlayer);
                    }
                }

                PlayerManager closestHatPlayer = null;
                float shortestDistance = float.MaxValue;

                foreach (var hatPlayer in hatPlayersList)
                {
                    Vector3 badPlayerPos = hatPlayer.transform.position;
                    float distanceToBadPlayer = Vector3.Distance(playerPos, badPlayerPos);

                    if (distanceToBadPlayer < shortestDistance)
                    {
                        closestHatPlayer = hatPlayer;
                        shortestDistance = distanceToBadPlayer;
                    }
                }

                return closestHatPlayer;
            }
            else
            {
                List<PlayerManager> noHatPlayersList = [];

                List<PlayerManager> hatPlayersList = [];

                foreach (var badPlayerId in hatPlayersId)
                {
                    PlayerManager badPlayer = null;
                    if (GameManager.Instance.activePlayers.ContainsKey(badPlayerId))
                    {
                        badPlayer = GameManager.Instance.activePlayers[badPlayerId];
                        if (badPlayer != null) hatPlayersList.Add(badPlayer);
                    }
                }

                foreach (var player in GameManager.Instance.activePlayers)
                {
                    try
                    {
                        if (player.value == null) continue;
                        if (player.value.dead) continue;
                        if (hatPlayersList.Contains(player.value)) continue;

                        noHatPlayersList.Add(player.value);
                    }
                    catch { }

                }

                PlayerManager closestNoHatPlayer = null;
                float shortestDistance = float.MaxValue;

                foreach (var noHatPlayer in noHatPlayersList)
                {
                    if (noHatPlayer.steamProfile.m_SteamID == clientId) continue;
                    Vector3 noHatPlayerPos = noHatPlayer.transform.position;
                    float distanceToGoodPlayer = Vector3.Distance(playerPos, noHatPlayerPos);

                    if (distanceToGoodPlayer < shortestDistance)
                    {
                        closestNoHatPlayer = noHatPlayer;
                        shortestDistance = distanceToGoodPlayer;
                    }
                }
                return closestNoHatPlayer;

            }
        }
    }
}

