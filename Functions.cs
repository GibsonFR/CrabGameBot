namespace GibsonBot
{
    //Ici on stock les fonctions, dans des class pour la lisibilité du code dans Plugin.cs 

    //Cette classe regroupe des fonctions qui servent a l'affichage d'infos utiles

    public class BotFunctions
    {
        public static void Combat(Vector3 playerPos, PlayerManager target, double combatProbability)
        {
            var distance = Vector3.Distance(playerPos, target.transform.position);
            System.Random random = new();

            if (distance <= 8f && random.NextDouble() < combatProbability)
            {
                clientMovement.playerCam.LookAt(target.transform.position);
                ClientSend.PunchPlayer(target.steamProfile.m_SteamID, (target.gameObject.transform.forward * -1) + new Vector3(0, 1.5f, 0));
            }
        }

        public static void LookAtTarget(Vector3 target)
        {
            Vector3 playerPos = clientBody.transform.position;

            // Calculer la direction de la cible sans changer la hauteur arbitrairement
            Vector3 vectorDir = new Vector3(target.x - playerPos.x, 0, target.z - playerPos.z);

            // Vérifier si le vecteur de direction est valide
            if (vectorDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(vectorDir);

                clientMovement.playerCam.transform.rotation = targetRotation;
            }
        }
    }
    public class UIFunctions
    {
        public static void SkipCinematicCamera()
        {
            GameObject mapCamera = GameObject.Find("MapCamera");
            if (mapCamera != null)
            {
                mapCamera.GetComponent<MonoBehaviourPublicBoObInUnique>().Method_Private_Void_0();
            }
        }
    }
    public class DisplayFunctions
    {
        public static void CreateDisplayFile(string path)
        {
            string menuContent = "<color=yellow>\t\r\n\tPosition : [POSITION]  |  HSpeed : [SPEEDH]  |  VSpeed : [SPEEDV]  |  Rotation : [ROTATION]  | GameState : [GAMESTATE]  | Map : [MAP]  |  Mode : [MODE]  |  TSpeedV : [HSPEEDT]  |  TSpeedH : [VSPEEDT]\t</color>";

            if (File.Exists(path))
            {
                string currentContent = File.ReadAllText(path, System.Text.Encoding.UTF8);


                if (currentContent != menuContent)
                {
                    File.WriteAllText(path, menuContent, System.Text.Encoding.UTF8);
                }
            }
            else
            {
                // Si le fichier n'existe pas, créez-le avec le contenu fourni
                File.WriteAllText(path, menuContent, System.Text.Encoding.UTF8);
            }
        }
        
        public static void RegisterDataCallbacks(Dictionary<string, System.Func<string>> dict)
        {
            foreach (KeyValuePair<string, Func<string>> pair in dict)
            {
                DisplayDataCallbacks.Add(pair.Key, pair.Value);
            }
        }
        public static void LoadMenuLayout()
        {
            layout = File.ReadAllText(displayFilePath, System.Text.Encoding.UTF8);
        }
        public static void RegisterDefaultCallbacks()
        {
            RegisterDataCallbacks(new System.Collections.Generic.Dictionary<string, System.Func<string>>(){
                
                {"POSITION", ClientData.ClientPositionDisplay},
                {"SPEEDH", ClientData.ClientHorizontalSpeedDisplay},
                {"SPEEDV", ClientData.ClientVerticalSpeedDisplay},
                {"ROTATION", ClientData.ClientRotationDisplay},
                {"GAMESTATE", GameData.GetGameState},
                {"MAP", ()=> mapId.ToString()},
                {"MODE", ()=> modeId.ToString()},
                {"VSPEEDT", ()=> new Vector3(0,SnowBrawlManager.closestPlayerVelocity.y,0).magnitude.ToString("F1")},
                {"HSPEEDT", ()=> new Vector3(SnowBrawlManager.closestPlayerVelocity.x, 0, SnowBrawlManager.closestPlayerVelocity.z).magnitude.ToString("F1")},
            });
        }
        public static string FormatLayout()
        {
            string formatted = layout;
            foreach (KeyValuePair<string, Func<string>> pair in DisplayDataCallbacks)
            {
                formatted = formatted.Replace("[" + pair.Key + "]", pair.Value());
            }
            return formatted;
        }
    }

    public class ClientData
    {
        //Cette fonction retourne le steam Id du client sous forme de ulong
        public static ulong GetClientId()
        {
            return (ulong)SteamManager.Instance.field_Private_CSteamID_0;
        }

        //Cette fonction retourne un booleen qui détermine si le client est Host ou non
        public static bool IsClientHost()
        {
            return SteamManager.Instance.IsLobbyOwner() && !LobbyManager.Instance.Method_Public_Boolean_0();
        }

        //Cette fonction retourne le GameObject du client
        public static GameObject GetClientObject()
        {
            return GameObject.Find("/Player");
        }
        //Cette fonction retourne le Rigidbody du client
        public static Rigidbody GetClientBody()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<Rigidbody>();
        }
        //Cette fonction retourne le PlayerManager du client
        public static PlayerManager GetClientManager()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<PlayerManager>();
        }

        //Cette fonction retourne la class Movement qui gère les mouvements du client
        public static PlayerMovement GetClientMovement()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<PlayerMovement>();
        }

        //Cette fonction retourne l'inventaire du client
        public static PlayerInventory GetClientInventory()
        {
            return GetClientObject() == null ? null : PlayerInventory.Instance;
        }

        //Cette fonction retourne le status du client
        public static PlayerStatus GetClientStatus()
        {
            return GetClientObject() == null ? null : PlayerStatus.Instance;
        }

        //Cette fonction retourne la Camera du client
        public static Camera GetClientCamera()
        {
            return GetClientBody() == null ? null : UnityEngine.Object.FindObjectOfType<Camera>();
        }

        //Cette fonction retourne l'username du client
        public static string GetClientUsername()
        {
            return GetClientManager() == null ? null : GetClientManager().username.ToString();
        }
        
        //Cette fonction retourne la rotation du client
        public static Quaternion? GetClientRotation()
        {
            return GetClientObject() == null ? null : GetClientCamera().transform.rotation;
        }

        //Cette fonction gère l'affichage de la rotation du client
        public static string ClientRotationDisplay()
        {
            return GetClientObject() == null ? "N/A" : GetClientCamera().transform.rotation.ToString();
        }

        //Cette fonction retourne la position du client
        public static Vector3? GetClientPosition()
        {
            return GetClientObject() == null ? null : GetClientBody().transform.position;
        }

        //Cette fonction gère l'affichage de la position du client
        public static string ClientPositionDisplay()
        {
            return GetClientObject() == null ? "N/A" : GetClientBody().transform.position.ToString();
        }

        //Cette fonction retourne la vitesse du client
        public static Vector3? GetClientSpeed()
        {
            return GetClientObject() == null ? null : clientBody.velocity;
        }

        //Cette fonction gère l'affichage de la vitesse du client
        public static string ClientHorizontalSpeedDisplay()
        {
            return GetClientObject() == null ? "N/A" : new Vector3 (clientBody.velocity.x, 0, clientBody.velocity.z).magnitude.ToString("F1");
        }

        //Cette fonction gère l'affichage de la vitesse du client
        public static string ClientVerticalSpeedDisplay()
        {
            return GetClientObject() == null ? "N/A" : new Vector3(0, clientBody.velocity.y, 0).magnitude.ToString("F1");
        }

        //Cette fonction retourne si le client a un item ou non équipé
        public static bool ClientHasItemCheck()
        {
            return PlayerInventory.Instance.currentItem == null ? false : true;
        }

        //Cette fonction désactive les mouvements du client
        public static void DisableClientMovement()
        {
            if (clientBody != null && clientBody.position != Vector3.zero)
            {
                clientBody.isKinematic = true;
                clientBody.useGravity = false;
            }
        }

        //Cette fonction active les mouvements du client
        public static void EnableClientMovement()
        {
            if (clientBody != null && clientBody.position != Vector3.zero)
            {
                clientBody.isKinematic = false;
                clientBody.useGravity = true;
            }
        }
    }

}
