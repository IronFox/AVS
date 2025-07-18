using AVS.Log;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;

namespace AVS.Assets
{
    /// <summary>
    /// Represents a set of assets related to a vehicle, including models,
    /// sprites, and fragments, loaded from an asset bundle.
    /// </summary>
    public readonly struct VehicleAssets
    {
        /// <summary>
        /// Retrieved model.
        /// Null if this asset was not loaded for a model.
        /// </summary>
        public GameObject? Model { get; }
        /// <summary>
        /// Retrieved ping sprite.
        /// Null if this asset was not loaded for sprite atlas.
        /// </summary>
        public Atlas.Sprite? Ping { get; }
        /// <summary>
        /// Retrieved crafter sprite.
        /// Null if this asset was not loaded for sprite atlas.
        /// </summary>
        public Atlas.Sprite? Crafter { get; }
        /// <summary>
        /// Unlock sprite.
        /// Null if this asset was not loaded for sprite atlas.
        /// </summary>
        public Sprite? Unlock { get; }
        /// <summary>
        /// Fragment GameObject.
        /// Null if this asset was not loaded for a fragment.
        /// </summary>
        public GameObject? Fragment { get; }
        /// <summary>
        /// Gets the asset bundle interface used to load these assets.
        /// </summary>
        public AssetBundleInterface AssetBundleInterface { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleAssets"/> struct.
        /// </summary>
        /// <param name="abi">The asset bundle interface.</param>
        /// <param name="model">The vehicle model GameObject.</param>
        /// <param name="ping">The ping sprite.</param>
        /// <param name="crafter">The crafter sprite.</param>
        /// <param name="fragment">The fragment GameObject.</param>
        /// <param name="unlock">The unlock sprite.</param>
        public VehicleAssets(
            AssetBundleInterface abi,
            GameObject? model,
            Atlas.Sprite? ping,
            Atlas.Sprite? crafter,
            GameObject? fragment,
            Sprite? unlock)
        {
            Model = model;
            Ping = ping;
            Crafter = crafter;
            Fragment = fragment;
            Unlock = unlock;
            AssetBundleInterface = abi;
        }

        /// <summary>
        /// Unloads the asset bundle associated with these assets.
        /// </summary>
        public void Close()
        {
            AssetBundleInterface.CloseBundle();
        }
    }
    /// <summary>
    /// Provides methods for loading and managing assets from a Unity asset bundle.
    /// </summary>
    public class AssetBundleInterface
    {
        internal string bundleName;
        internal AssetBundle? bundle;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetBundleInterface"/> class and loads the asset bundle from the specified path.
        /// </summary>
        /// <param name="bundlePath">The file path to the asset bundle.</param>
        internal AssetBundleInterface(string bundlePath)
        {
            bundleName = bundlePath;
            try
            {
                LogWriter.Default.Write($"Loading asset bundle from {bundlePath}");
                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle)
                    LogWriter.Default.Write($"Loaded asset bundle from {bundlePath}");
                else
                    LogWriter.Default.Write($"Loaded null asset bundle from {bundlePath}");
            }
            catch (Exception e)
            {
                LogWriter.Default.Error($"AssetBundleInterface failed to load AssetBundle with the path: {bundlePath}. Make sure the name is correct.", e);
                return;
            }
        }

        /// <summary>
        /// Loads a sprite atlas from the asset bundle.
        /// </summary>
        /// <param name="spriteAtlasName">The name of the sprite atlas.</param>
        /// <returns>The loaded <see cref="SpriteAtlas"/>, or null if not found.</returns>
        internal SpriteAtlas? GetSpriteAtlas(string spriteAtlasName)
        {
            if (bundle == null)
            {
                LogWriter.Default.Error($"AssetBundle {bundleName} is not loaded. Cannot get sprite atlas");
                return null;
            }
            SpriteAtlas? rs = null;
            try
            {
                rs = bundle.LoadAsset<SpriteAtlas>(spriteAtlasName);
            }
            catch
            {
                try
                {
                    rs = bundle.LoadAsset<SpriteAtlas>($"{spriteAtlasName}.spriteatlas");
                }
                catch (Exception e)
                {
                    LogWriter.Default.Error($"AssetBundle {bundleName} failed to get Sprite Atlas: {spriteAtlasName}.", e);
                    return null;
                }
            }
            if (!rs)
                LogWriter.Default.Error($"AssetBundle {bundleName} failed to get Sprite Atlas: {spriteAtlasName}.");
            return rs;
        }

        /// <summary>
        /// Loads a sprite from a sprite atlas in the asset bundle and wraps it in an <see cref="Atlas.Sprite"/>.
        /// </summary>
        /// <param name="spriteAtlasName">The name of the sprite atlas.</param>
        /// <param name="spriteName">The name of the sprite.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        internal Atlas.Sprite? GetSprite(string spriteAtlasName, string spriteName)
        {
            var thisAtlas = GetSpriteAtlas(spriteAtlasName);
            if (thisAtlas == null)
                return null;
            try
            {
                var ping = thisAtlas.GetSprite(spriteName);
                return new Atlas.Sprite(ping);
            }
            catch (Exception e)
            {
                LogWriter.Default.Error($"In AssetBundle {bundleName}, failed to get Sprite {spriteName} from Sprite Atlas {spriteAtlasName}.", e);
                return null;
            }
        }

        /// <summary>
        /// Loads a raw Unity <see cref="Sprite"/> from a sprite atlas in the asset bundle.
        /// </summary>
        /// <param name="spriteAtlasName">The name of the sprite atlas.</param>
        /// <param name="spriteName">The name of the sprite.</param>
        /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
        internal Sprite? GetRawSprite(string spriteAtlasName, string spriteName)
        {
            var thisAtlas = GetSpriteAtlas(spriteAtlasName);
            if (thisAtlas == null)
                return null;
            try
            {
                return thisAtlas.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                LogWriter.Default.Error($"In AssetBundle {bundleName}, failed to get Sprite {spriteName} from Sprite Atlas {spriteAtlasName}.", e);
                return null;
            }
        }

        /// <summary>
        /// Loads a <see cref="GameObject"/> from the asset bundle.
        /// </summary>
        /// <param name="gameObjectName">The name of the GameObject.</param>
        /// <returns>The loaded <see cref="GameObject"/>, or null if not found.</returns>
        internal GameObject? GetGameObject(string gameObjectName)
        {
            if (bundle == null)
            {
                LogWriter.Default.Error($"AssetBundle {bundleName} is not loaded. Cannot get GameObject {gameObjectName}");
                return null;
            }
            try
            {
                return bundle.LoadAsset<GameObject>(gameObjectName);
            }
            catch
            {
                try
                {
                    return bundle.LoadAsset<GameObject>($"{gameObjectName}.prefab");
                }
                catch (Exception e)
                {
                    LogWriter.Default.Error($"AssetBundle {bundleName} failed to get Sprite Atlas: {gameObjectName}.", e);
                    return null;
                }
            }
        }

        /// <summary>
        /// Loads an <see cref="AudioClip"/> from a prefab in the asset bundle by name.
        /// </summary>
        /// <param name="prefabName">The name of the prefab containing the audio source.</param>
        /// <param name="clipName">The name of the audio clip.</param>
        /// <returns>The loaded <see cref="AudioClip"/>, or null if not found.</returns>
        internal AudioClip? GetAudioClip(string prefabName, string clipName)
        {
            var go = GetGameObject(prefabName);
            if (go == null)
                return null;
            return
                go
                .GetComponents<AudioSource>()
                .Select(x => x.clip)
                .Where(x => x.name == clipName)
                .FirstOrDefault();
        }

        /// <summary>
        /// Loads vehicle-related assets from an asset bundle.
        /// </summary>
        /// <param name="bundleName">The name of the asset bundle file.</param>
        /// <param name="modelName">The name of the vehicle model GameObject.</param>
        /// <param name="spriteAtlasName">The name of the sprite atlas.</param>
        /// <param name="pingSpriteName">The name of the ping sprite.</param>
        /// <param name="crafterSpriteName">The name of the crafter sprite.</param>
        /// <param name="fragmentName">The name of the fragment GameObject.</param>
        /// <param name="unlockName">The name of the unlock sprite.</param>
        /// <returns>A <see cref="VehicleAssets"/> struct containing the loaded assets.</returns>
        public static VehicleAssets GetVehicleAssetsFromBundle(string bundleName, string modelName = "", string spriteAtlasName = "", string pingSpriteName = "", string crafterSpriteName = "", string fragmentName = "", string unlockName = "")
        {
            string directoryPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string bundlePath = Path.Combine(directoryPath, bundleName);
            AssetBundleInterface abi = new AssetBundleInterface(bundlePath);
            GameObject? model = null;
            GameObject? fragment = null;
            Atlas.Sprite? ping = null;
            Atlas.Sprite? crafter = null;
            Sprite? unlock = null;
            //result.abi = abi;
            if (!string.IsNullOrEmpty(modelName))
            {
                model = abi.GetGameObject(modelName);
            }
            if (spriteAtlasName != "")
            {
                if (pingSpriteName != "")
                {
                    ping = abi.GetSprite(spriteAtlasName, pingSpriteName);
                }
                if (crafterSpriteName != "")
                {
                    crafter = abi.GetSprite(spriteAtlasName, crafterSpriteName);
                }
                if (unlockName != "")
                {
                    unlock = abi.GetRawSprite(spriteAtlasName, unlockName);
                }
            }
            if (fragmentName != "")
            {
                fragment = abi.GetGameObject(fragmentName);
            }
            return new VehicleAssets(
                abi: abi,
                model: model,
                ping: ping,
                crafter: crafter,
                fragment: fragment,
                unlock: unlock
                );
        }

        /// <summary>
        /// Loads an additional <see cref="GameObject"/> from the asset bundle.
        /// </summary>
        /// <param name="abi">The asset bundle interface.</param>
        /// <param name="modelName">The name of the GameObject.</param>
        /// <returns>The loaded <see cref="GameObject"/>, or null if not found.</returns>
        public static GameObject? LoadAdditionalGameObject(AssetBundleInterface abi, string modelName)
        {
            return abi.GetGameObject(modelName);
        }

        /// <summary>
        /// Loads an additional <see cref="Atlas.Sprite"/> from the asset bundle.
        /// </summary>
        /// <param name="abi">The asset bundle interface.</param>
        /// <param name="SpriteAtlasName">The name of the sprite atlas.</param>
        /// <param name="SpriteName">The name of the sprite.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        public static Atlas.Sprite? LoadAdditionalSprite(AssetBundleInterface abi, string SpriteAtlasName, string SpriteName)
        {
            return abi.GetSprite(SpriteAtlasName, SpriteName);
        }

        /// <summary>
        /// Loads an additional raw <see cref="Sprite"/> from the asset bundle.
        /// </summary>
        /// <param name="abi">The asset bundle interface.</param>
        /// <param name="SpriteAtlasName">The name of the sprite atlas.</param>
        /// <param name="SpriteName">The name of the sprite.</param>
        /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
        public static Sprite? LoadAdditionalRawSprite(AssetBundleInterface abi, string SpriteAtlasName, string SpriteName)
        {
            return abi.GetRawSprite(SpriteAtlasName, SpriteName);
        }

        /// <summary>
        /// Loads an <see cref="AudioClip"/> from the asset bundle.
        /// </summary>
        /// <param name="abi">The asset bundle interface.</param>
        /// <param name="prefabName">The name of the prefab containing the audio source.</param>
        /// <param name="clipName">The name of the audio clip.</param>
        /// <returns>The loaded <see cref="AudioClip"/>, or null if not found.</returns>
        public static AudioClip? LoadAudioClip(AssetBundleInterface abi, string prefabName, string clipName)
        {
            return abi.GetAudioClip(prefabName, clipName);
        }

        /// <summary>
        /// Unloads the asset bundle from memory.
        /// </summary>
        public void CloseBundle()
        {
            if (bundle != null)
                bundle.Unload(false);
        }
    }
}
