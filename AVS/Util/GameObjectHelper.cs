using System.Collections.Generic;
using UnityEngine;

namespace AVS.Util
{
    public static class GameObjectHelper
    {
        public static T CopyComponentWithFieldsTo<T>(this T original, GameObject destination) where T : Component
        {
            if (!original)
            {
                Logger.Error($"Original component of type {typeof(T).Name} is null, cannot copy.");
                return null;
            }
            System.Type type = original.GetType();
            T copy = (T)destination.EnsureComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }


        public static T Or<T>(this T a, T b) where T : Object
        {
            if (a)
                return a;
            return b;
        }
        public static void LoggedSetActive(this GameObject gameObject, bool value)
        {
            if (gameObject == null)
            {
                Logger.Error("GameObject is null, cannot set active state.");
                return;
            }
            try
            {
                if (gameObject.activeSelf != value)
                {
                    Logger.DebugLog($"Setting active state of {gameObject.NiceName()} to {value}");
                    gameObject.SetActive(value);
                    Logger.DebugLog($"Set active state of {gameObject.NiceName()} to {value}");
                }
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to set active state of {gameObject.NiceName()}: {e.Message}");
                Logger.LogException("Exception details:", e);
            }
        }

        public static Transform GetTransform(this GameObject gameObject)
        {
            if (gameObject == null)
                return null;
            return gameObject.transform;
        }

        public static Transform GetTransform(this Component component)
        {
            if (component == null)
                return null;
            return component.transform;
        }
        public static GameObject GetGameObject(this Component component)
        {
            if (component == null)
                return null;
            return component.gameObject;
        }

        public static Texture2D GetTexture2D(this Sprite sprite)
        {
            if (sprite == null)
                return null;
            return sprite.texture;
        }


        public static string NiceName(this Object o)
        {
            if (!o)
            {
                return "<null>";
            }

            string text = o.name;
            int num = text.IndexOf('(');
            if (num >= 0)
            {
                text = text.Substring(0, num);
            }

            return $"<{o.GetType().Name}> '{text}' [{o.GetInstanceID()}]";
        }

        public static string PathToString(this Transform t)
        {
            if (!t)
            {
                return "<null>";
            }

            List<string> list = new List<string>();
            try
            {
                while ((bool)t)
                {
                    list.Add($"{t.name}[{t.GetInstanceID()}]");
                    t = t.parent;
                }
            }
            catch (UnityException)
            {
            }

            list.Reverse();
            return string.Join("/", list);
        }

        public static IEnumerable<Transform> GetChildren(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                yield return transform.GetChild(i);
            }
        }

        public static GameObject GetGameObjectOf(Collider collider)
        {
            if ((bool)collider.attachedRigidbody)
            {
                return collider.attachedRigidbody.gameObject;
            }

            return collider.gameObject;
        }

        public static void RequireActive(this MonoBehaviour c, Transform rootTransform)
        {
            if (c.isActiveAndEnabled)
            {
                return;
            }

            if (!c.enabled)
            {
                Logger.Error($"{c} has been disabled. Re-enabling");
                c.enabled = true;
            }

            if (c.isActiveAndEnabled)
            {
                return;
            }

            Transform transform = c.transform;
            while ((bool)transform && transform != rootTransform)
            {
                if (!transform.gameObject.activeSelf)
                {
                    Logger.Error($"{transform.gameObject} has been deactivate. Re-activating");
                    transform.gameObject.SetActive(value: false);
                    if (c.isActiveAndEnabled)
                    {
                        return;
                    }
                }

                transform = transform.parent;
            }

            if (!rootTransform.gameObject.activeSelf)
            {
                Logger.Error($"{rootTransform.gameObject} has been deactivate. Re-activating");
                rootTransform.gameObject.SetActive(value: false);
            }
        }
    }
}
