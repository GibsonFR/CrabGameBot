using TMPro;
using static GibsonBot.SteppingStonesConstants;
using static GibsonBot.SteppingStonesManager;
using static GibsonBot.SteppingStonesUtility;

namespace GibsonBot
{
    internal class SteppingStonesConstants
    {
        public const string ICE_PIECE_SOLID_GAME_OBJECT_NAME = "IcePieceSolid(Clone)";
        public const string SAND_PIECE_SOLID_GAME_OBJECT_NAME = "PiceSolid(Clone)";
        public const string GLASS_PIECE_SOLID_GAME_OBJECT_NAME = "GlassSolid(Clone)";
        public const int STEPPING_STONES_MODE_ID = 3;
    }

    public class SteppingStonesManager : MonoBehaviour
    {
        public static Vector3 playerPos, closestPlayerPos, closestTile, lastClosestTile, targetPosition;
        public static List<Vector3> allTiles;
        public static bool init, hasJumped;
        public static float elapsedTime, elapsedNextClosestTile, timer = 3f;
        public static int nextTileIndex;
        public static PlayerManager closestPlayer;
        private bool isSteppingStonesMode = false;

        void Awake()
        {
            isSteppingStonesMode = LobbyManager.Instance.gameMode.id == STEPPING_STONES_MODE_ID;
            ResetVariables();
        }

        void Update()
        {
            // Ensure the bot is in the correct mode before proceeding
            if (!isSteppingStonesMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;
            
            UpdateTimers();

            // Get the current player position and find the closest player
            playerPos = clientBody.transform.position;
            closestPlayer = FindClosestPlayer(playerPos);

            if (closestPlayer != null)
            {
                closestPlayerPos = closestPlayer.transform.position;
            }

            // Initialize the tiles if not already done
            if (!init && elapsedTime > 0.1f)
            {
                allTiles = FindAndSortTiles(playerPos);
                InitializeTilesForSpecificMaps();
                init = true;
            }

            if (!init) return; // Ensure the initialization is completed

            UpdateClosestTile();

            // Set the target position slightly above the closest tile for better jumping accuracy
            targetPosition = (mapId == 8 || mapId == 24) ? closestTile : closestTile + new Vector3(0, 2.5f, 0);

            // Handle jumping logic based on the map type
            HandleJumpingLogic();

            // Move the player towards the closest tile if no jump is in progress and distance is greater than 3 units
            if (!InputManager.simulateJump && Vector3.Distance(playerPos, closestTile) > 3f)
            {
                InputManager.Move(closestTile, playerPos);
            }

            if (!InputManager.simulateJump)
            {
                BotFunctions.Combat(playerPos, closestPlayer, 1f);
            }
            
        }
    }

    internal class SteppingStonesUtility
    {
        /// Resets key variables at the beginning or when needed.
        public static void ResetVariables()
        {
            init = false;
            hasJumped = false;
            nextTileIndex = 0;
            elapsedTime = 0;
            elapsedNextClosestTile = 2.5f;
            allTiles = [];
        }

        /// Updates timers for managing elapsed time and tile switching.
        public static void UpdateTimers()
        {
            if (GameData.GetGameState() == "Freeze") return;

            float delta = Time.deltaTime;
            elapsedTime += delta;
            elapsedNextClosestTile += delta;
        }

        /// Updates the closest tile based on the elapsed time and player's position.
        public static void UpdateClosestTile()
        {
            if (elapsedNextClosestTile > timer)
            {
                // Find the next closest tile
                closestTile = FindNextClosestTile(allTiles, playerPos);

                // Track the last tile before updating the index
                lastClosestTile = nextTileIndex >= 1 ? allTiles[nextTileIndex - 1] : allTiles[nextTileIndex];

                // Reset the timer and randomize the next tile switch time
                elapsedNextClosestTile = 0f;
                timer = UnityEngine.Random.Range(1f, 10f);
            }
        }

        /// Handles the logic for jumping based on the map type.
        public static void HandleJumpingLogic()
        {
            if (!hasJumped && nextTileIndex >= 1)
            {
                // Start the jump to the next tile
                InputManager.StartJump(targetPosition);
                hasJumped = true;
                elapsedTime = 0;
            }
        }

        /// Initializes specific tiles based on the map type.
        public static void InitializeTilesForSpecificMaps()
        {
            if (mapId == 8 || mapId == 24)
            {
                closestTile = new Vector3(0, 13, -20);
            }

            // Add initial and final points to the tile list based on the map
            float y = allTiles[0].y + 2;
            float z = allTiles[0].z - 12;
            float xFinal = allTiles.Last().x;
            float yFinal = allTiles.Last().y;
            float zFinal = allTiles.Last().z + 30;

            if (mapId == 8 || mapId == 24)
            {
                allTiles.Insert(0, new Vector3(0, 11.1f, z));
            }
            else
            {
                allTiles.Insert(0, new Vector3(0, -8.5f, z));
            }

            allTiles.Add(new Vector3(xFinal, yFinal, zFinal));
        }

        /// Finds and sorts tiles by their distance from the player.
        public static List<Vector3> FindAndSortTiles(Vector3 referencePoint)
        {
            // Find all tile-like objects in the scene
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            // Filter out the tiles based on specific names
            var filteredTiles = allObjects
                .Where(obj => obj.name == "IcePieceSolid(Clone)" || obj.name == "PiceSolid(Clone)" || obj.name == "GlassSolid(Clone)")
                .Select(obj => obj.transform.position)
                .OrderBy(pos => Vector3.Distance(pos, referencePoint))
                .ToList();

            return filteredTiles;
        }

        /// Finds the next closest tile based on the player's current position.
        public static Vector3 FindNextClosestTile(List<Vector3> tilePositions, Vector3 referencePoint)
        {
            // Calculate the distance to the next closest tile
            var distanceToClosestTile = Vector3.Distance(referencePoint, tilePositions[nextTileIndex]);

            // If the player is within a 3-unit range of the tile, move to the next tile
            if (distanceToClosestTile < 3f)
            {
                nextTileIndex++;
                hasJumped = false;
            }

            return tilePositions[nextTileIndex];
        }
    }
}
