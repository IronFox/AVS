using System.Collections;
using UnityEngine;

namespace AVS.Util
{
    /// <summary>
    /// Helper for the player character.
    /// </summary>
    public static class Character
    {
        /// <summary>
        /// Teleports the player to a specified destination.
        /// </summary>
        /// <remarks>
        /// Takes the player's current vehicle into account, if any.
        /// </remarks>
        /// <param name="destination">Target location</param>
        public static void TeleportTo(Vector3 destination)
        {
            var mv = Player.main.GetModVehicle();
            UWE.Utils.EnterPhysicsSyncSection();
            Player.main.SetCurrentSub(null, true);
            Player.main.playerController.SetEnabled(false);
            IEnumerator waitForTeleport()
            {
                yield return null;
                Player.main.SetPosition(destination);
                Player.main.SetCurrentSub(mv.SafeGetComponent<SubRoot>(), true);
                Player.main.playerController.SetEnabled(true);
                yield return null;
                UWE.Utils.ExitPhysicsSyncSection();
            }
            UWE.CoroutineHost.StartCoroutine(waitForTeleport());
        }



        /// <summary>
        /// Asynchronously animates the character to sit down in a chair.
        /// </summary>
        public static void SitDown()
        {
            Player.main.EnterSittingMode();
            UWE.CoroutineHost.StartCoroutine(SitDownInChair());
        }

        /// <summary>
        /// Sets the player inverse kinematics (IK) targets for the hands.
        /// </summary>
        /// <param name="leftHandTarget">Left hand target</param>
        /// <param name="rightHandTarget">Right hand target</param>
        /// <param name="ikArmToggleTime">Time to toggle IK arms</param>
        public static void SetArmsIKTargets(Transform? leftHandTarget, Transform? rightHandTarget, float ikArmToggleTime = 0.5f)
        {
            Player.main.armsController.ikToggleTime = ikArmToggleTime;
            Player.main.armsController.SetWorldIKTarget(leftHandTarget, rightHandTarget);
        }

        private static IEnumerator SitDownInChair()
        {
            Player.main.playerAnimator.SetBool("chair_sit", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_sit", false);
            Player.main.playerAnimator.speed = 100f;
            yield return new WaitForSeconds(0.05f);
            Player.main.playerAnimator.speed = 1f;
        }
        private static IEnumerator StandUpFromChair()
        {
            Player.main.playerAnimator.SetBool("chair_stand_up", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_stand_up", false);
            Player.main.playerAnimator.speed = 100f;
            yield return new WaitForSeconds(0.05f);
            Player.main.playerAnimator.speed = 1f;
            yield return null;
        }
        private static IEnumerator EventuallyStandUp()
        {
            yield return new WaitForSeconds(1f);
            yield return StandUpFromChair();
            uGUI.main.quickSlots.SetTarget(null);
            Player.main.currentMountedVehicle = null;
            Player.main.transform.SetParent(null);
        }

        public static void StandUp()
        {
            UWE.CoroutineHost.StartCoroutine(StandUpFromChair());
        }

        public static void ExitToSurface(Transform surfaceExitLocation)
        {
            Player.main.StartCoroutine(ExitToSurfaceRoutine(surfaceExitLocation));
        }

        private static IEnumerator ExitToSurfaceRoutine(Transform surfaceExitLocation)
        {
            int tryCount = 0;
            float playerHeightBefore = Player.main.transform.position.y;
            while (Player.main.transform.position.y < 2 + playerHeightBefore)
            {
                if (100 < tryCount)
                {
                    Logger.Error("Error: Failed to exit vehicle too many times. Stopping.");
                    yield break;
                }
                if (surfaceExitLocation == null)
                {
                    Logger.Error("Error: surfaceExitLocation is null. Cannot exit vehicle to surface.");
                    yield break;
                }
                Player.main.transform.position = surfaceExitLocation.position;
                tryCount++;
                yield return null;
            }
        }
    }
}
