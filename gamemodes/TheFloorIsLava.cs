using static GibsonBot.TheFloorIsLavaConstants;
using static GibsonBot.TheFloorIsLavaUtility;

namespace GibsonBot
{
    internal class TheFloorIsLavaConstants
    {
        public const string ICE_PIECE_SOLID_GAME_OBJECT_NAME = "SafePieceIce(Clone)";
        public const string SAND_PIECE_SOLID_GAME_OBJECT_NAME = "SafePieceSand(Clone)";
        public const string GLASS_PIECE_SOLID_GAME_OBJECT_NAME = "SafePiece(Clone)";
        public const int THE_FLOOR_IS_LAVA_MODE_ID = 8;
        public const float UPDATE_INTERVAL = 0.1f;
        public const float BAD_PLAYER_DISTANCE_MAX_FROM_TILE = 7f;
        public const float TILE_SEARCH_RADIUS = 13.0f;
        public const float MIN_TIME_ON_GROUND = 0.5f;
        public const float MIN_DISTANCE_FROM_TILE_CENTER = 3f;
        public const float COMBAT_PROBABILITY_PER_FRAME = 1f;
        public const float TILE_BREAKING_REACTION_TIME = 0.05f;
    }
    public class TheFloorIsLavaManager : MonoBehaviour
    {
        Vector3 playerPos, closestBadPlayerPos, safeTilePos;
        float distanceToClosestBadPlayer = float.MaxValue;  
        GameObject safeTile = null;
        GameObject lastSafeTile = null;
        GameObject closestTile = null;
        bool isBreaking = false;
        float elapsedBreaking = 0f;
        PlayerManager closestBadPlayer = null;
        private bool isTheFloorIsLavaMode = false;
        public static List<GameObject> allTiles = [];
        private float elapsedUpdate = 0f;
        private float elapsedGrounded = 0f;
        private float distanceToSafeTile = 0f;


        void Awake()
        {
            isTheFloorIsLavaMode = LobbyManager.Instance.gameMode.id == THE_FLOOR_IS_LAVA_MODE_ID;
            if (!isTheFloorIsLavaMode) return;
        }
        void Update()
        {
            if (!isTheFloorIsLavaMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;
            
            // Track elapsed time
            elapsedBreaking += Time.deltaTime;
            elapsedUpdate += Time.deltaTime;

             elapsedGrounded += Time.deltaTime;          


            playerPos = clientBody.transform.position;

            closestTile = GetClosestTile(playerPos, allTiles);


            if (elapsedUpdate >= UPDATE_INTERVAL && elapsedGrounded > MIN_TIME_ON_GROUND && !InputManager.simulateJump && distanceToClosestBadPlayer > BAD_PLAYER_DISTANCE_MAX_FROM_TILE)
            {
                elapsedUpdate = 0f;

                allTiles = GetTheFloorIsLavaTiles();

                closestBadPlayer = FindClosestPlayer(playerPos);

                distanceToClosestBadPlayer = Vector3.Distance(playerPos, closestBadPlayerPos);

                safeTile = GetSafestTileInRadiusOrClosest(playerPos, closestBadPlayerPos, allTiles, TILE_SEARCH_RADIUS); // Find the closest tile to the player


                // Start breaking if we move to a different safe tile
                if (safeTile != lastSafeTile && closestTile != safeTile)
                {
                    isBreaking = true;
                    elapsedGrounded = -1.2f;
                }
            }

            if (safeTile != null)
            {
                safeTilePos = safeTile.transform.position;
                distanceToSafeTile = Vector3.Distance(safeTilePos, playerPos);
            }

            if (closestBadPlayer != null) closestBadPlayerPos = closestBadPlayer.transform.position;

            if (!isBreaking) lastSafeTile = safeTile;


            if (!isBreaking && distanceToSafeTile >= MIN_DISTANCE_FROM_TILE_CENTER && !InputManager.simulateJump)
            {
                InputManager.MoveWithoutObstacle(safeTilePos);
            }

            if (isBreaking)
            {
                if (elapsedBreaking > TILE_BREAKING_REACTION_TIME)
                {
                    InputManager.StartJump(safeTilePos);
                    isBreaking = false;
                }
            }
            else
            {
                // Engage in combat if not breaking
                BotFunctions.Combat(playerPos, closestBadPlayer, COMBAT_PROBABILITY_PER_FRAME);
                elapsedBreaking = 0f;
            }
            
        }
    }

    internal class TheFloorIsLavaUtility
    {
        /// Finds the closest tile to the specified reference point from the list of tiles.
        public static GameObject GetClosestTile(Vector3 referencePoint, List<GameObject> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return null;
            }

            // Filter out null tiles before calculating the closest
            var nonNullTiles = tiles.Where(t => t != null && t.transform != null).ToList();

            if (nonNullTiles.Count == 0)
            {
                return null;
            }

            // Return the closest tile by calculating the distance from the reference point
            GameObject closestTileGameObject = nonNullTiles
                .OrderBy(t => Vector3.Distance(referencePoint, t.transform.position))
                .FirstOrDefault();

            return closestTileGameObject;
        }


