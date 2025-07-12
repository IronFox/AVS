# AVS
Aquatic Vehicle System: A Subnautica vehicle infrastructure derived from https://github.com/NeisesMike/VehicleFramework


## Setup

Setup environment variable SUBNAUTICA_PATH = [your Subnautica install directory WITHOUT trailing \ ]


Publicize DLL:

dotnet tool install -g BepInEx.AssemblyPublicizer.Cli

mkdir -Force "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\publicized_assemblies"

assembly-publicizer "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\Assembly-CSharp.dll" -o "$Env:SUBNAUTICA_PATH\Subnautica_Data\Managed\publicized_assemblies\Assembly-CSharp_publicized.dll"