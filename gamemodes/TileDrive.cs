using static GibsonBot.TileDriveConstants;
using static GibsonBot.TileDriveUtility;
using static GibsonBot.PathFindingUtility;
using static GibsonBot.PathFindingManager;

namespace GibsonBot
{
    internal class TileDriveConstants
    {
        public const string TILE_GAME_OBJECT_NAME_PREFIX = "NewTile";
        public const string TEAM_COLOR_TMP_GAME_OBJECT_NAME = "GameUI/Status/TopRight/TileDrive/Alert/Alert (1)/Header (1)/Text (TMP)";
        public const int TILE_DRIVE_MODE_ID = 9;
        public const float TILE_MAX_DISTANCE_FROM_NODE = 10f;
        public const float FIND_NEW_TILE_WAIT_TIME = 0.2f;
        public const float TILE_MIN_DISTANCE_TO_JUMP = 1.5f;
    }

    public class TileDriveManager : MonoBehaviour
    {
        private bool init;
        private bool isTileDriveMode;
        private List<GameObject> allTiles;
        private GameObject targetTile;
        private Vector3 playerPos, targetTilePos;
        private float distanceTargetTile;
        private int teamId = -1;
        private float elapsedFindNewTile;


        void Awake()
        {
            isTileDriveMode = LobbyManager.Instance.gameMode.id == TILE_DRIVE_MODE_ID;
        }

        void Update()
        {
            if (!isTileDriveMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedFindNewTile += Time.deltaTime;

            playerPos = clientBody.transform.position;
            if (!init)
            {
                init = true;

                allTiles = GetTileDriveTiles();
                teamId = GetTeamId();
            }

            if (elapsedFindNewTile >= FIND_NEW_TILE_WAIT_TIME) targetTile = FindClosestAccessibleTileWithDifferentColor(allTiles, playerPos, teamId);

            if (targetTile != null)
            {
                targetTilePos = targetTile.transform.position;

                distanceTargetTile = Vector3.Distance(targetTilePos, playerPos);

                MoveWithPathFinding(targetTilePos, playerPos);

                if (distanceTargetTile < TILE_MIN_DISTANCE_TO_JUMP) clientMovement.Jump();              
            }
        }    
    }

    internal class TileDriveUtility
    {
        public static List<GameObject> GetTileDriveTiles()
        {
            List<GameObject> foundObjects = [];

            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.name.StartsWith(TILE_GAME_OBJECT_NAME_PREFIX)) foundObjects.Add(obj);      
            }
            return foundObjects;
        }

        public static int GetTeamId()
        {
            string teamColor = GameObject.Find(TEAM_COLOR_TMP_GAME_OBJECT_NAME).GetComponent<TMPro.TextMeshProUGUI>().text.ToLower();
            if (teamColor == null) return -1;
            return teamColor switch
            {
                "team red" => 0,
                "team blue" => 1,
                "team green" => 2,
                "team pink" => 3,
                _ => -1,
            };
        }

        public static GameObject FindClosestAccessibleTileWithDifferentColor(List<GameObject> tiles, Vector3 playerPos, int playerTeamId)
        {
            GameObject closestTile = null;
            float minDistance = float.MaxValue;

            foreach (GameObject tile in tiles)
            {
                TileDriveTile tileComponent = tile.GetComponent<TileDriveTile>();

                if (tileComponent == null) continue;

                int tileColor = tileComponent.prop_Int32_0;

                if (tileColor != playerTeamId && Mathf.Abs(tile.transform.position.y - playerPos.y) <= 4)
                {

                    if (IsTileNearNode(tile.transform.position))
                    {

                        float distance = Vector3.Distance(tile.transform.position, playerPos);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestTile = tile;
                        }
                    }
                }
            }

            return closestTile;
        }

        private static bool IsTileNearNode(Vector3 tilePosition)
        {
            foreach (var node in loadedNodes.Keys)
            {
                if (Vector3.Distance(GridToWorld(node), tilePosition) <= TILE_MAX_DISTANCE_FROM_NODE) return true;
            }
            return false;
        }
    }
}
