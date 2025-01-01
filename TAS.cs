using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main namespace for GibsonBot functionality.
/// </summary>
namespace GibsonBot
{
    /// <summary>
    /// Manages the TAS (Tool-Assisted Speedrun) features, including initialization and toggling of AutoTech.
    /// </summary>
    public class TASManager : MonoBehaviour
    {
        private bool _initialized;
        private bool _action;

        public static bool autoTechToggle;
        public static float elapsedFrame;

        /// <summary>
        /// Unity's Update method, called once per frame. Handles AutoTech toggling and initialization.
        /// </summary>
        private void Update()
        {
            // Initial setup and resetting of important variables
            if (!_initialized)
            {
                autoTechToggle = false;
                AutoTechFunctions.HandleReplayEnd();      // Ensures no replay is active
                ClientData.EnableClientMovement();        // Re-enables player movement
                elapsedFrame = 0f;
                _initialized = true;
            }

            // If AutoTech is enabled, process the auto-technical replay logic
            if (autoTechToggle)
            {
                AutoTechFunctions.HandleAutoTech();
            }

            // Handle toggling messages and actions
            if (autoTechToggle && !_action)
            {
                _action = true;
                Utility.ForceMessage("■<color=orange>AutoTech ON</color>■");
            }
            else if (!autoTechToggle && _action)
            {
                _action = false;
                Utility.ForceMessage("■<color=orange>AutoTech OFF</color>■");

                // End the replay and re-enable movement when toggling off
                AutoTechFunctions.HandleReplayEnd();
                ClientData.EnableClientMovement();
            }
        }
    }

    /// <summary>
    /// Contains functions related to "AutoTech," such as finding nearest tech, handling replays, etc.
    /// </summary>
    public static class AutoTechFunctions
    {
        // Example static fields referenced in the code. Ensure these fields exist in your real project.
        public static bool initTech;
        public static bool replayActive;
        public static bool replayStop;
        public static bool isReplayInit;
        public static bool isReplayReaderInitialized;
        public static bool replaySafeClose;
        public static string techName;
        public static float estimatedFPS;
        public static int currentLineIndex;
        public static List<string> csvLines = new List<string>();


        // Variables used to store extracted data
        public static int isClientCloneTagged;
        public static Vector3 clientClonePosition;
        public static Vector3 otherPlayerClonePosition;
        public static Vector3 clientCloneRotation;
        public static Quaternion clientCloneQRotation;

        /// <summary>
        /// Searches in the specified directory (based on mapId) for the nearest tech (first lines of each file)
        /// that is within an acceptable vertical distance from the reference point.
        /// </summary>
        /// <param name="mapId">The current map ID.</param>
        /// <param name="referencePoint">The point from which distances are calculated.</param>
        /// <param name="folderPath">Base path to the folder containing tech data.</param>
        /// <returns>The closest valid position among the tech files; Vector3.zero if none is found.</returns>
        public static Vector3 FindNearestTech(int mapId, Vector3 referencePoint, string folderPath)
        {
            string fullFolderPath = Path.Combine(folderPath, FileChecker.GetMapNameWithId(mapId));
            CultureInfo culture = new CultureInfo("fr-FR");

            Vector3 closestPosition = Vector3.zero;
            float closestDistance = float.MaxValue;
            const float maxVerticalDiff = 5f;
            const int linesToCheck = 3;

            // Iterate over each file in the tech folder
            if (Directory.Exists(fullFolderPath))
            {
                foreach (var filePath in Directory.GetFiles(fullFolderPath))
                {
                    // Read all lines (so we can easily access lines by index)
                    var allLines = File.ReadAllLines(filePath);
                    int limit = Mathf.Min(linesToCheck, allLines.Length);

                    for (int i = 0; i < limit; i++)
                    {
                        string[] parts = allLines[i].Split(';');
                        if (parts.Length < 4) continue;

                        if (float.TryParse(parts[1], NumberStyles.Any, culture, out float x) &&
                            float.TryParse(parts[2], NumberStyles.Any, culture, out float y) &&
                            float.TryParse(parts[3], NumberStyles.Any, culture, out float z))
                        {
                            Vector3 currentPosition = new Vector3(x, y, z);

                            // Skip if vertical distance is above threshold
                            if (Mathf.Abs(referencePoint.y - currentPosition.y) > maxVerticalDiff)
                                continue;

                            float distance = Vector3.Distance(referencePoint, currentPosition);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestPosition = currentPosition;
                            }
                        }
                    }
                }
            }

