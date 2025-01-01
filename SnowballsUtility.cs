using static GibsonBot.SnowballsUtility;
using static GibsonBot.SnowballsConstants;
using static GibsonBot.SnowballsManager;

namespace GibsonBot
{
    /// <summary>
    /// Contains constants for snowball mechanics, such as speed and impact delay.
    /// </summary>
    internal class SnowballsConstants
    {
        public const string SNOWBALL_PILE_GAME_OBJECT_NAME = "SnowballPile";
        public const string LAYER_MASK_INTERACT = "Interact";
        public const string LAYER_MASK_HURTBOX = "Hurtbox";
        public const string LAYER_MASK_DETECTPLAYER = "DetectPlayer";
        public const string LAYER_MASK_PROXIMITY = "Proximity";
        public const string LAYER_MASK_DEFAULT = "Default";
        public const float SNOWBALL_INITIAL_SPEED = 100f;
        public const float SNOWBALL_GRAVITY_MULTIPLICATOR = 5f;
        public const float COLLISION_MARGIN = 1f;     
        public const float TRAJECTORY_CUBE_LIFETIME = 5f;  
        public const float LAUNCH_ANGLE_OFFSET = -0.5f;
        public const float SIMULATION_TIME_STEP = 0.01f;
        public static readonly Vector3 PLAYER_COLLISION_RADIUS = new(1.5f, 2.1f, 1.5f);
        public static readonly Vector3 SNOWBALL_POSITION_OFFSET = new(0f, 1f, 0f);
        public static readonly Vector3 TRAJECTORY_CUBE_SIZE = new(0.2f, 0.2f, 0.2f);
        public static readonly Vector3 TARGET_OFFSET = new(0, 1f, 0);
    }

    /// <summary>
    /// Handles the update loop, managing the active snowballs and their impact times.
    /// </summary>
    internal class SnowballsManager : MonoBehaviour
    {
        public static Dictionary<Vector3, Vector3> dangerousSnowballsDirections = new Dictionary<Vector3, Vector3>();
        public static Dictionary<Vector3, float> dangerousSnowballs = new Dictionary<Vector3, float>();
        public static List<SnowballPile> snowballPiles = new List<SnowballPile>();

        void Awake()
        {
            snowballPiles = GetSnowballPiles();
        }

        void Update()
        {
            List<Vector3> keysToRemove = new List<Vector3>();

            // Populate the keys list with the keys from the dangerousSnowballs dictionary
            List<Vector3> keys = new List<Vector3>(dangerousSnowballs.Keys);

            // Iterate through the active snowballs and update their timers
            foreach (var key in keys)
            {
                if (dangerousSnowballs[key] >= 0)
                {
                    dangerousSnowballs[key] -= Time.deltaTime;
                }
                else
                {
                    // Collect the keys to remove after iteration
                    keysToRemove.Add(key);
                }
            }

            // Remove the snowballs with expired timers after the iteration
            foreach (var key in keysToRemove)
            {
                dangerousSnowballs.Remove(key);
                dangerousSnowballsDirections.Remove(key);
            }
        }
    }



    /// <summary>
    /// Utility class that provides methods for calculating snowball trajectories, impacts, and collision detection.
    /// </summary>
    internal static class SnowballsUtility
    {
        /// <summary>
        /// Update the closest snowball pile and its position.
        /// </summary>
        public static void UpdateClosestSnowballPile(ref SnowballPile closestSnowballPile, ref Vector3 closestSnowballPilePos, Vector3 playerPos)
        {
            closestSnowballPile = GetClosestSnowballPile(playerPos, snowballPiles);
            closestSnowballPilePos = closestSnowballPile.transform.position;
        }

