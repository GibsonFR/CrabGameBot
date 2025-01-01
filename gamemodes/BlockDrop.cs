using static GibsonBot.BlockDropUtility;
using static GibsonBot.BlockDropConstants;
using static GibsonBot.BlockDropManager;

namespace GibsonBot
{
    internal class BlockDropConstants
    {
        public const string FALLING_BLOCK_GAME_OBJECT_NAME = "FallBlock(Clone)";
        public const float UPDATE_INTERVAL = 0.1f;
        public const float COMBAT_PROBABILITY_PER_FRAME = 0.05f;
        public static readonly int[] NEIGHBOUR_OFFSETS = [0, -1, 1, -4, 4, -5, 5, -3, 3];
        public static readonly int[] CENTRAL_COLUMNS_INDEX = [5, 6, 9, 10];
        public const int TOTAL_COLUMN_COUNT = 16;
        public const int BLOCK_DROP_MODE_ID = 15;
        public const int MAP_WIDTH = 64;
        public const int COLUMN_COUNT_PER_ROW = 4;
        public const int DIAGONAL_LONG_OFFSET = 5;
        public const int DIAGONAL_SHORT_OFFSET = 3; 
        public const int MAX_COLUMN_INDEX_IN_ROW = 3; 
        public const int VERTICAL_COLUMN_OFFSET = 4; 
        public const int HORIZONTAL_COLUMN_OFFSET_POS = 1; 
        public const int HORIZONTAL_COLUMN_OFFSET_NEG = -1;
        public const float MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP = 10f;
    }

    public class BlockDropManager : MonoBehaviour
    {
        public static Dictionary<int, bool> columnStatus = [];
        public static Dictionary<int, int> blocksPerColumn = [];
        public static List<int> processedBlocks = [];

        private static Vector3 playerPos, safePosition, closestBadPlayerPos;
        private static float columnWidth, elapsedUpdate;
        private static PlayerManager closestBadPlayer = null;
        private bool isBlockDropMode = false;
        private float distanceFromSafePos;

        void Awake()
        {
            isBlockDropMode = LobbyManager.Instance.gameMode.id == BLOCK_DROP_MODE_ID;

            if (!isBlockDropMode) return;

            for (int i = 0; i < TOTAL_COLUMN_COUNT; i++)
            {
                columnStatus[i] = true;
                blocksPerColumn[i] = 0;
            }

            columnWidth = MAP_WIDTH / COLUMN_COUNT_PER_ROW;
        }

        void Update()
        {
            if (!isBlockDropMode || !isClientBot ||  IsGameStateFreeze() || clientBody == null) return;
            
            elapsedUpdate += Time.deltaTime;

            playerPos = clientBody.transform.position;

            if (elapsedUpdate > UPDATE_INTERVAL)
            {
                elapsedUpdate = 0f;
                UpdateColumnStatus(columnWidth);
                int currentColumn = DetermineColumnIndex(playerPos, columnWidth);
                int currentHeight = columnStatus[currentColumn] == false ? blocksPerColumn[currentColumn] - 1 : blocksPerColumn[currentColumn];

                safePosition = FindSafeNeighbour(currentColumn, currentHeight, playerPos, columnWidth);

                closestBadPlayer = FindClosestPlayer(playerPos);
                if (closestBadPlayer != null) closestBadPlayerPos = closestBadPlayer.transform.position;
            }

            if (safePosition != Vector3.zero)
            {
                distanceFromSafePos = Vector3.Distance(playerPos, safePosition);

                if (distanceFromSafePos > 3) InputManager.MoveWithBunnyHop(safePosition, playerPos, MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP);        
                else BotFunctions.Combat(playerPos, closestBadPlayer, COMBAT_PROBABILITY_PER_FRAME);

            }
        }
    }

    internal class BlockDropUtility
    {
        public static int DetermineColumnIndex(Vector3 position, float columnWidth)
        {
            int col = Mathf.FloorToInt((position.x + (MAP_WIDTH / 2)) / columnWidth);
            int row = Mathf.FloorToInt((position.z + (MAP_WIDTH / 2)) / columnWidth);
            return col + row * COLUMN_COUNT_PER_ROW;
        }

        public static List<GameObject> GetFallingBlocks()
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            return allObjects.Where(obj => obj.name == FALLING_BLOCK_GAME_OBJECT_NAME).ToList();
        }

        public static void UpdateColumnStatus(float columnWidth)
        {
            var fallingBlocks = GetFallingBlocks();

            // Marquer d'abord toutes les colonnes comme sûres
            for (int i = 0; i < TOTAL_COLUMN_COUNT; i++)
            {
                columnStatus[i] = true;
            }

            // Parcourir tous les blocs tombants
            foreach (var block in fallingBlocks)
            {
                int blockID = block.GetInstanceID();
                int columnIndex = DetermineColumnIndex(block.transform.position, columnWidth);


                columnStatus[columnIndex] = false;

                if (!processedBlocks.Contains(blockID))
                {
                    processedBlocks.Add(blockID);
                    blocksPerColumn[columnIndex]++;
                }
            }
        }