            return closestPosition;
        }

        /// <summary>
        /// Similar to FindNearestTech but also retrieves the "end" position of the tech 
        /// (derived from the last line of the file).
        /// </summary>
        public static Vector3 FindNearestTechEnd(int mapId, Vector3 referencePoint, string folderPath)
        {
            string fullFolderPath = Path.Combine(folderPath, FileChecker.GetMapNameWithId(mapId));
            CultureInfo culture = new CultureInfo("fr-FR");

            Vector3 closestPosition = Vector3.zero;
            Vector3 endPosition = Vector3.zero;
            float closestDistance = float.MaxValue;
            const float maxVerticalDiff = 5f;
            const int linesToCheck = 3;

            if (Directory.Exists(fullFolderPath))
            {
                foreach (var filePath in Directory.GetFiles(fullFolderPath))
                {
                    var allLines = File.ReadAllLines(filePath);
                    int limit = Mathf.Min(linesToCheck, allLines.Length);

                    for (int i = 0; i < limit; i++)
                    {
                        string[] parts = allLines[i].Split(';');
                        if (parts.Length < 4) continue;

                        if (float.TryParse(parts[1], NumberStyles.Any, culture, out float x) &&
                            float.TryParse(parts[2], NumberStyles.Any, culture, out float y) &&
                            float.TryParse(parts[3], NumberStyles.Any, culture, out float z))
                        {
                            Vector3 currentPosition = new Vector3(x, y, z);

                            // Skip if vertical distance is above threshold
                            if (Mathf.Abs(referencePoint.y - currentPosition.y) > maxVerticalDiff)
                                continue;

                            float distance = Vector3.Distance(referencePoint, currentPosition);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestPosition = currentPosition;

                                // Retrieve end position from last line
                                string lastLine = allLines.LastOrDefault();
                                if (!string.IsNullOrEmpty(lastLine))
                                {
                                    var lastLineParts = lastLine.Split(';');
                                    if (lastLineParts.Length >= 4 &&
                                        float.TryParse(lastLineParts[1], NumberStyles.Any, culture, out float xe) &&
                                        float.TryParse(lastLineParts[2], NumberStyles.Any, culture, out float ye) &&
                                        float.TryParse(lastLineParts[3], NumberStyles.Any, culture, out float ze))
                                    {
                                        endPosition = new Vector3(xe, ye, ze);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return endPosition;
        }

        /// <summary>
        /// Retrieves a Vector3 from the file name itself (format: "x;y;z...").
        /// Throws an exception if parsing fails.
        /// </summary>
       
        public static Vector3 GetVector3FromPath(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string[] splitData = fileName.Split(';');

            if (splitData.Length < 3)
                throw new Exception("Invalid file path: cannot extract Vector3 data.");

            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            bool xParse = float.TryParse(splitData[0].Replace(',', '.'), NumberStyles.Float, cultureInfo, out float x);
            bool yParse = float.TryParse(splitData[1].Replace(',', '.'), NumberStyles.Float, cultureInfo, out float y);
            bool zParse = float.TryParse(splitData[2].Replace(',', '.'), NumberStyles.Float, cultureInfo, out float z);

            if (!xParse || !yParse || !zParse)
                throw new Exception("Float conversion failed while parsing file name.");

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Main method to handle AutoTech. Checks if replay can be started or continued,
        /// initializes it if needed, and processes lines at a specific frame rate.
        /// </summary>
        public static void HandleAutoTech()
        {
            // If there's no valid file in range and no replay is active, do nothing
            if (FileChecker.GetFileNameWithinRange(clientObject.transform.position) == null && !replayActive)
                return;

            // Initialize Tech if not done already
            if (!initTech)
            {
                techName = FileChecker.GetFileNameWithinRange(clientObject.transform.position);
                if (string.IsNullOrEmpty(techName))
                {
                    replayActive = false;
                    return;
                }

                Vector3 extractedPos = GetVector3FromPath(techName);
                techPosition = extractedPos;
                estimatedFPS = EstimateFpsFromFile(techName) * 1.05f;
                initTech = true;
            }

            replayActive = true;

            // If we are close enough to the file point, initialize replay and disable movement
            if (!isReplayInit && FileChecker.GetDistanceFromPlayerToFilePath(techName, clientBody.transform.position) <= 1)
            {
                isReplayInit = true;
                ClientData.DisableClientMovement();
            }

            // Increase elapsed time
            TASManager.elapsedFrame += Time.deltaTime;

            // If we are not in the correct range, stop the replay
            if (!isReplayInit)
            {
                float verticalDiff = FileChecker.GetVerticalDistance(clientBody.transform.position,
                                          FileChecker.GetPositionFromFilePath(techName));
                float distance = FileChecker.GetDistanceFromPlayerToFilePath(techName, clientBody.transform.position);

                if (verticalDiff > 1f || distance >= 5f)
                {
                    isReplayInit = false;
                    replayActive = false;
                    initTech = false;
                }
            }
            // Replay lines at the estimated FPS
            else if (TASManager.elapsedFrame >= 1f / estimatedFPS && isReplayInit)
            {
                InitializeReaderIfNecessary();
                ReadAndProcessLine();
                TASManager.elapsedFrame = 0f;
            }
        }

        /// <summary>
        /// Initializes the CSV reader if it wasn't initialized before.
        /// </summary>
        private static void InitializeReaderIfNecessary()
        {
            if (!isReplayReaderInitialized)
            {
                InitializeReader(techName);
                replaySafeClose = false;
            }
        }

        /// <summary>
        /// Reads and processes the next line(s) from the CSV lines if conditions are met.
        /// </summary>
        private static void ReadAndProcessLine()
        {
            // Process a single line each frame, or adapt as needed
            for (int i = 0; i < 1; i++)
            {
                // If there's still data to read, the replay isn't stopped, and player doesn't have an item
                if (currentLineIndex < csvLines.Count && !replayStop && !ClientData.ClientHasItemCheck())
                {
                    ExtractData(csvLines[currentLineIndex]);
                    HandlePOVTrigger();
                    currentLineIndex++;
                }
                else if (!replaySafeClose)
                {
                    // If there's no more data or some condition triggers the end, finalize the replay
                    HandleReplayEnd();
                    return;
                }
            }
        }

        /// <summary>
        /// Opens the file, reads its lines into memory, and checks basic validity.
        /// </summary>
        /// <param name="filePath">Full path to the CSV file containing replay data.</param>
        public static void InitializeReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                replayStop = true;
                return;
            }

            csvLines = File.ReadAllLines(filePath).ToList();

            // Basic validation of the file name
            string fullFilePath = filePath;
            if (!string.IsNullOrEmpty(fullFilePath))
            {
                string fileName = Path.GetFileNameWithoutExtension(fullFilePath);
                string[] parts = fileName.Split(';');
                if (parts.Length < 5)  // Example: check if file name has enough segments
                {
                    replayStop = true;
                    return;
                }
            }
            else
            {
                replayStop = true;
            }

            isReplayReaderInitialized = true;
        }

        /// <summary>
        /// Ends the replay, resets variables, and re-enables player movement.
        /// </summary>
        public static void HandleReplayEnd()
        {
            csvLines.Clear();
            currentLineIndex = 0;
            replayStop = false;
            isReplayReaderInitialized = false;
            isReplayInit = false;
            replayActive = false;
            initTech = false;

            ClientData.EnableClientMovement();
            replaySafeClose = true;
        }

        /// <summary>
        /// Splits a CSV line and extracts the relevant positional and rotational data for the clientClone and other players.
        /// </summary>
        /// <param name="line">Single CSV line containing position/rotation data.</param>
        public static void ExtractData(string line)
        {
            string[] data = line.Split(';');
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("fr-FR");

            // Expecting at least 11 segments for valid data
            if (data.Length < 11) return;

            // Example of parsing data
            isClientCloneTagged = int.Parse(data[4], NumberStyles.Integer, cultureInfo);

            clientClonePosition = new Vector3(
                float.Parse(data[1], NumberStyles.Float, cultureInfo),
                float.Parse(data[2], NumberStyles.Float, cultureInfo),
                float.Parse(data[3], NumberStyles.Float, cultureInfo)
            );

            otherPlayerClonePosition = new Vector3(
                float.Parse(data[5], NumberStyles.Float, cultureInfo),
                float.Parse(data[6], NumberStyles.Float, cultureInfo),
                float.Parse(data[7], NumberStyles.Float, cultureInfo)
            );

            clientCloneRotation = new Vector3(
                float.Parse(data[8], NumberStyles.Float, cultureInfo),
                float.Parse(data[9], NumberStyles.Float, cultureInfo),
                float.Parse(data[10], NumberStyles.Float, cultureInfo)
            );

            clientCloneQRotation = Quaternion.Euler(clientCloneRotation);
        }

        /// <summary>
        /// Updates the player's body position and camera orientation according to the replay data.
        /// </summary>
        private static void HandlePOVTrigger()
        {
            // Move the client's body to the recorded position
            clientBody.transform.position = clientClonePosition;

            // Rotate camera to match the replay's orientation
            Quaternion rotation = Quaternion.Euler(clientCloneRotation);
            Vector3 forwardVector = rotation * Vector3.forward;
            Vector3 targetPosition = clientMovement.playerCam.position + forwardVector;
            clientMovement.playerCam.LookAt(targetPosition);
        }
    }

    /// <summary>
    /// Provides utility functions for dealing with files, map names, and distances.
    /// </summary>
    public static class FileChecker
    {
        /// <summary>
        /// Retrieves the map name corresponding to a given map ID from a text file.
        /// </summary>
        public static string GetMapNameWithId(int mapId)
        {
            if (!File.Exists(Variables.mapNameFilePath)) return null;

            // Read each line: "id: mapName"
            string[] lines = File.ReadAllLines(Variables.mapNameFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int id) && id == mapId)
                {
                    return parts[1].Trim();
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a text file in the tech folder (based on the current map) whose embedded position 
        /// is within a small horizontal and vertical range from the player's position.
        /// </summary>
        public static string GetFileNameWithinRange(Vector3 playerPos)
        {
            string directoryPath = Path.Combine(techFolderPath, GetMapNameWithId(mapId));
            directoryPath = directoryPath.Trim();

            if (!Directory.Exists(directoryPath))
                return null;

            var format = new NumberFormatInfo { NumberDecimalSeparator = "," };
            List<string> validFiles = new List<string>();

            // Check each ".txt" file in directory
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.txt"))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = fileName.Split(';');

                if (parts.Length < 3) continue;

                bool xParse = float.TryParse(parts[0], NumberStyles.Float, format, out float x);
                bool yParse = float.TryParse(parts[1], NumberStyles.Float, format, out float y);
                bool zParse = float.TryParse(parts[2], NumberStyles.Float, format, out float z);

                if (!xParse || !yParse || !zParse)
                    continue;

                Vector3 filePos = new Vector3(x, y, z);
                float distance = Vector3.Distance(playerPos, filePos);
                float verticalDistance = GetVerticalDistance(playerPos, filePos);

                // Check if close enough (within 5 units horizontally, 1 unit vertically)
                if (distance < 5f && verticalDistance < 1f)
                {
                    validFiles.Add(filePath);
                }
            }

            // Return a random file among the valid ones
            if (validFiles.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, validFiles.Count);
                return validFiles[randomIndex];
            }

            return null;
        }

        /// <summary>
        /// Computes the distance between the player position and the position encoded in a file name.
        /// Returns -1 if the file name format is invalid or parsing fails.
        /// </summary>
        public static float GetDistanceFromPlayerToFilePath(string filePath, Vector3 playerPos)
        {
            var format = new NumberFormatInfo { NumberDecimalSeparator = "," };
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = fileName.Split(';');

            if (parts.Length < 3) return -1f;

            if (float.TryParse(parts[0], NumberStyles.Float, format, out float x) &&
                float.TryParse(parts[1], NumberStyles.Float, format, out float y) &&
                float.TryParse(parts[2], NumberStyles.Float, format, out float z))
            {
                Vector3 filePos = new Vector3(x, y, z);
                return Vector3.Distance(playerPos, filePos);
            }

            return -1f;
        }

        /// <summary>
        /// Extracts the position embedded in a file name ("x;y;z").
        /// Returns Vector3.zero if the format is invalid or parsing fails.
        /// </summary>
        public static Vector3 GetPositionFromFilePath(string filePath)
        {
            var format = new NumberFormatInfo { NumberDecimalSeparator = "," };
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = fileName.Split(';');

            if (parts.Length < 3) return Vector3.zero;

            try
            {
                float x = float.Parse(parts[0], format);
                float y = float.Parse(parts[1], format);
                float z = float.Parse(parts[2], format);
                return new Vector3(x, y, z);
            }
            catch
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Returns the absolute vertical distance (difference in Y) between two vectors.
        /// </summary>
        public static float GetVerticalDistance(Vector3 pos1, Vector3 pos2)
        {
            return Mathf.Abs(pos1.y - pos2.y);
        }
    }
}
