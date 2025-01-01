using static GibsonBot.BustlingButtonsConstants;
using static GibsonBot.BustlingButtonsUtility;
using static GibsonBot.BustlingButtonsManager;
using static GibsonBot.PathFindingManager;
using static GibsonBot.PathFindingUtility;

namespace GibsonBot
{
    internal class BustlingButtonsConstants
    {
        public const string STAGE_COLLIDER_GAME_OBJECT_NAME = "StageController/Collider & Trigger";
        public const string BUTTON_GAME_OBJECT_NAME = "Button";
        public const string NEXT_AUDIO_SOURCE_GAME_OBJECT = "StageController/Sfxs/Next";
        public const int BUSTLING_BUTTONS_MODE_ID = 12;
        public const float UPDATE_INTERVAL = 0.1f;
    }

    public class BustlingButtonsManager : MonoBehaviour
    {
        GameObject randomButton;
        public static BustlingButtonsStageCollider stageCollider = null;
        public static AudioSource nextPlayerTurn = null;
        private bool isBustlingButtonsMode = false;
        private bool playerTurn = false;
        private Vector3 randomButtonPos = Vector3.zero;
        private float distanceFromRandomButton = 0f;
        private Vector3 playerPos = Vector3.zero;
        private float elapsedUpdate = 0f;

        void Awake()
        {
            isBustlingButtonsMode = LobbyManager.Instance.gameMode.id == BUSTLING_BUTTONS_MODE_ID;
            if (!isBustlingButtonsMode) return;

            stageCollider = GameObject.Find(STAGE_COLLIDER_GAME_OBJECT_NAME).GetComponent<BustlingButtonsStageCollider>();
            nextPlayerTurn = GameObject.Find(NEXT_AUDIO_SOURCE_GAME_OBJECT).GetComponent<AudioSource>();
        }

        void Update()
        {
            if (!isBustlingButtonsMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;

            elapsedUpdate += Time.deltaTime;
            
            playerPos = clientBody.transform.position;

            if (elapsedUpdate >= UPDATE_INTERVAL)
            {
                elapsedUpdate = 0f;
                if (IsNextPlayerTurn())
                {
                    if (!IsStageColliderActivated())
                    {
                        randomButton = null;
                        playerTurn = true;
                    }
                    else playerTurn = false;
                }
            }

            if (!playerTurn) return;
       
            if (randomButton == null) randomButton = GetRandomActiveButton(GetActiveButton());
                    
            randomButtonPos = randomButton.transform.position;
            distanceFromRandomButton = Vector3.Distance(randomButtonPos, playerPos);

            if (distanceFromRandomButton < 3f)
            {
                clientMovement.playerCam.LookAt(randomButtonPos);
                randomButton.GetComponent<BustlingButtonsButton>().TryInteract();
            }
            else MoveWithPathFinding(randomButtonPos, playerPos);
        }
    }

    internal class BustlingButtonsUtility
    {
        public static bool IsStageColliderActivated()
        {
            return stageCollider.isActiveAndEnabled;
        }


        public static bool IsNextPlayerTurn()
        {
            return nextPlayerTurn.isPlaying;
        }

        public static List<GameObject> GetActiveButton()
        {
            List<GameObject> activeButtons = [];
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            var filteredButtons = allObjects.Where(obj => obj.name == BUTTON_GAME_OBJECT_NAME).ToList();

            foreach (GameObject obj in filteredButtons)
            {
                try
                {

                    if (obj.GetComponent<BustlingButtonsButton>().active) activeButtons.Add(obj);
                }
                catch { }
            }
            return activeButtons;
        }

        public static GameObject GetRandomActiveButton(List<GameObject> activeButtons)
        {
            Il2CppSystem.Random rand = new();
            int index = rand.Next(activeButtons.Count);
            return activeButtons[index];
        }
    }
}
