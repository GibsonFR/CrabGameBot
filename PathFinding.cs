using static GibsonBot.PathFindingConstants;
using static GibsonBot.PathFindingManager;
using static GibsonBot.PathFindingUtility;

using System.Diagnostics;


namespace GibsonBot
{
    internal class PathFindingConstants
    {
        public const int SEARCH_COST_LOG_INTERVAL = 100; // Log search cost every 100 searches
        public const int NODE_USAGE_LOG_INTERVAL = 100;  // Log node usage every 100 visit
        public const int SNOWBALL_DANGER_RADIUS = 10;
        public const float NODE_SIZE = 1f;
        public const float FEET_OFFSET = -1.61f;
        public const float BODY_OFFSET = 1.1f;
        public const float HEAD_OFFSET = 1.8f;
        public const float DELTA_POSITION_FOR_CALCULATING_PATH = 3f;
        public const float PATH_CALCULATION_DELAY = 0.3f;
        public const float MIN_DISTANCE_TO_JUMP = 15f;
    }

    internal class PathFindingUtility
    {
        private static long totalSearchCost = 0;  // Accumulator for total search cost
        private static int searchCount = 0;       // Counter for the number of pathfinding searches
        private static Dictionary<Vector3Int, int> nodeUsage = new(); // Track node usage across sessions
        private static Queue<Vector3Int> path = new();

