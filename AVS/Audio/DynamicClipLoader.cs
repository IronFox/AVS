using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AVS.Audio
{
    /// <summary>
    /// Represents an asynchronous operation that will eventually yield a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    public class AsyncPromise<T> where T : UnityEngine.Object
    {
        /// <summary>
        /// Gets the value of the promise if resolved.
        /// </summary>
        public T? Value { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the promise has completed (either resolved or rejected).
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the promise was rejected due to an error.
        /// </summary>
        public bool IsError { get; private set; }
        /// <summary>
        /// Error message if the promise was rejected.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Resolves the promise with the specified value.
        /// </summary>
        /// <param name="value">The value to resolve the promise with.</param>
        internal void Resolve(T value)
        {
            Value = value;
            IsDone = true;
        }

        /// <summary>
        /// Rejects the promise, indicating an error occurred.
        /// </summary>
        internal void Reject(string message)
        {
            IsError = true;
            IsDone = true;
            ErrorMessage = message;
        }
    }
    /// <summary>
    /// Provides asynchronous loading and caching of <see cref="AudioClip"/> assets.
    /// </summary>
    public static class DynamicClipLoader
    {
        /// <summary>
        /// Caches promises for audio clips by file path.
        /// </summary>
        private static Dictionary<string, AsyncPromise<AudioClip>> AudioClipPromises { get; }
            = new Dictionary<string, AsyncPromise<AudioClip>>();

        /// <summary>
        /// Asynchronously gets an <see cref="AudioClip"/> from the specified file path.
        /// If the clip is already being loaded, returns the existing promise.
        /// </summary>
        /// <param name="filePath">The file path to the audio clip.</param>
        /// <returns>An <see cref="AsyncPromise{AudioClip}"/> representing the loading operation.</returns>
        public static AsyncPromise<AudioClip> GetAudioClipAsync(string filePath)
        {
            if (AudioClipPromises.TryGetValue(filePath, out AsyncPromise<AudioClip> existingPromise))
            {
                return existingPromise;
            }
            AsyncPromise<AudioClip> promise = new AsyncPromise<AudioClip>();
            MainPatcher.Instance.StartCoroutine(LoadAudioClip(filePath, promise.Resolve, promise.Reject));
            AudioClipPromises[filePath] = promise;
            return promise;
        }

        /// <summary>
        /// Coroutine that loads an <see cref="AudioClip"/> from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to the audio clip.</param>
        /// <param name="onSuccess">Callback invoked with the loaded <see cref="AudioClip"/> on success.</param>
        /// <param name="onError">Callback invoked if loading fails.</param>
        /// <returns>An <see cref="IEnumerator"/> for use with Unity coroutines.</returns>
        public static IEnumerator LoadAudioClip(string filePath, Action<AudioClip> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.isHttpError || www.isNetworkError)
                {
                    onError?.Invoke(www.error);
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (!clip)
                    {
                        Logger.Error("Failed to retrieve AudioClip from file: " + filePath);
                        onError?.Invoke($"Resulting clip is null");
                    }
                    else
                    {
                        onSuccess?.Invoke(clip);
                    }
                }
            }
        }
    }
}
