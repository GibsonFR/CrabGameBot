using static GibsonBot.CrabFightConstants;
using static GibsonBot.CrabFightUtility;
using static GibsonBot.CrabFightManager;
using static GibsonBot.SnowballsManager;
using static GibsonBot.SnowballsUtility;
using static GibsonBot.InputManager;
using static GibsonBot.PathFindingUtility;


namespace GibsonBot
{

    internal class CrabFightConstants
    {
        public const int CRAB_FIGHT_MODE_ID = 18;
        public const string CRAB_GAME_OBJECT_NAME = "Crab";
        public const string SHOCK_WAVE_GAME_OBJECT_NAME = "ShockwaveRing(Clone)";
        public const string SHOCK_WAVE_COLLIDER_GAME_OBJECT_NAME = "Hurt";
        public const float SNOWBALLPILE_INTERACTION_MAX_DISTANCE = 7f;
        public const float CRAB_DANGER_DURATION = 3f;
        public static readonly Vector3 SAFE_POSITION_CUBE_SIZE = new(1f, 1f, 1f);
        public const float SAFE_POSITION_CUBE_LIFETIME = 0.1f;
        public const float CRAB_DANGER_RADIUS = 6f;
        public static readonly Vector3 CRAB_POSITION_OFFSET = new(0f, 2f, 0f);
    }

    public class CrabFightManager : MonoBehaviour
    {
        public static Dictionary<Vector3, float> crabDangers = [];
        private static SnowballPile closestSnowballPile = null; 
        private Vector3 crabPos = Vector3.zero;
        private Vector3 playerPos = Vector3.zero;
        private Vector3 closestSnowballPilePos = Vector3.zero;
        private Vector3 safePos = Vector3.zero;
        private float elapsedUpdate = 0f;
        private GameObject crab = null;
        private bool isCrabFightMode = false;
        private float distanceToClosestSnowballPile = 0f;
        public static bool updateSafePos = false;

        void Awake()
        {
            isCrabFightMode = LobbyManager.Instance.gameMode.id == CRAB_FIGHT_MODE_ID;
        }

        void Update()
        {
            List<Vector3> keysToRemove = [];

            // Populate the keys list with the keys from the dangerousSnowballs dictionary
            List<Vector3> keys = new(crabDangers.Keys);

            // Iterate through the active crab dangers and update their timers
            foreach (var key in keys)
            {
                if (crabDangers[key] >= 0)
                {
                    crabDangers[key] -= Time.deltaTime;
                }
                else
                {
                    // Collect the keys to remove after iteration
                    keysToRemove.Add(key);
                }
            }

            if (!isCrabFightMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;


            // Remove the crab dangers with expired timers after the iteration
            foreach (var key in keysToRemove)
            {
                crabDangers.Remove(key);
            }

            elapsedUpdate += Time.deltaTime;
            playerPos = clientBody.transform.position;


            if (crab == null) crab = GameObject.Find(CRAB_GAME_OBJECT_NAME);
            else if (crabPos == Vector3.zero) crabPos = crab.transform.position + CRAB_POSITION_OFFSET;

            if (elapsedUpdate > 0.1f)
            {
                elapsedUpdate = 0f;
                closestSnowballPile = GetClosestSafeSnowballPile(playerPos, crab, snowballPiles);
                closestSnowballPilePos = closestSnowballPile.transform.position;
                CheckShockWave();
            }

            if (closestSnowballPile != null) distanceToClosestSnowballPile = Vector3.Distance(closestSnowballPilePos, playerPos);

            if (crabDangers.Count() != 0)
            {
                if (updateSafePos)
                {
                    updateSafePos = false;
                    safePos = FindNodeAwayFromThreats(playerPos, crabDangers.Keys.ToList(), CRAB_DANGER_RADIUS);

                    if (safePos == Vector3Int.zero) safePos = playerPos;
                }

                CreateDebugCube(safePos, SAFE_POSITION_CUBE_SIZE, SAFE_POSITION_CUBE_LIFETIME);
                MoveDiagonaly(safePos, playerPos);

            }
            else if (distanceToClosestSnowballPile > SNOWBALLPILE_INTERACTION_MAX_DISTANCE && clientInventory.currentItem == null) MoveDiagonaly(closestSnowballPilePos, playerPos);

            if (clientInventory.currentItem == null && distanceToClosestSnowballPile <= SNOWBALLPILE_INTERACTION_MAX_DISTANCE) closestSnowballPile?.TryInteract();

            if (crab != null) UseSnowball(crabPos);
        }
    }
    internal class CrabFightUtility
    {
        public static void CheckShockWave()
        {
            GameObject[] rings = GameObject.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name == SHOCK_WAVE_GAME_OBJECT_NAME)
            .ToArray();

            if (rings.Length > 0 && clientBody != null)
            {
                Vector3 playerPosition = clientBody.transform.position;
                float closestDistance = float.MaxValue;
                Vector3 closestPoint = Vector3.zero;

                foreach (GameObject ring in rings)
                {
                    MeshRenderer ringRenderer = ring.transform.Find(SHOCK_WAVE_COLLIDER_GAME_OBJECT_NAME).GetComponent<MeshRenderer>();

                    if (ringRenderer != null)
                    {

                        Bounds bounds = ringRenderer.bounds;

                        Vector3 currentClosestPoint = bounds.ClosestPoint(playerPosition);

                        float currentDistance = Vector3.Distance(playerPosition, currentClosestPoint);

                        if (currentDistance < closestDistance && currentDistance > 2f)
                        {
                            closestDistance = currentDistance;
                            closestPoint = currentClosestPoint;
                        }
                    }
                }

                if (closestDistance > 0 && closestDistance < 10f && closestDistance > 2f)
                {
                    clientMovement.Jump();
                }
            }
        }

        public static SnowballPile GetClosestSafeSnowballPile(Vector3 playerPos, GameObject crab, List<SnowballPile> snowballPiles)
        {
            float shortestDistance = float.MaxValue;
            SnowballPile closestPile = null;

            if (clientBody == null || clientBody.transform == null)
                return null;

            Vector3? crabForward = crab != null ? (Vector3?)crab.transform.forward : null;
            Vector3? crabPosition = crab != null ? (Vector3?)crab.transform.position : null;

            foreach (var pile in snowballPiles)
            {
                if (pile == null || pile.transform == null) continue;

                GameObject rootPileGameObject = pile.transform.root.gameObject;
                Vector3 pilePosition = rootPileGameObject.transform.position;

                if (crabForward != null && crabPosition != null)
                {
                    Vector3 directionToPileFromCrab = (pilePosition - crabPosition.Value).normalized;
                    if (Vector3.Dot(crabForward.Value, directionToPileFromCrab) < Mathf.Cos(60 * Mathf.Deg2Rad))
                    {
                        float distance = Vector3.Distance(playerPos, pilePosition);

                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            closestPile = pile;
                        }
                    }
                }
                else
                {
                    float distance = Vector3.Distance(playerPos, pilePosition);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestPile = pile;
                    }
                }
            }

            return closestPile;
        }
    }

    public class CrabFightPatchs
    {
        [HarmonyPatch(typeof(CrabFightCrabAttackWarning), nameof(CrabFightCrabAttackWarning.SetWarning))]
        [HarmonyPostfix]
        public static void OnCrabFightCrabAttackWarningSetWarning(CrabFightCrabAttackWarning __instance)
        {
            updateSafePos = true;
            crabDangers.Add(__instance.transform.gameObject.transform.position, CRAB_DANGER_DURATION);
        }
    }
}
