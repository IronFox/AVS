using UnityEngine;

namespace AVS.Util
{
    public static class GameObjectHelper
    {
        public static Transform GetTransform(this GameObject gameObject)
        {
            if (gameObject == null)
                return null;
            return gameObject.transform;
        }
    }
}
