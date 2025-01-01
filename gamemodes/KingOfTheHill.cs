using static GibsonBot.KingOfTheHillConstants;

namespace GibsonBot
{
    internal class KingOfTheHillConstants
    {
        public const string SCORE_ZONE_GAME_OBJECT_NAME = "ScoreZone";
    }

    public class KingOfTheHillManager : MonoBehaviour
    {
        private Vector3 playerPos, closestPlayerPos, nearestTechPos, scoreZonePos = Vector3.zero;
        private bool doingAutotech, isOnTop, hasScrollItemBack, hasScrollItemNext, closestPlayerIsOnTop;
        PlayerManager closestPlayer;
        float distanceToClosestPlayer, distanceToNearestTech, elapsedUpdate;
        void Update()
        {
            if (modeId == 2 && isClientBot && !IsGameStateFreeze() && clientBody != null)
            {
                elapsedUpdate += Time.deltaTime;

                if (scoreZonePos == Vector3.zero) scoreZonePos = GameObject.Find("ScoreZone").transform.position;

                playerPos = clientBody.transform.position;
                closestPlayer =FindClosestPlayer(playerPos);

                if (closestPlayer != null)
                {
                    closestPlayerPos = closestPlayer.transform.position;
                }

                if (elapsedUpdate > 1f)
                {
                    elapsedUpdate = 0;
                    nearestTechPos = AutoTechFunctions.FindNearestTech(mapId, playerPos, KOTHbotTechFolderPath);
                }


                distanceToClosestPlayer = Vector3.Distance(closestPlayerPos, playerPos);
                if (nearestTechPos != null) distanceToNearestTech = Vector3.Distance(playerPos, nearestTechPos);

                closestPlayerIsOnTop = (closestPlayerPos.y - scoreZonePos.y) > -1.5f;
                isOnTop = (playerPos.y - scoreZonePos.y) > -1.5f && !TASManager.autoTechToggle;


                if (distanceToNearestTech <= 0.5f && !isOnTop)
                {
                    TASManager.autoTechToggle = true;
                    doingAutotech = true;
                }
                else if (!doingAutotech && !isOnTop)
                {
                    PathFindingUtility.MoveWithPathFinding(nearestTechPos,playerPos);
                    if (!hasScrollItemNext)
                    {
                        clientInventory.ScrollItem(1);
                        hasScrollItemNext = true;
                        hasScrollItemBack = false;
                    }
                }
                else if (!isOnTop)
                {
                    if (!replayActive && doingAutotech)
                    {
                        TASManager.autoTechToggle = false;
                        doingAutotech = false;
                    }
                }

                if (isOnTop)
                {
                    if (!hasScrollItemBack)
                    {
                        clientInventory.EquipItem(2);
                        hasScrollItemBack = true;
                        hasScrollItemNext = false;
                    }
                    if (Vector3.Distance(scoreZonePos, playerPos) > 2 && !closestPlayerIsOnTop)
                    {
                        InputManager.Move(scoreZonePos, playerPos);
                    }

                    if (closestPlayerIsOnTop && Vector3.Distance(scoreZonePos, playerPos) < 8)
                    {
                        InputManager.Move(closestPlayerPos, playerPos);
                    }
                    else if (Vector3.Distance(scoreZonePos, playerPos) > 2)
                    {
                        InputManager.Move(scoreZonePos, playerPos);
                    }

                    if (distanceToClosestPlayer < 5 && closestPlayerIsOnTop)
                    {
                        clientMovement.playerCam.LookAt(closestPlayerPos);
                        clientInventory.UseItem();
                    }
                }
            }
        }
    }
    internal class KingOfTheHill
    {

    }
}
