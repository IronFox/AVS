# AVS
Aquatic Vehicle System: A Subnautica vehicle infrastructure derived from https://github.com/NeisesMike/VehicleFramework

The code foundation has mostly been cooked up by Mikjaw/Mike Neises and all credit (and blame) for most of AVS's functionality should go their way.
If in doubt, assume all code, that has been present in the 'Initial Commit' commit, to come from there.

## Main Differences to Vehicle Framework

1) AVS is a library to be shipped with the mod, not a mod in itself. It will not update on its own.
2) AVS has no configuration of its own. Everything configurable must come from the dependent mod or won't be modifable by the end user.
3) AVS does not support arms or walkers. Supporting types and patches have been removed.
	The default Subnautica sub behavior "right mouse button=toggle flashlight" is hard coded.
4) AVS heavily re-engineered the communication between the base vehicle infrastructure and the client vehicle.
	Instead of a bazillion virtual methods, it receives input from the client vehicle via configurations through the constructor
	and composition resolved during Awake().
	Both configuration and composition are read-only and verified on construct.
5) AVS takes full advantage of modern C#'s nullable concept.
	Although it is not straighforward to apply the concept to Unity objects, it is still possible and has been done everywhere.
	AVS also uses readonly types where possible, particularly when dealing with structs.
	It favors properties where it is technically feasible to use them, increasing code readability.
	A lot of VF bugs have been fixed or mitigated by these changes alone.
6) AVS introduced a lot of Unity object helper extensions that mitigate some of the null issues that revolve around the fact
	that Unity objects are never truly null but may behave as though they were (?., ??, 'is null' are particularly dangerous constructs).
7) AVS has the Echelon's material adaptation built-in with major improvements and fixes.
8) AVS does not load or produce sounds on its own. Engines are silent on their own.
	The autopilot voice has been replaced with an event system.
	The client vehicle can listen to these events and use a pre-configured voice queue to play batches of sounds.
	The voice queue, while based on the VF voice queue, now uses a hard priority, single-batch system.
	No more than one batch can be enqueued at once.
	A batch is a string of sounds to be played one after the other with configurable gaps inbetween.
	If more than one batch is enqueued before the previous is done, depending in priorities,
	either the previous one is interrupted or the new one is discarded.
	This prevents endless strings of voice lines that are no longer relevant when they finally play.
9) The drive system is simplified and now enforced. AVS does not instantiate a drive on its own and requires the client vehicle to consider this.
10) AVS provides the sound source system used by the Echelon out of the box. This allows placing spatial sound sources that behave mostly naturally.




## Setup

Add the following environment variable:
SUBNAUTICA_PATH = [your Subnautica install directory WITHOUT trailing \ ]


Create the required publicized Subnautica DLL:

1) dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
2) mkdir -Force "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\publicized_assemblies"
3) assembly-publicizer "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\Assembly-CSharp.dll" -o "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\publicized_assemblies\Assembly-CSharp_publicized.dll"

It should now build and produce the dll required for mod creation.