using AVS.Assets;
using AVS.BaseVehicle;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS;

//SetupBuildBotPaths is invoked by Player.Start
internal static class BuildBotManager
{
    private static Color OriginalConstructingGhostColor =
        ((Material)Resources.Load("Materials/constructingGhost")).color;

    public static void SetupBuildBotBeamPoints(GameObject mv)
    {
        var bbbp = mv.EnsureComponent<BuildBotBeamPoints>();
        var bbbpList = new List<Transform>();
        var modVehicle = mv.GetComponent<AvsVehicle>();
        if (modVehicle != null)
        {
            if (modVehicle.collisionModel != null)
            {
                var go = modVehicle.collisionModel;
                foreach (var child in go.transform.GetComponentsInChildren<Transform>())
                    bbbpList.Add(child);
            }
        }
        else
        {
            foreach (var child in mv.GetComponentsInChildren<Transform>()) bbbpList.Add(child);
        }

        bbbp.beamPoints = bbbpList.ToArray();
    }

    public static IEnumerator SetupVFXConstructing(GameObject go)
    {
        yield return SeamothHelper.Coroutine;
        var seamoth = SeamothHelper.RequireSeamoth;
        var seamothVFXC = seamoth.GetComponent<VFXConstructing>();
        var rocketPlatformVfx = seamoth.GetComponentInChildren<VFXConstructing>();
        var vfxc = go.EnsureComponent<VFXConstructing>();

        var mv = go.GetComponent<AvsVehicle>();
        vfxc.timeToConstruct = mv == null ? 10f : mv.Config.TimeToConstruct;

        vfxc.alphaTexture = seamothVFXC.alphaTexture;
        vfxc.alphaDetailTexture = seamothVFXC.alphaDetailTexture;
        vfxc.wireColor = seamothVFXC.wireColor;
        vfxc.rBody = go.GetComponent<Rigidbody>();

        // we'll take the seamoth sound bc the other sound is goofy
        // we don't really like this splash, but at least the size is pretty good
        vfxc.surfaceSplashFX = rocketPlatformVfx.surfaceSplashFX;
        vfxc.surfaceSplashSound = seamothVFXC.surfaceSplashSound;
        vfxc.surfaceSplashVelocity = rocketPlatformVfx.surfaceSplashVelocity;

        vfxc.heightOffset = seamothVFXC.heightOffset;
        vfxc.constructSound = seamothVFXC.constructSound;
        vfxc.delay = 5f; // the time it takes for the buildbots to fly out and begin
        vfxc.isDone = false;
        vfxc.informGameObject = null;
        vfxc.transparentShaders = null; // TODO maybe we'll want to use this?
        vfxc.Regenerate();

        yield break;
    }

