using static GibsonBot.RaceConstants;
using static GibsonBot.RaceManager;
using static GibsonBot.RaceUtility;

namespace GibsonBot
{
    internal class RaceConstants
    {
        public const int RACE_MODE_ID = 11;
    }
    public class RaceManager : MonoBehaviour
    {
        Vector3 playerPos, nearestTechPos, endRace;
        bool doingAutotech, hasFinishRace, hasSetEndRace;
        bool isRaceMode = false;

        void Awake()
        {
            isRaceMode = LobbyManager.Instance.gameMode.id == RACE_MODE_ID;
        }
        void Update()
        {
            if (!isRaceMode || !isClientBot || IsGameStateFreeze() || clientBody == null) return;
            
            playerPos = clientBody.transform.position;
            nearestTechPos = AutoTechFunctions.FindNearestTech(mapId, playerPos, raceBotTechFolderPath);
            var distanceNearestTech = Vector3.Distance(playerPos, nearestTechPos);


            if (distanceNearestTech <= 2f && !hasFinishRace)
            {
                TASManager.autoTechToggle = true;
                doingAutotech = true;


            }
            else if (!doingAutotech && !hasFinishRace)
            {
                InputManager.Move(nearestTechPos, playerPos);
            }
            else if (!hasFinishRace)
            {
                if (!replayActive && doingAutotech)
                {
                    TASManager.autoTechToggle = false;
                    doingAutotech = false;
                    hasFinishRace = true;
                }
            }

            if (hasFinishRace)
            {
                if (!hasSetEndRace)
                {
                    hasSetEndRace = true;
                    endRace = playerPos;
                }
                if (Vector3.Distance(endRace, playerPos) > 5)
                {
                    InputManager.Move(endRace, playerPos);
                }
            }
            
        }
    }
    internal class RaceUtility
    {
    }
}