        public static Vector3 FindSafeNeighbour(int currentColumn, int currentHeight, Vector3 playerPos, float columnWidth)
        {
            Vector3 bestPosition = CalculateColumnCenter(currentColumn, playerPos, columnWidth); 
            int bestColumnIndex = currentColumn;

            List<(int ColumnIndex, Vector3 Position)> higherColumns = [];
            List<(int ColumnIndex, Vector3 Position)> sameLevelColumns = [];
            List<(int ColumnIndex, Vector3 Position)> lowerColumns = [];

            foreach (int offset in NEIGHBOUR_OFFSETS)
            {
                int neighbourColumn = currentColumn + offset;

                if (blocksPerColumn.ContainsKey(neighbourColumn) && columnStatus.ContainsKey(neighbourColumn) && columnStatus[neighbourColumn])
                {
                    int neighbourHeight = columnStatus[neighbourColumn] == false ? blocksPerColumn[neighbourColumn] - 1 : blocksPerColumn[neighbourColumn];

                    if (neighbourHeight == currentHeight + 1 && !IsBlockedByWall(currentColumn, neighbourColumn, currentHeight))
                    {
                        Vector3 potentialPosition = CalculateColumnCenter(neighbourColumn, playerPos, columnWidth);
                        if (IsPositionSafe(potentialPosition, columnWidth))
                        {
                            higherColumns.Add((neighbourColumn, potentialPosition));  
                        }
                    }
                    else if (neighbourHeight == currentHeight && !IsBlockedByWall(currentColumn, neighbourColumn, currentHeight))
                    {
                        Vector3 potentialPosition = CalculateColumnCenter(neighbourColumn, playerPos, columnWidth);
                        if (IsPositionSafe(potentialPosition, columnWidth))
                        {
                            sameLevelColumns.Add((neighbourColumn, potentialPosition));  
                        }
                    }
                    else if (neighbourHeight < currentHeight && !IsBlockedByWall(currentColumn, neighbourColumn, currentHeight))
                    {
                        Vector3 potentialPosition = CalculateColumnCenter(neighbourColumn, playerPos, columnWidth);
                        if (IsPositionSafe(potentialPosition, columnWidth))
                        {
                            lowerColumns.Add((neighbourColumn, potentialPosition));  
                        }
                    }
                }
            }

            if (higherColumns.Count > 0)
            {
                var bestHigherColumn = higherColumns
                    .OrderBy(col => CalculateDistanceToCenter(col.ColumnIndex)) 
                    .First();
                bestPosition = bestHigherColumn.Position;
                bestColumnIndex = bestHigherColumn.ColumnIndex;
            }
            else if (sameLevelColumns.Count > 0)
            {
                var bestSameLevelColumn = sameLevelColumns
                    .OrderBy(col => CalculateDistanceToCenter(col.ColumnIndex)) 
                    .First();
                bestPosition = bestSameLevelColumn.Position;
                bestColumnIndex = bestSameLevelColumn.ColumnIndex;
            }
            else if (lowerColumns.Count > 0)
            {
                var bestLowerColumn = lowerColumns
                    .OrderBy(col => CalculateDistanceToCenter(col.ColumnIndex)) 
                    .First();
                bestPosition = bestLowerColumn.Position;
                bestColumnIndex = bestLowerColumn.ColumnIndex;
            }

            return bestPosition;
        }

        public static int CalculateDistanceToCenter(int columnIndex)
        {
            return CENTRAL_COLUMNS_INDEX.Min(centerCol => Mathf.Abs(columnIndex - centerCol));
        }

        public static bool IsBlockedByWall(int fromColumn, int toColumn, int currentHeight)
        {
            if (Mathf.Abs(fromColumn - toColumn) == DIAGONAL_LONG_OFFSET || Mathf.Abs(fromColumn - toColumn) == DIAGONAL_SHORT_OFFSET)
            {
                int horizontalOffset = (fromColumn % 4 != MAX_COLUMN_INDEX_IN_ROW) ? HORIZONTAL_COLUMN_OFFSET_POS : HORIZONTAL_COLUMN_OFFSET_NEG;
                int verticalOffset = (fromColumn < toColumn) ? VERTICAL_COLUMN_OFFSET : -VERTICAL_COLUMN_OFFSET;

                int adjacentColumn1 = fromColumn + horizontalOffset;
                int adjacentColumn2 = fromColumn + verticalOffset;

                if (blocksPerColumn.ContainsKey(adjacentColumn1) && blocksPerColumn.ContainsKey(adjacentColumn2))
                {
                    return (columnStatus[adjacentColumn1] == false ? blocksPerColumn[adjacentColumn1] - 1 : blocksPerColumn[adjacentColumn1]) > currentHeight &&
                           (columnStatus[adjacentColumn2] == false ? blocksPerColumn[adjacentColumn2] - 1 : blocksPerColumn[adjacentColumn2]) > currentHeight;
                }
            }

            return false;
        }

        public static bool IsPositionSafe(Vector3 position, float columnWidth)
        {
            int columnIndex = DetermineColumnIndex(position, columnWidth);
            return columnStatus.ContainsKey(columnIndex) && columnStatus[columnIndex];
        }

        public static Vector3 CalculateColumnCenter(int columnIndex, Vector3 playerPos, float columnWidth)
        {
            int column = columnIndex % COLUMN_COUNT_PER_ROW; 
            int row = columnIndex / COLUMN_COUNT_PER_ROW;
            return new Vector3((column * columnWidth) - ((MAP_WIDTH / 2) - (columnWidth / 2)), playerPos.y, (row * columnWidth) - ((MAP_WIDTH / 2) - (columnWidth / 2)));
        }
    }

}