    public static void BuildPathsUsingCorners(GameObject root, GameObject pointsRoot, Transform A, Transform B,
        Transform C, Transform D, Transform E, Transform F, Transform G, Transform H)
    {
        var bbPathsRoot = new GameObject("BuildBotPaths");
        bbPathsRoot.transform.SetParent(root.transform);

        #region declarations

        var I = GetCentroid(pointsRoot, A, B, C, D);
        var J = GetCentroid(pointsRoot, E, F, G, H);
        var L = GetCentroid(pointsRoot, A, B, E, F);
        var K = GetCentroid(pointsRoot, C, D, G, H);
        var M = GetCentroid(pointsRoot, A, C, E, G);
        var N = GetCentroid(pointsRoot, B, D, F, H);
        var AB = GetMidpoint(pointsRoot, A, B);
        var CD = GetMidpoint(pointsRoot, C, D);
        var EF = GetMidpoint(pointsRoot, E, F);
        var GH = GetMidpoint(pointsRoot, G, H);
        var BD = GetMidpoint(pointsRoot, B, D);
        var FH = GetMidpoint(pointsRoot, F, H);
        var EG = GetMidpoint(pointsRoot, E, G);
        var AC = GetMidpoint(pointsRoot, A, C);
        var BF = GetMidpoint(pointsRoot, B, F);
        var AE = GetMidpoint(pointsRoot, A, E);
        var CG = GetMidpoint(pointsRoot, C, G);
        var DH = GetMidpoint(pointsRoot, D, H);
        var path1List = new List<Transform> { K, GH, J, EF, L, AB, I, CD };
        var path2List = new List<Transform> { I, BD, N, FH, J, EG, M, AC };
        var path3List = new List<Transform> { N, BF, L, AE, M, CG, K, DH };
        var path4List = new List<Transform> { J, G, C, I, B, F };

        #endregion

        void BuildBuildBotPath(GameObject rootGO, int number, List<Transform> pathList)
        {
            var bbPath = new GameObject("Path" + number.ToString());
            bbPath.transform.SetParent(rootGO.transform);
            var path = bbPath.AddComponent<BuildBotPath>();
            path.points = pathList.ToArray();
        }

        BuildBuildBotPath(bbPathsRoot, 1, path1List);
        BuildBuildBotPath(bbPathsRoot, 2, path2List);
        BuildBuildBotPath(bbPathsRoot, 3, path3List);
        BuildBuildBotPath(bbPathsRoot, 4, path4List);
    }

    public static Transform GetMidpoint(GameObject root, Transform left, Transform right)
    {
        var pointGO = new GameObject(left.name + right.name);
        var pointTR = pointGO.transform;
        pointTR.SetParent(root.transform);
        pointTR.localPosition = (left.position + right.position) / 2;
        return pointTR;
    }

    public static Transform GetCentroid(GameObject root, Transform topleft, Transform topright, Transform botleft,
        Transform botright)
    {
        var pointGO = new GameObject(topleft.name + topright.name + botleft.name + botright.name);
        var pointTR = pointGO.transform;
        pointTR.SetParent(root.transform);
        pointTR.localPosition = (topleft.position + topright.position + botleft.position + botright.position) / 4;
        return pointTR;
    }

    public enum CornerValue
    {
        lefttopfront,
        righttopfront,
        lefttopback,
        righttopback,
        leftbotfront,
        rightbotfront,
        leftbotback,
        rightbotback
    }

    public static (bool, bool, bool) CornerToBools(CornerValue corner)
    {
        switch (corner)
        {
            case CornerValue.lefttopfront:
                return (false, true, true);
            case CornerValue.righttopfront:
                return (true, true, true);
            case CornerValue.lefttopback:
                return (false, true, false);
            case CornerValue.righttopback:
                return (true, true, false);
            case CornerValue.leftbotfront:
                return (false, false, true);
            case CornerValue.rightbotfront:
                return (true, false, true);
            case CornerValue.leftbotback:
                return (false, false, false);
            case CornerValue.rightbotback:
                return (true, false, false);
            default:
                throw new ArgumentOutOfRangeException(nameof(corner), corner, null);
        }
    }

    public static Transform GetCorner(GameObject root, CornerValue corner, Vector3 center, float x, float y, float z)
    {
        Vector3 GetThisCorner((bool x, bool y, bool z) input)
        {
            var ret = center;
            ret += input.x ? x * Vector3.right : -1 * x * Vector3.right;
            ret += input.y ? y * Vector3.up : -1 * y * Vector3.up;
            ret += input.z ? z * Vector3.forward : -1 * z * Vector3.forward;
            return ret;
        }

        var pointGO = new GameObject(corner.ToString());
        var pointTR = pointGO.transform;
        pointTR.SetParent(root.transform);
        pointTR.localPosition = GetThisCorner(CornerToBools(corner));
        return pointTR;
    }

    public static Transform GetCornerCube(GameObject root, Vector3 cubeDimensions, Vector3 center, CornerValue corner)
    {
        var x = cubeDimensions.x / 2f;
        var y = cubeDimensions.y / 2f;
        var z = cubeDimensions.z / 2f;
        return GetCorner(root, corner, center, x, y, z);
    }