        /// <summary>
        /// Uses the snowball from the player's inventory, aiming at the closest player's position and velocity.
        /// </summary>
        /// return if the player used or not the snowball
        public static bool UseSnowball(Vector3 calculatedAimPoint)
        {
            try
            {
                // Get the current item
                SnowballComponent snowballComponent = PlayerInventory.Instance.currentItem.GetComponent<SnowballComponent>();

                bool isSnowballLoaded = snowballComponent.field_Protected_Boolean_0;

                // Check if the snowball is valid and can be used
                if (snowballComponent != null && isSnowballLoaded)
                {

                    clientMovement.playerCam.LookAt(calculatedAimPoint);
                    PlayerServerCommunication.Instance.ForceMovementUpdate();

                    PlayerInventory.Instance.UseItem();

                    return true;
                }
                return false;
            }
            catch
            {
                // Return false if any exception occurs during the process
                return false;
            }
        }

        /// <summary>
        /// Retrieves all snowball piles available in the game world.
        /// </summary>
        public static List<SnowballPile> GetSnowballPiles()
        {
            // Create a list to store the found snowball piles
            List<SnowballPile> snowballPiles = [];

            // Find all game objects whose name starts with the specified snowball pile name
            GameObject[] snowballPilesObjects = GameObject.FindObjectsOfType<GameObject>()
                .Where(go => go.name.StartsWith(SNOWBALL_PILE_GAME_OBJECT_NAME))
                .ToArray();

            // Add each valid snowball pile to the list
            foreach (GameObject snowballPileObject in snowballPilesObjects)
            {
                var pileComponent = snowballPileObject.GetComponent<SnowballPile>();
                if (pileComponent != null)
                {
                    snowballPiles.Add(pileComponent);
                }
            }
            return snowballPiles;
        }

        /// <summary>
        /// Finds the closest snowball pile relative to the given reference point.
        /// </summary>
        public static SnowballPile GetClosestSnowballPile(Vector3 referencePoint, List<SnowballPile> snowballPiles)
        {
            float shortestDistance = float.MaxValue;
            SnowballPile closestPile = null;

            // Return null if no snowball piles are available
            if (snowballPiles == null || snowballPiles.Count == 0) return null;

            // Iterate over each snowball pile and find the closest one
            foreach (var pile in snowballPiles)
            {
                if (pile == null || pile.transform == null) continue;

                GameObject rootPileGameObject = pile.transform.root.gameObject;
                float currentPileDistance = Vector3.Distance(referencePoint, rootPileGameObject.transform.position);

                // Check if the pile is more than 5 units above the reference point in the Y-axis
                float heightDifference = rootPileGameObject.transform.position.y - referencePoint.y;
                if (heightDifference > 3f) continue; // Skip piles that are more than 5f above the reference point

                // Update the closest pile if the current pile is closer
                if (currentPileDistance < shortestDistance)
                {
                    shortestDistance = currentPileDistance;
                    closestPile = pile;
                }
            }

            return closestPile;
        }



        /// <summary>
        /// Checks if the line of sight between the player and the target is clear to shoot a snowball.
        /// </summary>
        public static bool IsLineOfSightClearToShootSnowball(float maxDistance, PlayerManager target, Vector3 playerPos)
        {
            if (target == null)
            {
                return false;
            }

            int layerMask = ~LayerMask.GetMask(LAYER_MASK_INTERACT, LAYER_MASK_HURTBOX, LAYER_MASK_DETECTPLAYER, LAYER_MASK_PROXIMITY, LAYER_MASK_DEFAULT);

            Vector3 direction = (target.transform.position + TARGET_OFFSET - playerPos).normalized;

            if (Physics.Raycast(playerPos, direction, out RaycastHit hit, maxDistance, layerMask))
            {
                if (hit.transform == null)
                {
                    return false;
                }

                // Check if the hit object has a PlayerManager component
                PlayerManager playerManager = hit.transform.gameObject.GetComponent<PlayerManager>();

                if (playerManager == null)
                {
                    // No PlayerManager on the hit object, so the target is not directly visible
                    return false;
                }
                else if (playerManager == target)
                {
                    // If the raycast hits the target directly, the line of sight is clear
                    return true;
                }
            }

            // If the raycast didn't hit the target, return false
            return false;
        }

