namespace GibsonBot
{
    internal class NodeMapCleaner
    {
        public static void GenerateNodeMapFromUsage(string nodeUsagePath, string nodeMapPath, string outputPath)
        {
            // Step 1: Check if nodeUsage file exists
            if (!File.Exists(nodeUsagePath))
            {
                return;  // Exit the function if the file is not found
            }

            // Step 2: Check if nodeMap file exists
            if (!File.Exists(nodeMapPath))
            {
                return;  // Exit the function if the file is not found
            }

            // Step 3: Read the nodeUsage file and store node usage data
            Dictionary<(int, int, int), int> nodeUsageDict = new Dictionary<(int, int, int), int>();
            string[] nodeUsageLines = File.ReadAllLines(nodeUsagePath);

            foreach (var line in nodeUsageLines)
            {
                string[] parts = line.Split(':');
                string[] coords = parts[0].Split(',');

                // Parse coordinates and usage
                (int x, int y, int z) node = (int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
                int usage = int.Parse(parts[1]);

                nodeUsageDict[node] = usage;
            }

            // Step 4: Load the nodeMap from the provided nodeMap file
            HashSet<(int, int, int)> nodeMap = new HashSet<(int, int, int)>();
            string[] nodeMapLines = File.ReadAllLines(nodeMapPath);

            foreach (var line in nodeMapLines)
            {
                string[] coords = line.Trim('(', ')').Split(',');
                (int x, int y, int z) node = (int.Parse(coords[0]), int.Parse(coords[1]), int.Parse(coords[2]));
                nodeMap.Add(node);
            }

            // Step 5: Sort the nodes based on usage
            var sortedNodes = new List<(int, int, int)>(nodeMap);
            sortedNodes.Sort((node1, node2) =>
            {
                int usage1 = nodeUsageDict.ContainsKey(node1) ? nodeUsageDict[node1] : 0;
                int usage2 = nodeUsageDict.ContainsKey(node2) ? nodeUsageDict[node2] : 0;
                return usage2.CompareTo(usage1);  // Sort in descending order of usage
            });

            // Step 6: Write the sorted nodes to the new output file
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (var node in sortedNodes)
                {
                    writer.WriteLine($"({node.Item1}, {node.Item2}, {node.Item3})");
                }
            }
        }

        public static void CleanAndOptimizeNodeMap(string inputFilePath, string outputFilePath)
        {
            HashSet<Vector3Int> uniqueNodes = new HashSet<Vector3Int>();

            if (File.Exists(inputFilePath))
            {
                string[] lines = File.ReadAllLines(inputFilePath);

                foreach (string line in lines)
                {
                    if (TryParseVector3Int(line, out Vector3Int node))
                    {
                        // Ajouter au HashSet pour éviter les doublons
                        uniqueNodes.Add(node);
                    }
                }

                // Trier les noeuds en ordre lexicographique (X, Y, Z)
                List<Vector3Int> sortedNodes = uniqueNodes
                    .OrderBy(node => node.x)
                    .ThenBy(node => node.y)
                    .ThenBy(node => node.z)
                    .ToList();

                // Écrire les noeuds optimisés dans le fichier de sortie
                WriteNodesToFile(sortedNodes, outputFilePath);
            }
        }
        private static void WriteNodesToFile(List<Vector3Int> nodes, string outputFilePath)
        {
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                foreach (Vector3Int node in nodes)
                {
                    writer.WriteLine($"({node.x}, {node.y}, {node.z})");
                }
            }
        }

        // Méthode pour parser une ligne en Vector3Int
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
    }
}