    public static Transform GetCornerBoxCollider(GameObject root, BoxCollider box, CornerValue corner)
    {
        var worldScale = box.transform.lossyScale;
        var boxSizeScaled = Vector3.Scale(box.size, worldScale);
        boxSizeScaled *= 1.1f;
        var x = boxSizeScaled.x / 2f;
        var y = boxSizeScaled.y / 2f;
        var z = boxSizeScaled.z / 2f;
        var boxCenterScaled = Vector3.Scale(box.center, worldScale);
        return GetCorner(root, corner, boxCenterScaled, x, y, z);
    }

    public static void BuildPathsForGameObject(GameObject go, GameObject pointsRoot)
    {
        var localCenter = go.transform.localPosition;
        var localSize = new Vector3(6f, 8f, 12f);
        var A = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.lefttopfront);
        var B = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.righttopfront);
        var C = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.lefttopback);
        var D = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.righttopback);
        var E = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.leftbotfront);
        var F = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.rightbotfront);
        var G = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.leftbotback);
        var H = GetCornerCube(pointsRoot, localSize, localCenter, CornerValue.rightbotback);
        BuildPathsUsingCorners(go, pointsRoot, A, B, C, D, E, F, G, H);
    }

    public static void BuildPathsForAvsVehicle(AvsVehicle mv, GameObject pointsRoot)
    {
        var box = mv.Com.BoundingBoxCollider;
        if (box != null)
        {
            var A = GetCornerBoxCollider(pointsRoot, box, CornerValue.lefttopfront);
            var B = GetCornerBoxCollider(pointsRoot, box, CornerValue.righttopfront);
            var C = GetCornerBoxCollider(pointsRoot, box, CornerValue.lefttopback);
            var D = GetCornerBoxCollider(pointsRoot, box, CornerValue.righttopback);
            var E = GetCornerBoxCollider(pointsRoot, box, CornerValue.leftbotfront);
            var F = GetCornerBoxCollider(pointsRoot, box, CornerValue.rightbotfront);
            var G = GetCornerBoxCollider(pointsRoot, box, CornerValue.leftbotback);
            var H = GetCornerBoxCollider(pointsRoot, box, CornerValue.rightbotback);
            BuildPathsUsingCorners(mv.gameObject, pointsRoot, A, B, C, D, E, F, G, H);
        }
        else
        {
            BuildPathsForGameObject(mv.gameObject, pointsRoot);
        }
    }

    public static void BuildBotPathsHelper(GameObject go)
    {
        var mv = go.GetComponent<AvsVehicle>();
        var bbPointsRoot = new GameObject("BuildBotPoints");
        bbPointsRoot.transform.SetParent(go.transform);
        if (mv != null && mv.Com.BoundingBoxCollider != null)
        {
            bbPointsRoot.transform.localPosition =
                go.transform.InverseTransformPoint(mv.Com.BoundingBoxCollider.transform.position);
            BuildPathsForAvsVehicle(mv, bbPointsRoot);
        }
        else
        {
            bbPointsRoot.transform.localPosition = Vector3.zero;
            BuildPathsForGameObject(go, bbPointsRoot);
        }
    }

    public static IEnumerator SetupBuildBotPaths(GameObject go)
    {
        SetupBuildBotBeamPoints(go);
        yield return SetupVFXConstructing(go);
        BuildBotPathsHelper(go);
    }

    public static IEnumerator SetupBuildBotPathsForAllMVs()
    {
        foreach (var mv in AvsVehicleBuilder.prefabs)
            if (mv.GetComponentInChildren<BuildBotPath>(true) == null)
                yield return SetupBuildBotPaths(mv.gameObject);
    }

    public static void ResetGhostMaterial()
    {
        var ghostMat = (Material)Resources.Load("Materials/constructingGhost");
        ghostMat.color = OriginalConstructingGhostColor;
    }
}