        /// <summary>
        /// Calculates the aim position for a snowball based on the sender's position, target's position, and the target's velocity.
        /// Takes into account snowball speed and gravity to predict where the snowball should be thrown.
        /// </summary>
        public static bool CalculateAimPoint(Vector3 senderPos, Vector3 senderCamPos, Vector3 targetPos, Vector3 targetVelocity, out Vector3 lookAtPosition)
        {
            // Calculate the snowball's start position (adjusted for the offset of the snowball thrower).
            Vector3 snowballStartPos = senderPos + SNOWBALL_POSITION_OFFSET;

            // Adjust the target position by adding any necessary offset (e.g., target's height).
            targetPos += TARGET_OFFSET;

            // Number of iterations to refine the future position of the target
            int iterations = 10;

            // Calculate the initial direction to the target
            Vector3 directionToTarget = targetPos - snowballStartPos;

            float launchAngle = 0f;

            // Calculate the initial horizontal distance (ignoring the Y component)
            float horizontalDistance = new Vector3(directionToTarget.x, 0, directionToTarget.z).magnitude;

            // Estimate the initial time for the snowball to reach the target
            float impactDelay = horizontalDistance / SNOWBALL_INITIAL_SPEED;

            // Calculate the initial future position of the target based on its velocity
            Vector3 futureTargetPos = targetPos + targetVelocity * impactDelay;

            for (int i = 0; i < iterations; i++)
            {
                // Update the direction with the new future position
                directionToTarget = futureTargetPos - snowballStartPos;

                // Recalculate the new horizontal distance
                float newHorizontalDistance = new Vector3(directionToTarget.x, 0, directionToTarget.z).magnitude;

                // Recalculate the time of flight based on the new distance and launch angle
                // Here we need to calculate the launch angle on each iteration
                launchAngle = CalculateLaunchAngle(snowballStartPos, futureTargetPos, SNOWBALL_INITIAL_SPEED) + LAUNCH_ANGLE_OFFSET;

                // Calculate the time of flight using the horizontal component of the initial speed
                float timeOfFlight = newHorizontalDistance / (SNOWBALL_INITIAL_SPEED * Mathf.Cos(launchAngle));

                // Adjust the future position of the target based on its movement and the estimated time
                futureTargetPos = targetPos + targetVelocity * timeOfFlight;
            }

            launchAngle = CalculateLaunchAngle(snowballStartPos, futureTargetPos, SNOWBALL_INITIAL_SPEED) + LAUNCH_ANGLE_OFFSET;

            // Adjust the height for the launch angle
            float heightAdjustment = AdjustYForLaunchAngle(futureTargetPos, snowballStartPos, launchAngle);

            // Set the final aim position by including the height adjustment to the future target position.
            lookAtPosition = futureTargetPos + new Vector3(0, heightAdjustment, 0);

            return true;
        }



        /// <summary>
        /// Calculates the launch angle required to throw a snowball from the sender to a future target position based on gravity and speed.
        /// </summary>
        public static float CalculateLaunchAngle(Vector3 senderPos, Vector3 futureTargetPos, float snowballMaxSpeed)
        {
            float gravity = Mathf.Abs(Physics.gravity.y) * SNOWBALL_GRAVITY_MULTIPLICATOR;

            // Calculate the horizontal distance between the sender and the target
            float horizontalDistance = Vector3.Distance(new Vector3(senderPos.x, 0, senderPos.z), new Vector3(futureTargetPos.x, 0, futureTargetPos.z));
            float verticalDistance =futureTargetPos.y - senderPos.y;

            // If the horizontal distance or snowball speed is invalid, return NaN
            if (horizontalDistance <= 0 || snowballMaxSpeed <= 0)
            {
                return float.NaN;
            }

            // Calculate sin(2 * theta) for the trajectory equation
            float sin2Theta = (horizontalDistance * gravity) / (snowballMaxSpeed * snowballMaxSpeed);

            // Ensure sin(2 * theta) is within the valid range [-1, 1]
            if (sin2Theta < -1 || sin2Theta > 1)
            {
                return float.NaN;
            }

            // Calculate 2 * theta using the inverse sine function
            float twoTheta = Mathf.Asin(sin2Theta);

            // Divide by 2 to get theta (the launch angle)
            float launchAngle = twoTheta / 2;

            // Convert launch angle from radians to degrees
            return (launchAngle * Mathf.Rad2Deg);
        }

