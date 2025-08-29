using AVS.Log;
using System.Collections;
using UnityEngine;

namespace AVS.Util;

/// <summary>
/// Helper for the player character.
/// </summary>
public static class Character
{
    /// <summary>
    /// Gets the current PDA status
    /// </summary>
    public static PDA.State PDA => Player.main.pda.state;

    /// <summary>
    /// Checks if the build menu is currently open.
    /// </summary>
    public static bool IsBuildMenuOpen => uGUI_BuilderMenu.IsOpen();

    /// <summary>
    /// Checks if the main menu is currently open.
    /// </summary>
    public static bool IsMainMenuOpen => IngameMenu.main.gameObject.activeSelf;

    /// <summary>
    /// Checks if any menu is currently open.
    /// </summary>
    public static bool IsAnyMenuOpen =>
        IsBuildMenuOpen || IsMainMenuOpen || PDA != global::PDA.State.Closed;

    /// <summary>
    /// Teleports the player to a specified destination.
    /// </summary>
    /// <remarks>
    /// Takes the player's current vehicle into account, if any.
    /// </remarks>
    /// <param name="destination">Target location</param>
    public static void TeleportTo(Vector3 destination)
    {
        var av = Player.main.GetAvsVehicle();
        UWE.Utils.EnterPhysicsSyncSection();
        Player.main.SetCurrentSub(null, true);
        Player.main.playerController.SetEnabled(false);

        IEnumerator waitForTeleport()
        {
            yield return null;
            Player.main.SetPosition(destination);
            Player.main.SetCurrentSub(av.SafeGetComponent<SubRoot>(), true);
            Player.main.playerController.SetEnabled(true);
            yield return null;
            UWE.Utils.ExitPhysicsSyncSection();
        }

        RootModController.AnyInstance.StartAvsCoroutine(
            nameof(Character) + '.' + nameof(TeleportTo),
            _ => waitForTeleport());
    }

    /// <summary>
    /// Grants the player invincibility for a specified duration.
    /// </summary>
    /// <param name="time">Time in seconds to become invincible</param>
    /// <param name="rmc">root mod controller instance to run the coroutine</param>
    public static void GrantInvincibility(RootModController rmc, float time)
    {
        rmc.StartAvsCoroutine(
            nameof(Character) + '.' + nameof(IntlGrantPlayerInvincibility),
            _ => IntlGrantPlayerInvincibility(3f));
    }

    private static IEnumerator IntlGrantPlayerInvincibility(float time)
    {
        Player.main.liveMixin.invincible = true;
        yield return new WaitForSeconds(time);
        Player.main.liveMixin.invincible = false;
    }

    /// <summary>
    /// Asynchronously animates the character to sit down in a chair.
    /// </summary>
    public static void SitDown(RootModController rmc)
    {
        Player.main.EnterSittingMode();
        rmc.StartAvsCoroutine(
            nameof(Character) + '.' + nameof(SitDownInChair),
            _ => SitDownInChair());
    }

    /// <summary>
    /// Sets the player inverse kinematics (IK) targets for the hands.
    /// </summary>
    /// <param name="leftHandTarget">Left hand target</param>
    /// <param name="rightHandTarget">Right hand target</param>
    /// <param name="ikArmToggleTime">Time to toggle IK arms</param>
    public static void SetArmsIKTargets(Transform? leftHandTarget, Transform? rightHandTarget,
        float ikArmToggleTime = 0.5f)
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

    /// <summary>
    /// Makes the player stand up from a seated position.
    /// </summary>
    /// <remarks>
    /// This method initiates an animation sequence to transition the player character from sitting to standing.
    /// The process is managed asynchronously and may involve resetting player animator states.
    /// </remarks>
    public static void StandUp(RootModController rmc)
    {
        rmc.StartAvsCoroutine(nameof(Character) + '.' + nameof(StandUpFromChair), _ => StandUpFromChair());
    }

    /// <summary>
    /// Exits the player to the surface at a specified surface exit location.
    /// </summary>
    /// <remarks>
    /// Used when the player exits a vehicle underwater but needs to resurface.
    /// </remarks>
    /// <param name="surfaceExitLocation">The location to exit to the surface.</param>
    /// <param name="rmc">The root mod controller instance used to start coroutines.</param>
    public static void ExitToSurface(RootModController rmc, Transform surfaceExitLocation)
    {
        rmc.StartAvsCoroutine(
            nameof(Character) + '.' + nameof(ExitToSurfaceRoutine),
            outLog => ExitToSurfaceRoutine(surfaceExitLocation, outLog));
    }

    private static IEnumerator ExitToSurfaceRoutine(Transform surfaceExitLocation, SmartLog log)
    {
        log.Write(
            $"Trying to exit player to {surfaceExitLocation.NiceName()} @{surfaceExitLocation.SafeGet(x => x.position, Vector3.zero)}");
        var tryCount = 0;
        var playerHeightBefore = Player.main.transform.position.y;
        while (Player.main.transform.position.y < 2 + playerHeightBefore)
        {
            if (!surfaceExitLocation)
            {
                log.Error("surfaceExitLocation expired. Cannot exit vehicle to surface.");
                yield break;
            }

            if (100 < tryCount)
            {
                log.Error(
                    $"Failed to exit vehicle to surface too many times while trying to relocate to {surfaceExitLocation.NiceName()} @{surfaceExitLocation.position}. Stopping.");
                yield break;
            }

            if (Player.main.currentMountedVehicle.IsNotNull())
            {
                log.Error("Cannot exit vehicle to surface while mounted.");
                yield break;
            }

            Player.main.transform.position = surfaceExitLocation.position;
            tryCount++;
            yield return null;
        }
    }
}