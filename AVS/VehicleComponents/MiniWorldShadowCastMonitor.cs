using System.Collections.Generic;
using UnityEngine;

namespace AVS.VehicleComponents
{
    internal class MiniWorldShadowCastMonitor : MonoBehaviour
    {
        private readonly List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
        public void Update()
        {
            meshRenderers.Clear();
            transform.GetComponentsInChildren<MeshRenderer>(false, meshRenderers);
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;
            }
        }
    }
}
