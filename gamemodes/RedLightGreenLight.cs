using static GibsonBot.RedLightGreenLightConstants;
using static GibsonBot.RedLightGreenLightManager;
using static GibsonBot.RedLightGreenLightUtility;

namespace GibsonBot
{
    internal class RedLightGreenLightConstants
    {
        public const string STATUE_GAME_OBJECT_NAME = "Statue";
        public const int RED_LIGHT_GREEN_LIGHT_MODE_ID = 1;
        public static readonly Vector3 SAFE_POSITION = new(0, 0, 145);
    }

    public class RedLightGreenLightManager : MonoBehaviour
    {
        Vector3 closestPlayerPos = Vector3.zero;
        Vector3 playerPos = Vector3.zero;
        float distanceToSafePos = 0f;
        float distanceToClosestPlayer = 0f;
        PlayerManager closestPlayer = null;
        bool isMad, isMadSet, shouldLook;
        float elapsed;
        private bool isRedLightGreenLightMode = false;

        void Awake()
        {
            isRedLightGreenLightMode = LobbyManager.Instance.gameMode.id == RED_LIGHT_GREEN_LIGHT_MODE_ID;
        }
        void Update()
        {
            elapsed += Time.deltaTime;

            if (!isRedLightGreenLightMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;
            
            playerPos = clientBody.transform.position;
            closestPlayer = FindClosestPlayer(playerPos);

            if (closestPlayer != null)
            {
                closestPlayerPos = closestPlayer.transform.position;
            }

            distanceToSafePos = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(SAFE_POSITION.x, SAFE_POSITION.z));
            distanceToClosestPlayer = Vector3.Distance(closestPlayerPos, playerPos);

            if (IsStatueScanning() && playerPos.z < 128)
            {
                if (shouldLook && !InputManager.simulateStopASAP)
                {
                    shouldLook = false;
                    BotFunctions.LookAtTarget(closestPlayerPos);
                }

                if (elapsed > 0.5f) BotFunctions.Combat(playerPos, closestPlayer, 1f);

                isMadSet = false;
                isMad = false;
            }
            else
            {
                elapsed = 0f;
                InputManager.Stop();
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
                    InputManager.MoveWithBunnyHop(closestPlayerPos, playerPos, 0f);
                }
                else if (distanceToSafePos > 5) InputManager.MoveWithBunnyHop(SAFE_POSITION, playerPos, 0f);

            }
            
        }
    }
    internal class RedLightGreenLightUtility
    {
        public static bool IsStatueScanning()
        {
            GameObject statue = GameObject.Find(STATUE_GAME_OBJECT_NAME);
            if (statue != null) return statue.GetComponent<RedLightGreenLightStatue>().field_Private_Quaternion_2 == Quaternion.identity;
            else return false;
        }
    }
}