        /// Retrieves all valid tiles from the scene.
        public static List<GameObject> GetTheFloorIsLavaTiles()
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            // Filter objects that are considered "SafePiece"
            var filteredTiles = allObjects.Where(obj =>
                obj.name == ICE_PIECE_SOLID_GAME_OBJECT_NAME ||
                obj.name == SAND_PIECE_SOLID_GAME_OBJECT_NAME ||
                obj.name == GLASS_PIECE_SOLID_GAME_OBJECT_NAME).ToList();

            return filteredTiles;
        }

       
        /// Finds the safest tile within a predefined radius around the player. If none are found in the radius, returns the closest safe tile.
        public static GameObject GetSafestTileInRadiusOrClosest(
            Vector3 playerPosition,
            Vector3 closestBadPlayerPos,
            List<GameObject> tiles,
            float searchRadius) // New parameter for the search radius
        {
            // Filter tiles to only include those within the search radius
            var tilesInRadius = tiles.Where(t => Vector3.Distance(playerPosition, t.transform.position) <= searchRadius).ToList();

            // Filter to only include safe tiles within the radius
            var safeTilesInRadius = tilesInRadius.Where(t => {
                var component = t.GetComponent<FloorIsLavaPiece>();
                return component != null && !component.field_Private_Boolean_0;  // Only safe tiles
            }).ToList();

            // Add the current tile if it's safe
            var currentTile = tiles.FirstOrDefault(t => Vector3.Distance(t.transform.position, playerPosition) < MIN_DISTANCE_FROM_TILE_CENTER);
            if (currentTile != null)
            {
                var component = currentTile.GetComponent<FloorIsLavaPiece>();
                if (component != null && !component.field_Private_Boolean_0)
                {
                    safeTilesInRadius.Add(currentTile);  // Add the current tile to the list of safe tiles
                }
            }

            // If there are safe tiles in the radius, return the best option based on neighbors and distance
            if (safeTilesInRadius.Count > 0)
            {
                // Dictionary to store the number of neighbors for each tile
                Dictionary<GameObject, int> neighborCounts = new Dictionary<GameObject, int>();

                // Count the neighbors for each safe tile
                foreach (var tile in safeTilesInRadius)
                {
                    neighborCounts[tile] = safeTilesInRadius.Count(t => IsNeighbor(tile.transform.position, t.transform.position) && tile != t);
                }

                // Sort the tiles by number of neighbors, then by distance to the player
                var sortedTiles = neighborCounts
                    .OrderByDescending(kvp => kvp.Value)  // Sort by number of neighbors (descending)
                    .ThenBy(kvp => Vector3.Distance(playerPosition, kvp.Key.transform.position))  // Sort by proximity to the player
                    .ToList();

                // Loop through the sorted tiles to find the best option
                GameObject bestTile = null;
                float closestDistance = float.MaxValue;

                foreach (var tile in sortedTiles)
                {
                    // Ensure the tile is accessible
                    if (IsAccessibleFromPlayer(playerPosition, tile.Key.transform.position, safeTilesInRadius))
                    {
                        float tileDistanceToPlayer = Vector3.Distance(playerPosition, tile.Key.transform.position);
                        float tileDistanceToEnemy = Vector3.Distance(closestBadPlayerPos, tile.Key.transform.position);

                        // If the tile is farther from the enemy or we don't have a best tile yet, choose it
                        if (tileDistanceToEnemy > BAD_PLAYER_DISTANCE_MAX_FROM_TILE || bestTile == null)
                        {
                            bestTile = tile.Key;
                            closestDistance = tileDistanceToPlayer;
                        }
                        else if (tileDistanceToPlayer == closestDistance && tileDistanceToEnemy > BAD_PLAYER_DISTANCE_MAX_FROM_TILE)
                        {
                            bestTile = tile.Key;
                        }
                    }
                }

                return bestTile; // Return the best safe tile found
            }

            // If no safe tiles are found in the radius, return the closest safe tile, regardless of distance
            var closestSafeTile = tiles
                .Where(t => {
                    var component = t.GetComponent<FloorIsLavaPiece>();
                    return component != null && !component.field_Private_Boolean_0;  // Only safe tiles
                })
                .OrderBy(t => Vector3.Distance(playerPosition, t.transform.position)) // Sort by proximity to the player
                .FirstOrDefault();

            return closestSafeTile; // Return the closest safe tile, or null if none found
        }

        /// Checks if the tile is accessible from the player's current position.
        /// Uses a breadth-first search to check accessibility.
        public static bool IsAccessibleFromPlayer(Vector3 playerPosition, Vector3 targetTilePosition, List<GameObject> tiles)
        {
            Queue<Vector3> queue = new();
            HashSet<Vector3> visited = new();
            queue.Enqueue(playerPosition);

            // Perform breadth-first search to determine if the tile is accessible
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Check if the current tile is the target tile
                if (current == targetTilePosition)
                {
                    return true;
                }

                // Add unvisited neighbors to the queue
                foreach (var neighborTile in tiles)
                {
                    Vector3 neighborPos = neighborTile.transform.position;
                    if (IsNeighbor(current, neighborPos) && !visited.Contains(neighborPos))
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                }
            }

            return false; // Return false if the target tile cannot be reached
        }

        /// Checks if two tiles are neighbors based on a distance threshold.
        public static bool IsNeighbor(Vector3 tilePosition, Vector3 otherTilePosition)
        {
            float maxDistance = TILE_SEARCH_RADIUS; // Adjust based on tile size
            return Vector3.Distance(tilePosition, otherTilePosition) <= maxDistance;
        }
    }
}
