using static GibsonBot.InputConstants;

namespace GibsonBot
{
    internal class InputConstants
    {
        public const float OBSTACLE_FEETS_DETECTION_RANGE = 1f;
        public const float OBSTACLE_BODY_DETECTION_RANGE = 1f;
        public const float OBSTACLE_HEAD_DETECTION_RANGE = 1f;
        public const float MINIMAL_SPEED_TO_BUNNY_HOP = 14f;
        public const float MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP = 7f;
        public const float MAX_ALLOWED_ANGLE_FOR_BUNNY_HOP = 5f;
    }
    internal class InputState
    {
        public static bool inputForward, inputBackwards, inputLeft, inputRight, inputSprint, inputCrouch;
    }
    internal class InputSettings
    {
        public static KeyCode forward = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.forward;
        public static KeyCode backwards = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.backwards;
        public static KeyCode left = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.left;
        public static KeyCode right = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.right;
        public static KeyCode sprint = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.sprint;
        public static KeyCode crouch = (KeyCode)MonoBehaviourPublicInfobaInlerijuIncrspUnique.crouch;
    }
    internal class InputPatchs
    {
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) })]
        [HarmonyPostfix]
        public static void InputGetKeyPostfix(KeyCode __0, ref bool __result)
        {
            if (__0 == InputSettings.forward && inputForward) __result = true;
            if (__0 == InputSettings.backwards && inputBackwards) __result = true;
            if (__0 == InputSettings.left && inputLeft) __result = true;
            if (__0 == InputSettings.right && inputRight) __result = true;
            if (__0 == InputSettings.sprint && inputSprint) __result = true;
            if (__0 == InputSettings.crouch && inputCrouch) __result = true;

        }
    }
    public class InputManager : MonoBehaviour
    {
        // Flags to control the state of the hyperglide simulation
        static public bool hyperglideJump, hyperglideGroundMaxSpeedReached, hyperglideCrouch;
        static public bool wallBoostJump;
        static public string hyperglideSide, bunnyHopSide;
        static public Vector3 theDestination;
        static public float stopThreshold = 1f, hyperglideSpeed = 10;
        static public bool simulateStopASAP, simulateJump, simulateWallBoost, simulateBunnyHop, simulateMove, onLadder;
        public static List<GameObject> visualizationMarkers = [];
        public static float feetOffset = -1.61f, bodyOffset = 1.43f, headOffset = 2f;

        static public float elapsed;


        // Update is called once per frame
        void Update()
        {
            // Increment time for timing-based actions
            elapsed += Time.deltaTime;

            // If hyperglide simulation is active, perform actions based on conditions
            if (simulateJump)
            {
                PerformJump(theDestination);
            }
        }

        // LateUpdate is called after all Update functions, resets the inputs every frame
        void LateUpdate()
        {
            ResetInputs();
        }

        public static void Move(Vector3 destination, Vector3 playerPos)
        {
            ManageObstacle(destination, playerPos); 

            BotFunctions.LookAtTarget(destination);
            Sprint();
            Forward();
        }

        public static void MoveWithoutObstacle(Vector3 destination)
        {
            BotFunctions.LookAtTarget(destination);
            Sprint();
            Forward();
        }

        public static void MoveDiagonaly(Vector3 destination, Vector3 playerPos)
        {
            ManageObstacle(destination, playerPos);

            AdjustCameraRotation("right", destination);
            Sprint();
            Forward();
            Right();
        }

        public static void Stop()
        {

            Vector3 currentVelocity = clientBody.velocity;

            if (currentVelocity.magnitude < stopThreshold) simulateStopASAP = false;


            Vector3 forward = clientMovement.playerCam.forward.normalized;
            Vector3 right = clientMovement.playerCam.right.normalized;


            float forwardSpeed = Vector3.Dot(currentVelocity, forward); 
            float rightSpeed = Vector3.Dot(currentVelocity, right);     

            if (forwardSpeed > 0.1f) 
            {
                Backwards();  
            }
            else if (forwardSpeed < -0.1f)  
            {
                Forward();  
            }


            if (rightSpeed > 0.1f) 
            {
                Left();  
            }
            else if (rightSpeed < -0.1f)  
            {
                Right();  
            }
        }

        public static void MoveWithBunnyHopForPathFinding(Vector3 destination, Vector3 playerPos, bool canJump)
        {
            MoveDiagonaly(destination, playerPos);

            float playerSpeed = clientBody.velocity.magnitude;

            Vector3 directionToDestination = (destination - playerPos).normalized;

            Vector3 playerVelocityDirection = clientBody.velocity.normalized;

            float angleBetween = Vector3.Angle(directionToDestination, playerVelocityDirection);

            float distanceToDestination = Vector3.Distance(destination, clientBody.transform.position);


            if (clientMovement.grounded
                && canJump
                && playerSpeed >= MINIMAL_SPEED_TO_BUNNY_HOP
                && distanceToDestination > MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP
                && angleBetween < MAX_ALLOWED_ANGLE_FOR_BUNNY_HOP
                && !simulateJump) 
            {
                StartJump(destination);
            }
        }

        public static void MoveWithBunnyHop(Vector3 destination, Vector3 playerPos, float MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP)
        {
            ManageObstacle(destination, playerPos);

            AdjustCameraRotation("right", destination);
            Sprint();
            Forward();
            Right();

            float playerSpeed = clientBody.velocity.magnitude;

            Vector3 directionToDestination = (destination - playerPos).normalized;

            Vector3 playerVelocityDirection = clientBody.velocity.normalized;

            float angleBetween = Vector3.Angle(directionToDestination, playerVelocityDirection);

            float distanceToDestination = Vector3.Distance(destination, clientBody.transform.position);


            if (clientMovement.grounded 
                && playerSpeed >= MINIMAL_SPEED_TO_BUNNY_HOP
                && distanceToDestination > MINIMAL_DISTANCE_TO_DESTINATION_TO_BUNNY_HOP
                && angleBetween < MAX_ALLOWED_ANGLE_FOR_BUNNY_HOP) // Condition ajoutée pour vérifier l'angle
            {
                Jump();
            }
        }




        static void CreateRaycastVisual(Vector3 origin, Vector3 direction, float length)
        {
            // Create a stretched cube to represent the raycast
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = origin + direction.normalized * (length / 2);
            cube.transform.forward = direction;
            cube.transform.localScale = new Vector3(0.05f, 0.05f, length); // Stretched in the Z direction

            cube.GetComponent<Renderer>().material.color = Color.red;
            visualizationMarkers.Add(cube);

            // Destroy the cube after 0.2 seconds
            Destroy(cube, 0.2f);
        }
        private static void ManageObstacle(Vector3 destination, Vector3 playerPos)
        {

            if (clientMovement.field_Private_Boolean_6) return; // Doesnt apply on ladders

            int layerMask = ~(LayerMask.GetMask("Interact", "Hurtbox", "DetectPlayer", "Proximity", "Default")
                  | (1 << 8) 
                  | (1 << 1) 
                  | (1 << 18));
            Vector3 raycastOrigin;
            RaycastHit hit;
            Vector3 feetHitNormal = Vector3.zero;
            bool feetHit = false, bodyHit = false, headHit = false;
            float feetHitDistance = 0f, bodyHitDistance = 0f;

            Vector3 directionToDestination = destination - playerPos;
            Vector3 raycastDirection = new Vector3(directionToDestination.x, 0, directionToDestination.z).normalized;



            raycastOrigin = new Vector3(playerPos.x, playerPos.y + feetOffset, playerPos.z);

            // Raycast at feet level
            if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, OBSTACLE_FEETS_DETECTION_RANGE, layerMask))
            {
                feetHitNormal = hit.normal;
                feetHit = true;
                feetHitDistance = hit.distance;
                CreateRaycastVisual(raycastOrigin, raycastDirection, OBSTACLE_FEETS_DETECTION_RANGE); // Visualize the feet raycast
            }

            if (feetHitDistance == 0f) return;

            // Raycast at body level
            raycastOrigin = new Vector3(playerPos.x, playerPos.y + bodyOffset, playerPos.z);
            if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, feetHitDistance, layerMask))
            {
                if (hit.transform.gameObject.layer == 8) return;
                bodyHit = true;
                CreateRaycastVisual(raycastOrigin, raycastDirection, feetHitDistance); // Visualize the body raycast
                bodyHitDistance = hit.distance;
            }

            // Raycast at head level
            raycastOrigin = new Vector3(playerPos.x, playerPos.y + headOffset, playerPos.z);
            if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, feetHitDistance, layerMask))
            {
                if (hit.transform.gameObject.layer == 8) return;
                headHit = true;
                CreateRaycastVisual(raycastOrigin, raycastDirection, feetHitDistance); // Visualize the head raycast
            }

            float slopeAngle = Vector3.Angle(feetHitNormal, Vector3.up) * 2;

            if (feetHit && !bodyHit && !headHit) // Obstacle or Ramp
            {
                if (slopeAngle >= 90) Jump();
            }
            else if (!headHit)
            {
                if (bodyHitDistance > feetHitDistance * 2) Jump();
            }
        }

        public static float CalculateDistanceToSlope(float height1, float height2, float slopeAngle, float distance1ToSlope)
        {

            float deltaHeight = height2 - height1;

            float slopeAngleRadians = Mathf.Deg2Rad * slopeAngle;

            float deltaDistance = deltaHeight / Mathf.Tan(slopeAngleRadians);

            float distance2ToSlope = distance1ToSlope + deltaDistance;

            return distance2ToSlope;
        }


        // Initializes and starts the hyperglide simulation
        public static void StartJump(Vector3 destination)
        {
            theDestination = destination;
            hyperglideSide = "right";
            simulateJump = true;
            hyperglideJump = false;
            hyperglideGroundMaxSpeedReached = false;
            hyperglideCrouch = false;
            elapsed = 0f;  // Reset elapsed time
        }

        public static void AdjustCameraRotation(string side, Vector3 destination)
        {
            if (clientMovement == null) return;
            if (clientMovement.playerCam == null) return;
            Vector3 directionToDestination = (destination - clientMovement.playerCam.transform.position).normalized;

            Vector3 upAxis = Vector3.up;

            float angle = (side == "right") ? -45f : 45f;

            Quaternion rotationAdjustment = Quaternion.AngleAxis(angle, upAxis);

            Vector3 rotatedDirection = rotationAdjustment * directionToDestination;

            clientMovement.playerCam.rotation = Quaternion.LookRotation(rotatedDirection, upAxis);
        }

        // Handles the hyperglide simulation logic
        private void PerformJump(Vector3 destination)
        {
            // Continuously adjust camera rotation towards the destination
            AdjustCameraRotation("right", destination);

            Sprint();
            Forward();
            Right();

            // Calculate the horizontal velocity of the client (ignoring y component)
            float clientVelocity = new Vector3(clientBody.velocity.x, 0, clientBody.velocity.z).magnitude;

            float destinationDistance = Vector3.Distance(clientBody.transform.position, destination);

            float crouchDelay = 0.15f;
            if (!hyperglideCrouch) crouchDelay = (destinationDistance - 3f) / 100;
            if (crouchDelay > 0.15f) crouchDelay = 0.15f;

            float groundSpeed = 10f;
            groundSpeed = destinationDistance - 7f;
            if (groundSpeed > 10f) groundSpeed = 10f;


            float timeRemaining = 0.84f - elapsed;
            float predictedDistance = clientVelocity * timeRemaining;


            if (predictedDistance > destinationDistance) Backwards();

            // Check if ground max speed is reached for hyperglide initiation
            if (!hyperglideGroundMaxSpeedReached && clientVelocity >= groundSpeed)
            {
                hyperglideGroundMaxSpeedReached = true;
                elapsed = 0f;  // Reset elapsed time for jump tracking
            }

            // Execute jump when ground max speed is reached and jump hasn't occurred yet
            if (!hyperglideJump && hyperglideGroundMaxSpeedReached)
            {
                hyperglideJump = true;
                Jump();
            }

            // Check if hyperglide has landed after the jump
            if (hyperglideJump && elapsed > 0.2f && clientMovement.grounded)
            {
                EndJump();
            }

            if (destinationDistance <= 15f) hyperglideCrouch = true;

            // Handle crouching during hyperglide if speed threshold is met
            if (hyperglideJump && elapsed > crouchDelay && !hyperglideCrouch)
            {
                if (elapsed > crouchDelay+0.01f) hyperglideCrouch = true;

                Crouch();
            }
        }


        // Ends the hyperglide simulation and logs the distance traveled
        private void EndJump()
        {
            simulateJump = false;  // End simulation
        }

        // Resets all input flags to false, called at the end of every frame
        private void ResetInputs()
        {
            inputForward = false;
            inputBackwards = false;
            inputLeft = false;
            inputRight = false;
            inputSprint = false;
            inputCrouch = false;
        }

        // Simulates jump action
        private static void Jump()
        {
            clientMovement.Jump();
        }

        // Input functions to set movement and actions
        public static void Forward() => inputForward = true;
        public static void Backwards() => inputBackwards = true;
        public static void Left() => inputLeft = true;
        public static void Right() => inputRight = true;
        public static void Sprint() => inputSprint = true;
        public static void Crouch() => inputCrouch = true;
    }

}
