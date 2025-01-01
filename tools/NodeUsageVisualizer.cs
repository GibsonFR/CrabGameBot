
namespace GibsonBot
{
    public class NodeUsageVisualizer : MonoBehaviour
    {
        private bool isPractice = false;  // Check if the game mode is Practice
        private bool isLoading = false;   // To prevent multiple loads at the same time
        private float NODE_SIZE = 1f;

        private readonly Dictionary<Vector3Int, int> nodeUsageDict = new Dictionary<Vector3Int, int>();
        private readonly HashSet<Vector3Int> nodeMapSet = new HashSet<Vector3Int>();
        private List<GameObject> visualizationMarkers = new List<GameObject>();

        void Awake()
        {
            // Check if in Practice mode (assuming mode ID 13 is for Practice)
            isPractice = GetModeId() == 13;
        }

        void Update()
        {
            if (!isPractice) return;  // Only execute in Practice mode

            // When 'J' is pressed, load and visualize node usage
            if (Input.GetKeyDown(KeyCode.J) && !isLoading)
            {
                isLoading = true;
                StartCoroutine(LoadNodeUsageWithColorsAsync($"{nodeMapFolderPath}{mapId}.txt", $"{nodeMapFolderPath}map_{mapId}_nodeUsage.txt").WrapToIl2Cpp());
            }
        }

        /// <summary>
        /// Async function to load node usage and node map, visualizing nodes with colors.
        /// </summary>
        private IEnumerator LoadNodeUsageWithColorsAsync(string nodeMapFilePath, string nodeUsageFilePath)
        {
            // Step 1: Load node usage data from the file
            nodeUsageDict.Clear();
            nodeMapSet.Clear();
            ClearMarkers();  // Clear any previous markers

            if (!File.Exists(nodeUsageFilePath) || !File.Exists(nodeMapFilePath))
            {
                Debug.LogError("Node map or usage file not found.");
                yield break;
            }

            // Load node usage file
            string[] nodeUsageLines = File.ReadAllLines(nodeUsageFilePath);
            foreach (var line in nodeUsageLines)
            {
                string[] parts = line.Split(':');
                string[] coords = parts[0].Split(',');

                Vector3Int node = new Vector3Int(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
                int usage = int.Parse(parts[1]);
                nodeUsageDict[node] = usage;
            }

            // Load the node map file
            string[] nodeMapLines = File.ReadAllLines(nodeMapFilePath);
            foreach (var line in nodeMapLines)
            {
                string[] coords = line.Trim('(', ')').Split(',');
                Vector3Int node = new Vector3Int(int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
                nodeMapSet.Add(node);
            }

            // Step 2: Sort usage values to create percentiles
            List<int> sortedUsageValues = nodeUsageDict.Values.ToList();
            sortedUsageValues.Sort();

            int maxUsage = sortedUsageValues.Last();  // Highest usage value
            int minUsage = sortedUsageValues.First(); // Lowest usage value

            // Set up percentile thresholds for color mapping
            float[] percentiles = new float[] { 0.2f, 0.4f, 0.6f, 0.8f };  // 5 color bands
            Dictionary<float, float> thresholds = new Dictionary<float, float>();

            // Compute the values that correspond to each percentile
            foreach (float percentile in percentiles)
            {
                int index = Mathf.FloorToInt(percentile * sortedUsageValues.Count);
                thresholds[percentile] = sortedUsageValues[index];
            }

            // Step 3: Visualize nodes with colors based on percentiles
            foreach (var node in nodeMapSet)
            {
                Color nodeColor;

                if (nodeUsageDict.ContainsKey(node))
                {
                    // Determine where the node usage fits in the percentiles
                    int usage = nodeUsageDict[node];
                    nodeColor = GetPercentileColor(usage, thresholds, maxUsage);
                }
                else
                {
                    nodeColor = Color.gray;  // Gray for unused nodes
                }

                CreateMarker(node, nodeColor);

                // Yield every 10 nodes to avoid freezing the game
                if (nodeMapSet.Count % 10 == 0)
                {
                    yield return null;
                }
            }

            isLoading = false;  // Reset loading flag once visualization is done
        }

        /// <summary>
        /// Clears any existing visualization markers from the previous run.
        /// </summary>
        private void ClearMarkers()
        {
            foreach (var marker in visualizationMarkers)
            {
                if (marker != null) Destroy(marker);
            }
            visualizationMarkers.Clear();
        }

        /// <summary>
        /// Gets a color based on the node usage percentile, using smooth gradient interpolation.
        /// </summary>
        private Color GetPercentileColor(int usage, Dictionary<float, float> thresholds, int maxUsage)
        {
            float percentile;

            if (usage >= thresholds[0.8f])
            {
                percentile = Mathf.InverseLerp(thresholds[0.8f], maxUsage, usage);  // Map to 80%-100%
                return Color.Lerp(Color.red, Color.red, percentile);  // Pure red for top 20%
            }
            else if (usage >= thresholds[0.6f])
            {
                percentile = Mathf.InverseLerp(thresholds[0.6f], thresholds[0.8f], usage);  // Map to 60%-80%
                return Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, percentile);  // Orange to red
            }
            else if (usage >= thresholds[0.4f])
            {
                percentile = Mathf.InverseLerp(thresholds[0.4f], thresholds[0.6f], usage);  // Map to 40%-60%
                return Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), percentile);  // Yellow to orange
            }
            else if (usage >= thresholds[0.2f])
            {
                percentile = Mathf.InverseLerp(thresholds[0.2f], thresholds[0.4f], usage);  // Map to 20%-40%
                return Color.Lerp(Color.green, Color.yellow, percentile);  // Green to yellow
            }
            else
            {
                percentile = Mathf.InverseLerp(0, thresholds[0.2f], usage);  // Map to 0%-20%
                return Color.Lerp(Color.blue, Color.green, percentile);  // Blue to green
            }
        }


        /// <summary>
        /// Creates a visual marker for the node at the given position with the given color.
        /// </summary>
        private void CreateMarker(Vector3Int nodePosition, Color color)
        {
            Vector3 worldPosition = GridToWorld(nodePosition);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = worldPosition;
            marker.transform.localScale = new Vector3(NODE_SIZE, NODE_SIZE, NODE_SIZE);
            marker.GetComponent<Renderer>().material.color = color;
            visualizationMarkers.Add(marker);
        }

        /// <summary>
        /// Converts grid coordinates to world coordinates.
        /// </summary>
        private Vector3 GridToWorld(Vector3Int gridPos)
        {
            return new Vector3(
            gridPos.x * NODE_SIZE + NODE_SIZE / 2,
            gridPos.y * NODE_SIZE + NODE_SIZE / 2,
                gridPos.z * NODE_SIZE + NODE_SIZE / 2);
        }
    }
}