        /// <summary>
        /// Adjusts the Y position (height) of the target (object B) so that the launch angle between object A and the adjusted target is the desired launch angle.
        /// </summary>
        private static float AdjustYForLaunchAngle(Vector3 targetPos, Vector3 senderPos, float launchAngle)
        {
            // Step 1: Calculate the horizontal distance (XZ-plane) between the sender and the target
            float distanceX = Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z), new Vector3(senderPos.x, 0, senderPos.z));

            // Step 2: Convert the launch angle from degrees to radians for the calculations
            float radianAngle = launchAngle * Mathf.Deg2Rad;

            // Step 3: Calculate the required change in Y (height adjustment) using the tangent of the launch angle
            // tan(angle) = opposite / adjacent -> opposite (Delta Y) = tan(angle) * adjacent (distanceX)
            float heightAdjustment = Mathf.Tan(radianAngle) * distanceX;

            // Step 4: Return the vertical adjustment needed for the target position
            return heightAdjustment;
        }



        /// <summary>
        /// Determines whether the snowball is likely to hit the player.
        /// </summary>
        public static bool IsSnowballLikelyToHit(Vector3 hitPosition, ulong senderId, out float timeToImpact, out Vector3 impactPosition, out Vector3 horizontalDirection)
        {
            timeToImpact = -1f;  // Initialize with an invalid value
            impactPosition = Vector3.zero; // Initialize with an invalid value
            horizontalDirection = Vector3.zero; // Initialize with an invalid value

            float gravity = Physics.gravity.y * SNOWBALL_GRAVITY_MULTIPLICATOR;

            // Ignore snowballs sent by the local player
            if (senderId == clientId || clientBody == null) return false;
            if (clientBody.transform.position == Vector3.zero) return false;

            PlayerManager sender = GameManager.Instance.activePlayers[senderId];
            if (sender == null) return false;

            Vector3 snowballStartPos = sender.transform.position + SNOWBALL_POSITION_OFFSET;  // Start position of the snowball

            Vector3 playerPos = clientBody.transform.position;
            Vector3 playerVelocity = clientBody.velocity;

            Vector3 directionToTarget = (hitPosition - snowballStartPos).normalized;
            horizontalDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
            float launchAngle = CalculateLaunchAngle(snowballStartPos, hitPosition) + LAUNCH_ANGLE_OFFSET;

            float distanceToPlayerXZ = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(snowballStartPos.x, 0, snowballStartPos.z));
            float horizontalSpeed = SNOWBALL_INITIAL_SPEED * Mathf.Cos(launchAngle * Mathf.Deg2Rad);
            float totalTime = distanceToPlayerXZ / horizontalSpeed;

            // Simulate snowball trajectory
            for (float t = 0; t <= totalTime; t += SIMULATION_TIME_STEP)
            {
                Vector3 snowballPosAtTimeT = CalculateSnowballPositionAtTime(snowballStartPos, directionToTarget, SNOWBALL_INITIAL_SPEED, launchAngle, gravity, t);
                CreateDebugCube(snowballPosAtTimeT);

                Vector3 predictedPlayerPos = playerPos + playerVelocity * t;

                // Check if snowball is within the player's danger zone
                if (IsWithinDangerZone(snowballPosAtTimeT, predictedPlayerPos, PLAYER_COLLISION_RADIUS))
                {
                    impactPosition = snowballPosAtTimeT;
                    timeToImpact = t;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Calculates the launch angle for the snowball trajectory.
        /// </summary>
        public static float CalculateLaunchAngle(Vector3 startPos, Vector3 hitPosition)
        {
            Vector3 directionToTarget = (hitPosition - startPos).normalized;
            float horizontalDistance = new Vector3(directionToTarget.x, 0, directionToTarget.z).magnitude;
            float verticalDistance = directionToTarget.y;

            float launchAngleRadians = Mathf.Atan2(verticalDistance, horizontalDistance);
            return launchAngleRadians * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Calculates the snowball position at a specific time during its trajectory.
        /// </summary>
        public static Vector3 CalculateSnowballPositionAtTime(Vector3 startPos, Vector3 directionToTarget, float speed, float angle, float gravity, float time)
        {
            float radianAngle = angle * Mathf.Deg2Rad;

            float horizontalSpeed = speed * Mathf.Cos(radianAngle);
            float verticalSpeed = speed * Mathf.Sin(radianAngle);

            Vector3 horizontalDisplacement = horizontalSpeed * time * new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
            float verticalDisplacement = (verticalSpeed * time) + (0.5f * gravity * time * time);

            return startPos + horizontalDisplacement + new Vector3(0, verticalDisplacement, 0);
        }

        /// <summary>
        /// Checks if the snowball is within the player's danger zone.
        /// </summary>
        public static bool IsWithinDangerZone(Vector3 snowballPos, Vector3 playerPos, Vector3 collisionRadius)
        {
            return Mathf.Abs(snowballPos.x - playerPos.x) <= collisionRadius.x + COLLISION_MARGIN &&
                   Mathf.Abs(snowballPos.y - playerPos.y) <= collisionRadius.y + COLLISION_MARGIN &&
                   Mathf.Abs(snowballPos.z - playerPos.z) <= collisionRadius.z + COLLISION_MARGIN;
        }

        /// <summary>
        /// Creates a small red cube at the snowball's position for debugging purposes.
        /// </summary>
        public static void CreateDebugCube(Vector3 position)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.localScale = TRAJECTORY_CUBE_SIZE;  // Small size for debugging
            cube.GetComponent<Renderer>().material.color = Color.red;
            GameObject.Destroy(cube, TRAJECTORY_CUBE_LIFETIME);  // Destroy cube after a short time
        }
    }

    /// <summary>
    /// Harmony patch that checks for snowball impacts after the snowball is used online.
    /// </summary>
    public class SnowballsPatch
    {
        [HarmonyPatch(typeof(SnowballComponent), nameof(SnowballComponent.OnlineUse))]
        [HarmonyPostfix]
        public static void OnSnowballOnlineUse(Vector3 __0, ulong __1)
        {
            if (!isClientBot) return;
            if (IsSnowballLikelyToHit(__0, __1, out float timeToImpact, out Vector3 impactPosition, out Vector3 horizontalDirection))
            {
                if (timeToImpact > 0f)
                {
                    ForceMessage($"<color=yellow>Detected</color> Snowball <color=red><b>Impact</b></color><color=yellow> in {timeToImpact:F2} secondes</color>");
                    if (!dangerousSnowballs.ContainsKey(impactPosition)) dangerousSnowballs.Add(impactPosition, timeToImpact);                           
                    else dangerousSnowballs[impactPosition] = timeToImpact;

                    if (!dangerousSnowballsDirections.ContainsKey(impactPosition)) dangerousSnowballsDirections.Add(impactPosition, horizontalDirection);
                    else dangerousSnowballsDirections[impactPosition] = horizontalDirection;

                }
            }
        }
    }
}