dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
mkdir -Force "%SUBNAUTICA_PATH%\Subnautica_Data\Managed\publicized_assemblies"
assembly-publicizer "%SUBNAUTICA_PATH%\Subnautica_Data\Managed\Assembly-CSharp.dll" -o "%SUBNAUTICA_PATH%\Subnautica_Data\Managed\publicized_assemblies\Assembly-CSharp_publicized.dll"
assembly-publicizer "%SUBNAUTICA_PATH%\Subnautica_Data\Managed\Assembly-CSharp-firstpass.dll" -o "%SUBNAUTICA_PATH%\Subnautica_Data\Managed\publicized_assemblies\Assembly-CSharp-firstpass_publicized.dll"