using static GibsonBot.DeathFromAboveConstants;
using static GibsonBot.DeathFromAboveUtility;
using static GibsonBot.DeathFromAboveManager;

namespace GibsonBot
{
    internal class DeathFromAboveConstants
    {
        public const string LAYER_MASK_OBJECT = "Object";
        public const float COLUMN_WIDTH = 8f;
        public const float MAX_RAY_CAST_DISTANCE = 500f;
        public const float DEFAULT_GROUND_HEIGHT = -19.1f;
        public const float COMBAT_PROBABILITY_PER_FRAME = 0.02f;
        public const float UPDATE_INTERVAL = 0.1f;
        public const float MAP_WIDTH = 96;
        public const int DEATH_FROM_ABOVE_MODE_ID = 14;
        public const int COLUMNS_PER_ROW = 12;
        public const int TOTAL_COLUMNS = 144;
        public const float MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP = 10f;
    }
    public class DeathFromAboveManager : MonoBehaviour
    {
        public static Dictionary<int, bool> columnSunlightStatus = []; 
        private Vector3 playerPos = Vector3.zero;
        private Vector3 safePos = Vector3.zero;
        private Vector3 closestBadPlayerPos = Vector3.zero;
        private PlayerManager closestBadPlayer = null;
        private bool isDeathFromAboveMode = false;
        private float elapsedUpdate = 0f;
        private float distanceFromSafePos = 0f;

        void Awake()
        {
            isDeathFromAboveMode = LobbyManager.Instance.gameMode.id == DEATH_FROM_ABOVE_MODE_ID;

            if (!isDeathFromAboveMode) return;

            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                columnSunlightStatus[i] = false;
            }
        }
        void Update()
        {
            if (!isDeathFromAboveMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;
            
            elapsedUpdate += Time.deltaTime;
            playerPos = clientBody.transform.position;

            if (elapsedUpdate >= UPDATE_INTERVAL)
            {
                UpdateColumnSunlightStatus();
                Vector3 newSafePosition = FindSafePosition(playerPos);
                if (newSafePosition != Vector3.zero) safePos = new Vector3(newSafePosition.x, DEFAULT_GROUND_HEIGHT, newSafePosition.z);

                elapsedUpdate = 0f;
                closestBadPlayer = FindClosestPlayer(playerPos);
                if (closestBadPlayer != null) closestBadPlayerPos = closestBadPlayer.transform.position;

            }
            
            distanceFromSafePos = Vector3.Distance(safePos, playerPos);

            if (safePos != Vector3.zero)
            {
                if (distanceFromSafePos > 2) InputManager.MoveWithBunnyHop(safePos, playerPos, MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP);
                else BotFunctions.Combat(playerPos, closestBadPlayer, COMBAT_PROBABILITY_PER_FRAME);      
            }
        }
    }

    internal class DeathFromAboveUtility
    {
        public static void UpdateColumnSunlightStatus()
        {
            int layerMask = LayerMask.GetMask(LAYER_MASK_OBJECT);

            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                Vector3 columnPosition = CalculateColumnCenter(i);
                columnSunlightStatus[i] = IsInSunlight(columnPosition, layerMask);
            }
        }

        public static bool IsInSunlight(Vector3 point, int layerMask)
        {
            RaycastHit hit;

            if (Physics.Raycast(point, Vector3.up, out hit, MAX_RAY_CAST_DISTANCE, layerMask))
            {
                return false;
            }

            return true;
        }

        public static Vector3 CalculateColumnCenter(int columnIndex)
        {
            int col = columnIndex % COLUMNS_PER_ROW; 
            int row = columnIndex / COLUMNS_PER_ROW;
            float x = (col * COLUMN_WIDTH) - ((MAP_WIDTH / 2) - (COLUMN_WIDTH / 2)); 
            float z = (row * COLUMN_WIDTH) - ((MAP_WIDTH / 2) - (COLUMN_WIDTH / 2));
            return new Vector3(x, 0, z); 
        }

        public static Vector3 FindSafePosition(Vector3 playerPos)
        {
            int currentPlayerColumn = DetermineColumnIndex(playerPos);

            if (columnSunlightStatus[currentPlayerColumn])
            {
                return Vector3.zero;
            }

            Vector3 nearestSafePosition = Vector3.zero;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                if (columnSunlightStatus[i])
                {
                    Vector3 columnCenter = CalculateColumnCenter(i);
                    float distance = Vector3.Distance(playerPos, columnCenter);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestSafePosition = columnCenter;
                    }
                }
            }

            return nearestSafePosition;
        }

        public static int DetermineColumnIndex(Vector3 position)
        {

            int col = Mathf.FloorToInt((position.x + (MAP_WIDTH / 2)) / COLUMN_WIDTH);
            int row = Mathf.FloorToInt((position.z + (MAP_WIDTH / 2)) / COLUMN_WIDTH);
            return col + row * COLUMNS_PER_ROW; 
        }
    }
}
