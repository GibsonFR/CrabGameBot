using static GibsonBot.LightsOutConstants;
using static GibsonBot.LightOutManager;
using static GibsonBot.LightsOutUtility;
using static GibsonBot.PathFindingUtility;

namespace GibsonBot
{
    internal class LightsOutConstants
    {
        public const int LIGHT_OUT_MODE_ID = 7;
        public const float UPDATE_INTERVAL = 0.1f;
    }

    public class LightOutManager : MonoBehaviour
    {
        private bool isLightOutMode = false;
        private Vector3 playerPos, closestPlayerPos, safePos;
        private float elapsedSafe, elapsedUpdate;
        PlayerManager closestPlayer = null;

        void Awake()
        {
            isLightOutMode = LobbyManager.Instance.gameMode.id == LIGHT_OUT_MODE_ID;
        }

        void Update()
        {
            if (!isLightOutMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedSafe += Time.deltaTime;
            elapsedUpdate += Time.deltaTime;

            if (elapsedUpdate >= UPDATE_INTERVAL)
            {
                closestPlayer = FindClosestPlayer(playerPos);
            }

            if (closestPlayer != null) closestPlayerPos = closestPlayer.transform.position;

            playerPos = clientBody.transform.position;
            var distanceNearestPlayer = Vector3.Distance(closestPlayerPos, playerPos);

            string playerHealthString = clientStatus.currentHp.ToString();
            int playerHealth = int.Parse(playerHealthString);
            int limitHealth = 50;

            if (distanceNearestPlayer < 5f && playerHealth >= limitHealth)
            {
                if (distanceNearestPlayer > 3f)  BunnyHopWithPathFinding(closestPlayerPos, playerPos);
                clientMovement.playerCam.LookAt(closestPlayerPos);
                clientInventory.UseItem();

            }
            else if (playerHealth >= limitHealth)
            {
                BunnyHopWithPathFinding(closestPlayerPos, playerPos);
            }
            else
            {
                if (elapsedSafe > 1f)
                {
                    safePos = FindNodeAwayFromThreats(playerPos, [closestPlayerPos], 20f);
                    elapsedSafe = 0f;
                }


                BunnyHopWithPathFinding(safePos, playerPos);
                if (distanceNearestPlayer < 5f)
                {
                    clientMovement.playerCam.LookAt(closestPlayerPos);
                    clientInventory.UseItem();
                }
            }
        }
        GameObject FindGun()
        {
            // Point de départ - la caméra principale
            Transform currentTransform = Camera.main.transform;

            // Chemin de la hiérarchie à suivre
            string[] hierarchyPath = new string[]
            {
                    "Recoil", "Shake", "Main Camera", "WeaponPos",
                    "WeaponOffset", "WeaponReloadParent", "WeaponParent"
            };

            // Parcourir la hiérarchie
            foreach (var name in hierarchyPath)
            {
                currentTransform = currentTransform.Find(name);
                if (currentTransform == null)
                {
                    // Si un élément du chemin n'est pas trouvé, renvoyer null
                    return null;
                }
            }

            // Trouver le GameObject suivant après "WeaponParent"
            Transform nextTransform = currentTransform.Find("Pistol(Clone)"); // Remplacez par le nom réel
            if (nextTransform != null)
            {
                return nextTransform.gameObject;
            }
            else
            {
                return null;
            }
        }
    }
    internal class LightsOutUtility
    {
    }
}
