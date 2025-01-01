namespace GibsonBot
{
    internal static class Utility
    {
        public static void CreateDebugCube(Vector3 position, Vector3 SAFE_POSITION_CUBE_SIZE, float SAFE_POSITION_CUBE_LIFETIME)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.localScale = SAFE_POSITION_CUBE_SIZE;  // Small size for debugging
            cube.GetComponent<Renderer>().material.color = Color.yellow;
            GameObject.Destroy(cube, SAFE_POSITION_CUBE_LIFETIME);  // Destroy cube after a short time
        }
        public static PlayerManager FindClosestPlayer(Vector3 playerPos)
        {
            PlayerManager closestPlayer = null;
            float closestDistance = float.PositiveInfinity;
            foreach (var player in GameManager.Instance.activePlayers)
            {
                try
                {
                    if (player.value == null) continue;
                    if (player.value.steamProfile.m_SteamID == clientId) continue;
                    if (player.value.dead) continue;

                    var distance = Vector3.Distance(playerPos, player.value.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = player.value;
                    }
                }
                catch { }
            }
            return closestPlayer;
        }

        public static bool IsGameStateFreeze()
        {
            return GetGameState() == "Freeze";
        }
    
        public static float EstimateFpsFromFile(string path)
        {
            var lignes = File.ReadLines(path).Take(200).ToList();
            float totalDifference = 0;
            int count = 0;

            for (int i = 1; i < lignes.Count; i++)
            {
                string[] actualPart = lignes[i].Split(';');
                string[] previousPart = lignes[i - 1].Split(';');

                float actualTime = float.Parse(actualPart[0]);
                float previousTime = float.Parse(previousPart[0]);

                float difference = actualTime - previousTime;

                if (difference <= 100)
                {
                    totalDifference += difference;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0;
            }

            float differenceAverage = totalDifference / count;
            return 1000 / differenceAverage;
        }
        public static void CreateTechsFolders(string techFolderPath, string mapPath)
        {
            if (!Directory.Exists(techFolderPath))
            {
                Directory.CreateDirectory(techFolderPath);
            }

            if (File.Exists(mapPath))
            {
                string listOfMaps = File.ReadAllText(mapPath, System.Text.Encoding.UTF8);
                string[] lines = listOfMaps.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string[] elements = line.Split(':');
                    if (elements.Length == 2)
                    {
                        if (int.TryParse(elements[0].Trim(), out int mapId))
                        {
                            string mapName = elements[1].Trim();
                            string folderName = Path.Combine(techFolderPath, mapName);

                            if (!Directory.Exists(folderName))
                            {
                                Directory.CreateDirectory(folderName);
                            }

                        }
                    }
                }
            }
        }
        public static void PressLobbyButton()
        {
            GameObject button = GameObject.Find("Button/Button");
            if (button != null && clientBody != null)
                button.GetComponent<LobbyReadyInteract>().TryInteract();
        }

        //Cette fonction envoie un message dans le chat de la part du client
        public static void SendMessage(string message)
        {
            ChatBox.Instance.SendMessage(message);
        }

        //Cette fonction envoie un message dans le chat de la part du client en mode Force (seul le client peut voir le message)
        public static void ForceMessage(string message)
        {
            ChatBox.Instance.ForceMessage(message);
        }

        //Cette fonction envoie un message dans le chat de la part du server, marche uniquement en tant que Host de la partie
        public static void SendServerMessage(string message)
        {
            ServerSend.SendChatMessage(1, message);
        }

        //Cette Fonction permet d'écrire une ligne dans un fichier txt
        public static void Log(string path, string line)
        {
            // Utiliser StreamWriter pour ouvrir le fichier et écrire à la fin
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(line); // Écrire la nouvelle ligne
            }
        }

        //Cette fonction créer un dossier si il n'existe pas déjà
        public static void CreateFolder(string path, string logPath)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFolder] : " + ex.Message);
            }
        }
        //Cette fonction créer un fichier si il n'existe pas déjà
        public static void CreateFile(string path, string logPath)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFile] : " + ex.Message);
            }
        }

        //Cette fonction réinitialise un fichier
        public static void ResetFile(string path, string logPath)
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(path))
                {
                    using StreamWriter sw = new(path, false);
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [ResetFile] : " + ex.Message);
            }
        }

        public static void ReadBotWhiteList(List<ulong> list, string filePath)
        {
            bool parseSuccess;

            if (File.Exists(filePath))
            {
                list.Clear();

                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    if (line != null)
                    {
                        parseSuccess = ulong.TryParse(line, out ulong resultUlong);
                        if (parseSuccess) list.Add(resultUlong);
                    }
                }
            }
        }

        //Créer un fichier de configuration lisible par ReadConfigFile()
        public static void SetConfigFile(string configFilePath)
        {
            // Définition des valeurs par défaut
            Dictionary<string, string> defaultConfig = new Dictionary<string, string>
            {
                {"version", "v0.1.0"},
                {"menuKey", "f3"},
                {"displayFrameRate", "0,05"},
                {"exeFrameRate", "30" },
            };

            Dictionary<string, string> currentConfig = new Dictionary<string, string>();

            // Si le fichier existe, lire la configuration actuelle
            if (File.Exists(configFilePath))
            {
                string[] lignes = File.ReadAllLines(configFilePath);

                foreach (string ligne in lignes)
                {
                    string[] keyValue = ligne.Split('=');
                    if (keyValue.Length == 2)
                    {
                        currentConfig[keyValue[0]] = keyValue[1];
                    }
                }
            }

            // Fusionner la configuration actuelle avec les valeurs par défaut
            foreach (KeyValuePair<string, string> paire in defaultConfig)
            {
                if (!currentConfig.ContainsKey(paire.Key))
                {
                    currentConfig[paire.Key] = paire.Value;
                }
            }

            // Sauvegarder la configuration fusionnée
            using (StreamWriter sw = File.CreateText(configFilePath))
            {
                foreach (KeyValuePair<string, string> paire in currentConfig)
                {
                    sw.WriteLine(paire.Key + "=" + paire.Value);
                }
            }
        }
        //Lit un fichier de config créer par SetConfigFile
        public static void ReadConfigFile(string configFilePath)
        {
            string[] lines = System.IO.File.ReadAllLines(configFilePath);
            Dictionary<string, string> config = new Dictionary<string, string>();
            CultureInfo cultureInfo = new CultureInfo("fr-FR");
            bool parseSuccess;

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    config[key] = value;
                }
            }

            menuKey = config["menuKey"];

            parseSuccess = int.TryParse(config["exeFrameRate"], out int resultInt);
            exeFrameRate = parseSuccess ? resultInt : 30;

            parseSuccess = float.TryParse(config["displayFrameRate"], out float resultFloat);
            displayFrameRate = parseSuccess ? resultFloat : 0.1f;

            //-----------------------------//
            //Exemple d'utilisation du ReadConfigFile, permet de lire le fichier config et d'associer cette valeur a une variable globale
            /*
            menuKey = config["menuKey"];

            parseSuccess = int.TryParse(config["messageTimer"], out resultInt);
            messageTimer = parseSuccess ? resultInt : 30;

            parseSuccess = int.TryParse(config["playerToAutoStart"], out resultInt);
            playerToAutoStart = parseSuccess ? resultInt : 2;

            parseSuccess = int.TryParse(config["afkCheckDuration"], out resultInt);
            afkCheckDuration = parseSuccess ? resultInt : 5;

            parseSuccess = bool.TryParse(config["afkCheck"], out resultBool);
            afkCheck = parseSuccess ? resultBool : false;

            parseSuccess = bool.TryParse(config["chatConsole"], out resultBool);
            chatConsole = parseSuccess ? resultBool : false;

            parseSuccess = bool.TryParse(config["fireworks"], out resultBool);
            fireworks = parseSuccess ? resultBool : false;

            parseSuccess = bool.TryParse(config["snowballs"], out resultBool);
            snowballs = parseSuccess ? resultBool : false;
            */
            //-----------------------------//
        }
    }

    //Cette class regroupe un ensemble de fonction relative aux données de la partie
    public class GameData
    {
        //Cette fonction retourne le GameState de la partie en cours
        public static string GetGameState()
        {
            return GameManager.Instance.gameMode.modeState.ToString();
        }

        //Cette fonction retourne le LobbyManager
        public static LobbyManager GetLobbyManager()
        {
            return LobbyManager.Instance;
        }

        public static SteamManager GetSteamManager()
        {
            return SteamManager.Instance;
        }

        //Cette fonction retourne l'id de la map en cours
        public static int GetMapId()
        {
            return GetLobbyManager().map.id;
        }

        //Cette fonction retourne l'id du mode en cours
        public static int GetModeId()
        {
            return GetLobbyManager().gameMode.id;
        }
        
    }
}
