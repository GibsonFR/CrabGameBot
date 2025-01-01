using static GibsonBot.DodgeBallUtility;
using static GibsonBot.DodgeBallConstants;
using static GibsonBot.SnowballsUtility;
using static GibsonBot.SnowballsManager;
using static GibsonBot.InputManager;


namespace GibsonBot
{
    internal class DodgeBallConstants
    {
        public const int DODGEBALL_MODE_ID = 17;
        public const float SNOWBALLPILE_INTERACTION_MAX_DISTANCE = 7f;
        public const int MAX_QUEUE_SIZE_FOR_BAD_PLAYER_POSITIONS = 10;
        public const float QUEUE_POSITION_WAIT_TIME = 0.05f;
        public const float CALCULATE_VELOCITY_COUNT = 2f;
        public static readonly Vector3 SAFE_POSITION_CUBE_SIZE = new(1f, 1f, 1f);
        public const float SAFE_POSITION_CUBE_LIFETIME = 0.1f;
    }

    internal class DodgeBallManager : MonoBehaviour
    {
        private bool isDodgeBallMode = false;
        private bool init = false;
        private bool hasCalculatedVelocity = false;
        private List<PlayerManager> badPlayersList = [];
        private SnowballPile closestSnowballPile = null;
        private PlayerManager closestPlayer = null;
        private Vector3 playerPos, closestPlayerPosition, closestPlayerPreviousPosition, closestPlayerVelocity, closestSnowballPilePos, safePos;
        private float distanceToClosestSnowballPile;
        private List<Vector3> capturedVelocities = [];
        private Vector3 calculatedAimPoint = Vector3.zero;

        void Awake()
        {
            isDodgeBallMode = LobbyManager.Instance.gameMode.id == DODGEBALL_MODE_ID;
        }

        void FixedUpdate()
        {
            if (!isDodgeBallMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            if (closestPlayer == null) closestPlayer = FindClosestPlayer(playerPos);
            else if (closestPlayer.dead) FindClosestPlayer(playerPos);

            if (closestPlayer == null) return;

            closestPlayerPosition = closestPlayer.GetComponent<Rigidbody>().transform.position;
            capturedVelocities.Add(closestPlayer.GetComponent<Rigidbody>().velocity);

            if (capturedVelocities.Count >= CALCULATE_VELOCITY_COUNT)
            {
                Vector3 totalVelocity = Vector3.zero;
                foreach (var capturedVelocity in capturedVelocities)
                {
                    totalVelocity += capturedVelocity;
                }
                closestPlayerVelocity = totalVelocity / capturedVelocities.Count;
                capturedVelocities.Clear();
                hasCalculatedVelocity = true;

                // Calculate the aim position and adjust the camera to aim at the target
                CalculateAimPoint(playerPos, clientMovement.playerCam.transform.position, closestPlayerPosition, closestPlayerVelocity, out Vector3 aimPos);
                calculatedAimPoint = aimPos;
            }
        }
        void Update()
        {
            if (!isDodgeBallMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            playerPos = clientBody.transform.position;

            if (!init)
            {
                init = true;

                GetBadPlayers(ref badPlayersList, ref playerPos);

                UpdateClosestSnowballPile(ref closestSnowballPile, ref closestSnowballPilePos, playerPos);
            }

            if (badPlayersList.Count == 0) return;

            if (closestPlayer == null || closestPlayer.dead) closestPlayer = FindClosestBadPlayer(badPlayersList, playerPos);


            if (closestSnowballPile != null)
            {
                distanceToClosestSnowballPile = Vector3.Distance(closestSnowballPilePos, playerPos);
            }

            if (dangerousSnowballs.Count() != 0)
            {
                safePos = PathFindingUtility.FindSnowballSafePosition(PathFindingUtility.WorldToGrid(playerPos), dangerousSnowballs, dangerousSnowballsDirections, playerPos);
                CreateDebugCube(safePos, SAFE_POSITION_CUBE_SIZE, SAFE_POSITION_CUBE_LIFETIME);
                MoveDiagonaly(safePos, playerPos);

            }
            else if (distanceToClosestSnowballPile > SNOWBALLPILE_INTERACTION_MAX_DISTANCE && clientInventory.currentItem == null) MoveDiagonaly(closestSnowballPilePos, playerPos);

            if (clientInventory.currentItem == null && distanceToClosestSnowballPile <= SNOWBALLPILE_INTERACTION_MAX_DISTANCE) closestSnowballPile?.TryInteract();

            if (hasCalculatedVelocity)
            {
                hasCalculatedVelocity = false;
                if (UseSnowball(calculatedAimPoint))
                {
                    UpdateClosestSnowballPile(ref closestSnowballPile, ref closestSnowballPilePos, playerPos);
                }
            }
        }
    }


    internal class DodgeBallUtility
    {
        public static void GetBadPlayers(ref List<PlayerManager> badPlayersList,ref Vector3 playerPos)
        {
            badPlayersList.Clear(); 

            foreach (var player in GameManager.Instance.activePlayers.Values)
            {
                if (player != null && !player.dead && player.steamProfile.m_SteamID != clientId)
                {
                    Vector3 validPlayerPosition = player.transform.position;

                    bool isOnOtherSide = (playerPos.z > 1 && validPlayerPosition.z < 1) || (playerPos.z < 1 && validPlayerPosition.z > 1);

                    if (isOnOtherSide)
                    {
                        badPlayersList.Add(player);
                    }
                }
            }
        }

        public static PlayerManager FindClosestBadPlayer(List<PlayerManager> badPlayerList, Vector3 playerPos)
        {
            PlayerManager closestPlayer = null;
            float shortestDistance = float.MaxValue;

            List<PlayerManager> playersToRemove = new List<PlayerManager>(); // Correcting array initialization

            foreach (var player in badPlayerList)
            {
                if (player == null) // Add this null check to avoid null reference exception
                {
                    playersToRemove.Add(player);
                    continue;
                }

                if (player.dead)
                {
                    playersToRemove.Add(player);
                    continue;
                }

                float currentDistance = Vector3.Distance(playerPos, player.transform.position);

                if (currentDistance < shortestDistance)
                {
                    closestPlayer = player;
                    shortestDistance = currentDistance;
                }
            }

            // Remove dead or null players from the original list
            foreach (var player in playersToRemove)
            {
                badPlayerList.Remove(player);
            }

            return closestPlayer;
        }   
    }
}