        public static Vector3Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / NODE_SIZE),
                Mathf.FloorToInt(worldPos.y / NODE_SIZE),
                Mathf.FloorToInt(worldPos.z / NODE_SIZE));
        }

        public static Vector3 GridToWorld(Vector3Int gridPos)
        {
            return new Vector3(
            gridPos.x * NODE_SIZE + NODE_SIZE / 2,
            gridPos.y * NODE_SIZE + NODE_SIZE / 2,
            gridPos.z * NODE_SIZE + NODE_SIZE / 2);
        }

        // Pathfinding function to move the bot towards the destination using the loaded nodes
        public static void MoveWithPathFinding(Vector3 targetPosition, Vector3 playerPos)
        {
            Vector3Int targetNode = WorldToGrid(targetPosition);
            Vector3Int startNode = WorldToGrid(playerPos);
            
            float initialDistance = Vector3.Distance(targetNode, startNode);

            Vector3Int adjustedTargetNode = FindNearestNode(targetNode);
            Vector3Int adjustedStartNode = FindNearestNode(startNode);

            float adjustedDistance = Vector3.Distance(adjustedTargetNode, adjustedStartNode);

            if (initialDistance > adjustedDistance)
            { 
                targetNode = adjustedTargetNode;
                startNode = adjustedStartNode;
            }

            if (targetNode == Vector3Int.zero || startNode == Vector3Int.zero) return;

            ClearVisualizations();

            // Path calculation if necessary
            if (elapsedPathCalculation >= PATH_CALCULATION_DELAY)
            {
                path = FindPathWithCostTracking(startNode, targetNode, mapId);
            }

            // If the path is empty after calculation go directly to the destination
            if (path == null || path.Count == 0)
            {
                Vector3 destination = targetPosition;
                VisualizeDestination(destination);
                InputManager.Move(destination, playerPos);
                return;
            }

            // Skip nodes that are too close or already at the start node
            if (path.Count > 0 && (path.Peek() == startNode || Vector3.Distance(playerPos, GridToWorld(path.Peek())) < 1.5f))
            {
                path.Dequeue();
            }

            // Follow the calculated path
            if (path.Count > 0)
            {
                Queue<Vector3Int> filteredPath = FilterStackedNodes(path);
                VisualizePath(filteredPath);

                Vector3Int? farthestNodeInLine = GetFarthestAlignedNode(filteredPath);
                if (farthestNodeInLine.HasValue)
                {
                    Vector3 destination = GridToWorld(farthestNodeInLine.Value);
                    VisualizeDestination(destination);
                    InputManager.Move(destination, playerPos);
                    TrackNodeUsage(farthestNodeInLine.Value, mapId);
                }
                else
                {
                    foreach (Vector3Int node in filteredPath)
                    {
                        Vector3 destination = GridToWorld(node);
                        VisualizeDestination(destination);
                        InputManager.Move(destination, playerPos);
                        TrackNodeUsage(node, mapId);
                    }
                }
            }
        }

        // Pathfinding function to bunnyHop the bot towards the destination using the loaded nodes
        public static void BunnyHopWithPathFinding(Vector3 targetPosition, Vector3 playerPos)
        {
            Vector3Int targetNode = WorldToGrid(targetPosition);
            Vector3Int startNode = WorldToGrid(playerPos);

            ClearVisualizations();

            // Check if the target node is loaded
            if (!loadedNodes.ContainsKey(targetNode))
            {
                targetNode = FindNearestNode(targetNode);

                if (targetNode == Vector3Int.zero)
                {
                    targetNode = FindNearestNode(startNode);

                    if (targetNode == Vector3Int.zero)
                    {
                        return;
                    }
                }
            }

            // Check if the start node is loaded
            if (!loadedNodes.ContainsKey(startNode))
            {
                Vector3Int nearestNode = FindNearestNode(startNode);
                startNode = nearestNode;
            }

            // Path calculation if necessary
            if (elapsedPathCalculation >= PATH_CALCULATION_DELAY)
            {
                path = FindPathWithCostTracking(startNode, targetNode, mapId);
            }

            // If the path is empty after calculation go directly to the destination
            if (path == null || path.Count == 0)
            {
                Vector3 destination = targetPosition;

                VisualizeDestination(destination);
                float distanceToTarget = Vector3.Distance(startNode, targetPosition);
                if (distanceToTarget > MIN_DISTANCE_TO_JUMP) InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, true);
                else InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, false);
                return;
            }

            // Skip nodes that are too close or already at the start node
            if (path.Count > 0 && (path.Peek() == startNode || Vector3.Distance(playerPos, GridToWorld(path.Peek())) < 1.5f))
            {
                path.Dequeue();
            }

            // Follow the calculated path
            if (path.Count > 0)
            {
                Queue<Vector3Int> filteredPath = FilterStackedNodes(path);
                VisualizePath(filteredPath);

                Vector3Int? farthestNodeInLine = GetFarthestAlignedNode(filteredPath);
                if (farthestNodeInLine.HasValue)
                {
                    Vector3 destination = GridToWorld(farthestNodeInLine.Value);
                    VisualizeDestination(destination);
                    float distanceToTarget = Vector3.Distance(startNode, targetPosition);
                    if (distanceToTarget > MIN_DISTANCE_TO_JUMP) InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, true);
                    else InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, false);
                    TrackNodeUsage(farthestNodeInLine.Value, mapId);
                }
                else
                {
                    foreach (Vector3Int node in filteredPath)
                    {
                        Vector3 destination = GridToWorld(node);
                        VisualizeDestination(destination);
                        float distanceToTarget = Vector3.Distance(startNode, targetPosition);
                        if (distanceToTarget > MIN_DISTANCE_TO_JUMP) InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, true);
                        else InputManager.MoveWithBunnyHopForPathFinding(destination, playerPos, false);
                        TrackNodeUsage(node, mapId);
                    }
                }
            }
        }

        // Track pathfinding search costs and log them periodically
        public static Queue<Vector3Int> FindPathWithCostTracking(Vector3Int start, Vector3Int target, int mapId)
        {
            elapsedPathCalculation = 0f;
            Stopwatch stopwatch = Stopwatch.StartNew();  // Start timing the pathfinding process
            Queue<Vector3Int> path = FindPath(start, target);  // Call the pathfinding algorithm
            stopwatch.Stop();

            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;  // Get the time taken
            totalSearchCost += elapsedMilliseconds;  // Add to total search time
            searchCount++;

            // Log the average search cost every SEARCH_COST_LOG_INTERVAL searches
            if (searchCount % SEARCH_COST_LOG_INTERVAL == 0)
            {
                WriteAverageSearchCost(mapId);
            }

            return path;
        }

        // Write the average search cost to a file specific to the map
        private static void WriteAverageSearchCost(int mapId)
        {
            string filePath = $"{nodeMapFolderPath}map_{mapId}_searchCost.txt";
            long averageCost = totalSearchCost / searchCount;

            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine($"Search count: {searchCount}, Average cost: {averageCost} ms");
            }

            // Reset the counters
            totalSearchCost = 0;
            searchCount = 0;
        }

        // Track node usage and periodically log the results
        public static void TrackNodeUsage(Vector3Int node, int mapId)
        {
            if (nodeUsage.ContainsKey(node))
            {
                nodeUsage[node]++;
            }
            else
            {
                nodeUsage[node] = 1;
            }
        }

        // Load node usage data from a file
        public static void LoadNodeUsageData(int mapId)
        {
            string filePath = $"{nodeMapFolderPath}map_{mapId}_nodeUsage.txt";
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (TryParseNodeUsage(line, out Vector3Int node, out int usage))
                    {
                        if (nodeUsage.ContainsKey(node))
                        {
                            nodeUsage[node] += usage;
                        }
                        else
                        {
                            nodeUsage[node] = usage;
                        }
                    }
                }
            }
        }

        // Write node usage data to a file
        public static void WriteNodeUsageData(int mapId)
        {
            string filePath = $"{nodeMapFolderPath}map_{mapId}_nodeUsage.txt";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var entry in nodeUsage)
                {
                    writer.WriteLine($"{entry.Key.x},{entry.Key.y},{entry.Key.z}:{entry.Value}");
                }
            }
        }

        // Parse a node usage line from the file
        private static bool TryParseNodeUsage(string line, out Vector3Int node, out int usage)
        {
            string[] parts = line.Split(':');
            node = Vector3Int.zero;
            usage = 0;

            if (parts.Length == 2)
            {
                string[] nodeCoords = parts[0].Split(',');
                if (nodeCoords.Length == 3 &&
                    int.TryParse(nodeCoords[0], out int x) &&
                    int.TryParse(nodeCoords[1], out int y) &&
                    int.TryParse(nodeCoords[2], out int z) &&
                    int.TryParse(parts[1], out usage))
                {
                    node = new Vector3Int(x, y, z);
                    return true;
                }
            }

            return false;
        }
        private static void VisualizeDestination(Vector3 destination)
        {
            VisualizeDestinationNode(destination, Color.yellow);
        }
        private static void VisualizePath(Queue<Vector3Int> path)
        {
            foreach (Vector3Int node in path)
            {
                VisualizePathNode(node, Color.blue, NODE_SIZE * 0.5f); // Visualize path nodes in blue
            }
        }

        private static void VisualizePathNode(Vector3Int nodePosition, Color color, float nodeSize)
        {
            Vector3 worldPosition = GridToWorld(nodePosition);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = worldPosition;
            marker.transform.localScale = new Vector3(nodeSize, nodeSize, nodeSize);
            marker.GetComponent<Renderer>().material.color = color;
            visualizationMarkers.Add(marker);
        }

        private static void VisualizeDestinationNode(Vector3 nodePosition, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = nodePosition;
            marker.transform.localScale = new Vector3(NODE_SIZE, NODE_SIZE, NODE_SIZE);
            marker.GetComponent<Renderer>().material.color = color;
            visualizationMarkers.Add(marker);
        }

        private static void ClearVisualizations()
        {
            foreach (GameObject marker in visualizationMarkers)
            {
                GameObject.Destroy(marker);
            }
            visualizationMarkers.Clear();
        }

        private static Queue<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
        {
            Queue<Vector3Int> path = new();
            HashSet<Vector3Int> visitedNodes = new();
            Queue<Vector3Int> frontier = new();
            Dictionary<Vector3Int, Vector3Int?> cameFrom = new();

            frontier.Enqueue(start);
            visitedNodes.Add(start);
            cameFrom[start] = null;

            while (frontier.Count > 0)
            {
                Vector3Int currentNode = frontier.Dequeue();

                if (currentNode == target)
                {
                    break;
                }

                foreach (Vector3Int neighbor in GetNeighbors(currentNode))
                {
                    if (!visitedNodes.Contains(neighbor) && loadedNodes.ContainsKey(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        visitedNodes.Add(neighbor);
                        cameFrom[neighbor] = currentNode;
                    }
                }
            }

            // Reconstruct path
            if (!cameFrom.ContainsKey(target))
            {
                return null;
            }

            Vector3Int? currentStep = target;
            while (currentStep.HasValue)
            {
                path.Enqueue(currentStep.Value);
                TrackNodeUsage(currentStep.Value, LobbyManager.Instance.map.id);
                currentStep = cameFrom[currentStep.Value];
            }

            return new Queue<Vector3Int>(new Stack<Vector3Int>(path));
        }

        private static List<Vector3Int> GetNeighbors(Vector3Int node)
        {
            List<Vector3Int> neighbors = new();
            neighbors.Add(new Vector3Int(node.x + 1, node.y, node.z));
            neighbors.Add(new Vector3Int(node.x - 1, node.y, node.z));
            neighbors.Add(new Vector3Int(node.x, node.y + 1, node.z));
            neighbors.Add(new Vector3Int(node.x, node.y - 1, node.z));
            neighbors.Add(new Vector3Int(node.x, node.y, node.z + 1));
            neighbors.Add(new Vector3Int(node.x, node.y, node.z - 1));
            return neighbors;
        }

        private static Vector3Int FindNearestNode(Vector3Int target)
        {
            Vector3Int closestNode = Vector3Int.zero;
            float closestDistance = float.MaxValue;

            foreach (var node in loadedNodes.Keys)
            {
                float distance = Vector3Int.Distance(target, node);
                if (distance < closestDistance)
                {
                    closestNode = node;
                    closestDistance = distance;
                }
            }

            return closestNode;
        }

        private static Vector3Int? GetFarthestAlignedNode(Queue<Vector3Int> path)
        {
            if (path.Count < 2)
            {
                return null;
            }

            Vector3Int[] pathArray = path.ToArray();
            Vector3Int direction = pathArray[1] - pathArray[0];
            Vector3Int lastAlignedNode = pathArray[0];

            for (int i = 1; i < pathArray.Length; i++)
            {
                Vector3Int currentDirection = pathArray[i] - lastAlignedNode;
                if (currentDirection == direction)
                {
                    lastAlignedNode = pathArray[i];
                }
                else
                {
                    break;
                }
            }

            // Vérifiez si lastAlignedNode a une valeur avant de la retourner
            return pathArray.Length > 0 ? (Vector3Int?)lastAlignedNode : null;
        }

        private static Queue<Vector3Int> FilterStackedNodes(Queue<Vector3Int> path)
        {
            Queue<Vector3Int> filteredPath = new();

            Vector3Int? previousNode = null;

            foreach (Vector3Int node in path)
            {
                if (previousNode != null)
                {
                    if (node.x == previousNode.Value.x && node.z == previousNode.Value.z && node.y != previousNode.Value.y)
                    {
                        continue;
                    }
                }

                filteredPath.Enqueue(node);
                previousNode = node;
            }

            return filteredPath;
        }

        public static void LoadNodMapFromFile(string path)
        {
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (TryParseVector3Int(line, out Vector3Int nodePosition))
                    {
                        loadedNodes[nodePosition] = true;
                    }
                }
            }
        }

        private static bool TryParseVector3Int(string value, out Vector3Int result)
        {
            result = Vector3Int.zero;
            string[] parts = value.Trim('(', ')').Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int x) &&
                int.TryParse(parts[1], out int y) &&
                int.TryParse(parts[2], out int z))
            {
                result = new Vector3Int(x, y, z);
                return true;
            }
            return false;
        }

        public static Vector3Int FindSnowballSafePosition(Vector3Int startNode, Dictionary<Vector3, float> dangerousSnowballs, Dictionary<Vector3, Vector3> dangerousNodesDirections, Vector3 playerPos)
        {
            ClearVisualizations();
            // Ensure the startNode is within loadedNodes, or find the nearest one
            startNode = FindNearestNode(startNode);

            // If no reachable node is found, return zero
            if (startNode == Vector3Int.zero)
            {
                return Vector3Int.zero;
            }

            // Create a hash set to store the nodes marked as dangerous
            HashSet<Vector3Int> dangerousNodes = new HashSet<Vector3Int>();

            // Mark all nodes within a dangerous radius of 5 around each snowball impact
            foreach (var snowball in dangerousSnowballs)
            {
                Vector3 impactPosition = snowball.Key;
                Vector3Int impactNode = WorldToGrid(impactPosition);
                MarkDangerousNodes(impactNode, dangerousNodesDirections[snowball.Key], SNOWBALL_DANGER_RADIUS, dangerousNodes, playerPos);
            }

            // Start searching for a safe node from the starting node
            Vector3Int safeNode = FindClosestSafeNodeWithExpansion(startNode, dangerousNodes, dangerousNodesDirections);

            // Return the closest safe node found or Vector3Int.zero if none is found
            return safeNode;
        }
        private static void MarkDangerousNodes(Vector3Int impactNode, Vector3 horizontalDirection, int radius, HashSet<Vector3Int> dangerousNodes, Vector3 playerPosition)
        {
            Vector3 directionNormalized = horizontalDirection.normalized;
            Vector3 oppositeDirection = -directionNormalized;
            Vector3Int playerNode = WorldToGrid(playerPosition);

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        Vector3Int node = new Vector3Int(impactNode.x + x, impactNode.y + y, impactNode.z + z);

                        // Skip nodes that are not in loadedNodes
                        if (!loadedNodes.ContainsKey(node)) continue;

                        Vector3 directionToNode = (new Vector3(node.x, 0, node.z) - new Vector3(impactNode.x, 0, impactNode.z)).normalized;
                        float dotProduct = Vector3.Dot(directionToNode, directionNormalized);
                        float dotProductOpposite = Vector3.Dot(directionToNode, oppositeDirection);

                        // Mark nodes in the forward direction of the snowball or behind the player
                        if (dotProduct > 0.5f) // In the snowball's direction
                        {
                            dangerousNodes.Add(node);
                            VisualizePathNode(node, Color.red, NODE_SIZE);
                        }
                        else if (dotProductOpposite > 0.5f && node.z < playerNode.z) // Behind the player
                        {
                            dangerousNodes.Add(node);
                            VisualizePathNode(node, Color.red, NODE_SIZE);
                        }
                    }
                }
            }
        }

        // Find the closest safe node using an expanding search pattern
        private static Vector3Int FindClosestSafeNodeWithExpansion(Vector3Int startNode, HashSet<Vector3Int> dangerousNodes, Dictionary<Vector3, Vector3> dangerousNodesDirections)
        {
            // Queue for BFS-like expanding search
            Queue<Vector3Int> frontier = new Queue<Vector3Int>();
            HashSet<Vector3Int> visitedNodes = new HashSet<Vector3Int>();

            frontier.Enqueue(startNode);
            visitedNodes.Add(startNode);

            // Define the maximum search radius (optional: to limit the expansion)
            int maxSearchRadius = 15;

            // Start the search with an expanding frontier
            while (frontier.Count > 0)
            {
                Vector3Int currentNode = frontier.Dequeue();

                // Check if the node is safe, in loadedNodes, and doesn't cross a dangerous line of fire
                if (!dangerousNodes.Contains(currentNode)
                    && loadedNodes.ContainsKey(currentNode)
                    && !IsCrossingDangerLine(startNode, currentNode, dangerousNodesDirections))
                {
                    return currentNode;  // Safe node found
                }

                // Get neighbors of the current node and expand the search
                foreach (Vector3Int neighbor in GetNeighbors(currentNode))
                {
                    if (!visitedNodes.Contains(neighbor) && loadedNodes.ContainsKey(neighbor) && Vector3Int.Distance(startNode, neighbor) <= maxSearchRadius)
                    {
                        frontier.Enqueue(neighbor);
                        visitedNodes.Add(neighbor);
                    }
                }
            }

            // Return zero if no safe node is found within the max search radius
            return Vector3Int.zero;
        }

        // Helper function to check if the path crosses a snowball's danger line
        private static bool IsCrossingDangerLine(Vector3Int startNode, Vector3Int destinationNode, Dictionary<Vector3, Vector3> dangerousNodesDirections)
        {
            foreach (var snowballDirection in dangerousNodesDirections)
            {
                Vector3 snowballImpactPosition = snowballDirection.Key;
                Vector3 snowballDirectionVector = snowballDirection.Value.normalized;

                Vector3Int snowballImpactNode = WorldToGrid(snowballImpactPosition);

                // Calculate the vector from the snowball impact point to the destination node
                Vector3 directionToNode = (new Vector3(destinationNode.x, 0, destinationNode.z) - new Vector3(snowballImpactNode.x, 0, snowballImpactNode.z)).normalized;

                // If the direction from the snowball impact point to the destination aligns with the snowball's direction, it's crossing a danger line
                float dotProduct = Vector3.Dot(directionToNode, snowballDirectionVector);
                if (dotProduct > 0.7f) // Threshold to consider it as aligned with the snowball's direction (adjust as necessary)
                {
                    // The path to the destination crosses the snowball's line of fire
                    return true;
                }
            }

            return false;
        }

        public static Vector3Int FindNodeAwayFromThreats(Vector3 worldPosition, List<Vector3> threatPositions, float radius)
        {
            Vector3Int startNode = WorldToGrid(worldPosition);

            if (!loadedNodes.ContainsKey(startNode))
            {
                startNode = FindNearestNode(startNode);  
                if (!loadedNodes.ContainsKey(startNode))
                {
                    return Vector3Int.zero; 
                }
            }

            HashSet<Vector3Int> dangerousNodes = new HashSet<Vector3Int>();
            MarkDangerousNodes(threatPositions, dangerousNodes, radius);

            Vector3Int safeNode = FindClosestSafeNodeWithExpansion(startNode, dangerousNodes);

            return safeNode != Vector3Int.zero ? safeNode : startNode;
        }

        private static void MarkDangerousNodes(List<Vector3> threatPositions, HashSet<Vector3Int> dangerousNodes, float radius)
        {
            foreach (Vector3 threatPosition in threatPositions)
            {
                Vector3Int impactNode = WorldToGrid(threatPosition);

                for (int x = -Mathf.CeilToInt(radius); x <= Mathf.CeilToInt(radius); x++)
                {
                    for (int y = -Mathf.CeilToInt(radius); y <= Mathf.CeilToInt(radius); y++)
                    {
                        for (int z = -Mathf.CeilToInt(radius); z <= Mathf.CeilToInt(radius); z++)
                        {
                            Vector3Int node = new Vector3Int(impactNode.x + x, impactNode.y + y, impactNode.z + z);

                            if (Vector3Int.Distance(impactNode, node) <= radius && loadedNodes.ContainsKey(node))
                            {
                                dangerousNodes.Add(node);
                            }
                        }
                    }
                }
            }
        }

        private static Vector3Int FindClosestSafeNodeWithExpansion(Vector3Int startNode, HashSet<Vector3Int> dangerousNodes)
        {
            Queue<Vector3Int> frontier = new Queue<Vector3Int>();
            HashSet<Vector3Int> visitedNodes = new HashSet<Vector3Int>();

            frontier.Enqueue(startNode);
            visitedNodes.Add(startNode);

            while (frontier.Count > 0)
            {
                Vector3Int currentNode = frontier.Dequeue();

                if (!dangerousNodes.Contains(currentNode) && loadedNodes.ContainsKey(currentNode))
                {
                    return currentNode;  
                }

                // Explorer les voisins
                foreach (Vector3Int neighbor in GetNeighbors(currentNode))
                {
                    if (!visitedNodes.Contains(neighbor) && loadedNodes.ContainsKey(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        visitedNodes.Add(neighbor);
                    }
                }
            }

            return Vector3Int.zero;  
        }

        public static Vector3Int FindNodeAwayFromThreat(Vector3 worldPosition, Vector3 threatPosition, float radius)
        {
            Vector3Int startNode = WorldToGrid(worldPosition);
            startNode = FindNearestNode(startNode);

            Queue<Vector3Int> frontier = new Queue<Vector3Int>();
            HashSet<Vector3Int> visitedNodes = new HashSet<Vector3Int>();

            frontier.Enqueue(startNode);
            visitedNodes.Add(startNode);


            Vector3 directionToThreat = threatPosition - worldPosition;
            directionToThreat.Normalize(); 

            while (frontier.Count > 0)
            {
                Vector3Int currentNode = frontier.Dequeue();

                float currentDistance = Vector3Int.Distance(startNode, currentNode);

                if (currentDistance >= radius)
                {

                    Vector3 directionToNode = GridToWorld(currentNode) - worldPosition;
                    directionToNode.Normalize(); 

                    float dotProduct = Vector3.Dot(directionToThreat, directionToNode);

                    if (dotProduct < 0) 
                    {
                        return currentNode; 
                    }
                }

                foreach (Vector3Int neighbor in GetNeighbors(currentNode))
                {
                    if (!visitedNodes.Contains(neighbor) && loadedNodes.ContainsKey(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        visitedNodes.Add(neighbor);
                    }
                }
            }

            // Si aucun nœud n'est trouvé respectant les critères, retourner le nœud de départ
            return startNode;
        }
    }


    internal class PathFindingManager : MonoBehaviour
    {
        public static Dictionary<Vector3Int, bool> loadedNodes = new();
        public static List<GameObject> visualizationMarkers = new();
        public static float elapsedPathCalculation = 0;
        private bool saveNodeUsageData;

        void Awake()
        {
            int mapId = LobbyManager.Instance.map.id;
            LoadNodMapFromFile($"{nodeMapFolderPath}{mapId}.txt");

            LoadNodeUsageData(mapId);

            NodeMapCleaner.CleanAndOptimizeNodeMap($"{nodeMapFolderPath}{mapId}.txt", $"{nodeMapFolderPath}{mapId}O.txt");
            NodeMapCleaner.GenerateNodeMapFromUsage($"{nodeMapFolderPath}map_{mapId}_nodeUsage.txt", $"{nodeMapFolderPath}{mapId}.txt", $"{nodeMapFolderPath}{mapId}U.txt");
        }

        void Update()
        {
            elapsedPathCalculation += Time.deltaTime;
            if (!saveNodeUsageData && (GetGameState() == "Ended" || GetGameState() == "GameOver"))
            {
                WriteNodeUsageData(mapId);
                saveNodeUsageData = true;
            }
        }
    }
}
