using static GibsonBot.SnowbrawlConstants;
using static GibsonBot.SnowballsUtility;
using static GibsonBot.SnowballsManager;
using static GibsonBot.InputManager;
using Il2CppSystem.Xml.Schema;

namespace GibsonBot
{
    internal class SnowbrawlConstants : MonoBehaviour
    {
        public const int SNOWBRAWL_MODE_ID = 16;
        public const float SNOWBALLPILE_INTERACTION_MAX_DISTANCE = 7f;
        public const int MAX_QUEUE_SIZE_FOR_BAD_PLAYER_POSITIONS = 10;
        public const float QUEUE_POSITION_WAIT_TIME = 0.05f;
        public static readonly Vector3 SAFE_POSITION_CUBE_SIZE = new(1f, 1f, 1f);
        public const float SAFE_POSITION_CUBE_LIFETIME = 0.2f;
        public const float SNOWBALL_MAX_RANGE = 200f;
        public const float CALCULATE_VELOCITY_COUNT = 2f;
    }

    internal class SnowBrawlManager : MonoBehaviour
    {
        private bool isSnowBrawlMode = false;
        private bool init = false;
        private bool hasCalculatedVelocity = false;
        private SnowballPile closestSnowballPile = null;
        private PlayerManager closestPlayer = null;
        private Vector3 playerPos, closestPlayerPosition, closestPlayerPreviousPosition, closestSnowballPilePos, safePos;
        private float distanceToClosestSnowballPile, lineOfSightClearElapsed;
        public static Vector3 closestPlayerVelocity = Vector3.zero;
        private List<Vector3> capturedVelocities = [];

        private Vector3 calculatedAimPoint = Vector3.zero;   

        void Awake()
        {
            isSnowBrawlMode = LobbyManager.Instance.gameMode.id == SNOWBRAWL_MODE_ID;
        }

        void FixedUpdate()
        {
            if (!isSnowBrawlMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

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
            if (!isSnowBrawlMode || !isClientBot || GetGameState() == "Freeze" || clientBody == null) return;

            playerPos = clientBody.transform.position;

            if (!init)
            {
                init = true;

                UpdateClosestSnowballPile(ref closestSnowballPile, ref closestSnowballPilePos, playerPos);
            }


            if (closestSnowballPile != null)
            {
                distanceToClosestSnowballPile = Vector3.Distance(closestSnowballPilePos, playerPos);
            }

            if (closestPlayer != null) 
            {
                if (IsLineOfSightClearToShootSnowball(SNOWBALL_MAX_RANGE, closestPlayer, playerPos)) lineOfSightClearElapsed += Time.deltaTime;
                else if (lineOfSightClearElapsed > 0) lineOfSightClearElapsed -= Time.deltaTime;
            }
            

            if (dangerousSnowballs.Count() != 0)
            {
                safePos = PathFindingUtility.FindSnowballSafePosition(PathFindingUtility.WorldToGrid(playerPos), dangerousSnowballs, dangerousSnowballsDirections, playerPos);
                CreateDebugCube(safePos, SAFE_POSITION_CUBE_SIZE, SAFE_POSITION_CUBE_LIFETIME);
                MoveDiagonaly(safePos, playerPos);

            }
            else if (distanceToClosestSnowballPile > SNOWBALLPILE_INTERACTION_MAX_DISTANCE && clientInventory.currentItem == null) PathFindingUtility.MoveWithPathFinding(closestSnowballPilePos, playerPos);
            else if (PlayerInventory.Instance.currentItem != null) PathFindingUtility.MoveWithPathFinding(closestPlayerPosition, playerPos);


            if (clientInventory.currentItem == null && distanceToClosestSnowballPile <= SNOWBALLPILE_INTERACTION_MAX_DISTANCE) closestSnowballPile?.TryInteract();

            if (lineOfSightClearElapsed >= 0.1f && hasCalculatedVelocity)
            {
                hasCalculatedVelocity = false;
                if (UseSnowball(calculatedAimPoint))
                {
                    UpdateClosestSnowballPile(ref closestSnowballPile, ref closestSnowballPilePos, playerPos);
                    FindClosestPlayer(playerPos);
                }
                lineOfSightClearElapsed = 0f;
            }
        }
    }
}
