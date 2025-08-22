using AVS.BaseVehicle;
using AVS.Util;
using TMPro;
using UnityEngine;

namespace AVS.StorageComponents;

internal class uGUI_VehicleHUD : MonoBehaviour
{
    public enum HUDChoice
    {
        Normal,
        Storage
    }

    public HUDChoice HUDType = HUDChoice.Normal;
    private bool IsStorageHUD() => textStorage.IsNotNull();

    private bool HasMvStorage(AvsVehicle mv) => mv.Com.InnateStorages.IsNotNull() ||
                                                ModularStorageInput.GetAllModularStorageContainers(mv).Count > 0;

    private void DeactivateAll()
    {
        root!.SetActive(false);
    }

    private bool ShouldIDie(AvsVehicle? mv, PDA? pda)
    {
        if (mv.IsNull() || pda.IsNull())
            // show nothing if we're not in an MV
            // or if PDA isn't available
            return true;

        if (IsStorageHUD())
        {
            if (HasMvStorage(mv))
                switch (HUDType)
                {
                    case HUDChoice.Normal:
                        // I'm the storage HUD, and I can be displayed, but the user wants the normal HUD. I should die.
                        return true;
                    case HUDChoice.Storage:
                        // I'm the storage HUD, and I can be displayed, and the user wants me. I should live.
                        return false;
                }
            else
                // I'm the storage HUD, but I can't be displayed. I should die.
                return true;
        }
        else
        {
            switch (HUDType)
            {
                case HUDChoice.Normal:
                    // I'm the normal HUD, and the user wants me. I should live
                    return false;
                case HUDChoice.Storage:
                    // I'm the normal HUD, but the user wants storage. I should die if it is available.
                    return HasMvStorage(mv);
            }
        }

        return true;
    }

    public void Update()
    {
        if (Player.main.IsNull())
        {
            DeactivateAll();
            return;
        }

        var mv = Player.main.GetAvsVehicle();
        var pda = Player.main.GetPDA();
        if (ShouldIDie(mv, pda))
        {
            DeactivateAll();
            return;
        }

        root!.transform.localPosition = Vector3.zero;

        var mvflag = !pda.isInUse;
        if (root.activeSelf != mvflag)
            root.SetActive(mvflag);
        if (mvflag)
        {
            UpdateHealth();
            UpdatePower();
            UpdateTemperature();
            UpdateStorage();
        }
    }

    public void UpdateHealth()
    {
        var mv = Player.main.GetAvsVehicle();
        if (mv.IsNotNull())
        {
            mv.GetHUDValues(out var num, out var num2);
            var num3 = Mathf.CeilToInt(num * 100f);
            if (lastHealth != num3)
            {
                lastHealth = num3;
                textHealth!.text = IntStringCache.GetStringForInt(lastHealth);
            }
        }
    }

    public void UpdateTemperature()
    {
        var mv = Player.main.GetAvsVehicle();
        if (mv.IsNotNull())
        {
            var temperature = mv.GetTemperature();
            temperatureSmoothValue = temperatureSmoothValue < -10000f
                ? temperature
                : Mathf.SmoothDamp(temperatureSmoothValue, temperature, ref temperatureVelocity, 1f);
            int tempNum;
            if (mv.Config.HudTemperatureIsFahrenheit)
                tempNum = Mathf.CeilToInt(temperatureSmoothValue * 1.8f + 32);
            else
                tempNum = Mathf.CeilToInt(temperatureSmoothValue);
            if (lastTemperature != tempNum)
            {
                lastTemperature = tempNum;
                textTemperature!.text = IntStringCache.GetStringForInt(lastTemperature);
                textTemperatureSuffix!.color = new Color32(byte.MaxValue, 220, 0, byte.MaxValue);
                if (mv.Config.HudTemperatureIsFahrenheit)
                    textTemperatureSuffix.text = "°F";
                else
                    textTemperatureSuffix.text = "°C";
            }
        }
    }

    public void UpdatePower()
    {
        var mv = Player.main.GetAvsVehicle();
        if (mv.IsNotNull())
        {
            mv.GetHUDValues(out var num, out var num2);
            var num4 = Mathf.CeilToInt(num2 * 100f);
            if (lastPower != num4)
            {
                lastPower = num4;
                textPower!.text = IntStringCache.GetStringForInt(lastPower);
            }
        }
    }

    public void UpdateStorage()
    {
        if (textStorage.IsNull())
            return;
        var mv = Player.main.GetAvsVehicle();
        if (mv.IsNotNull())
        {
            mv.GetStorageValues(out var stored, out var capacity);
            if (capacity > 0)
            {
                var ratio = 100 * stored / capacity;
                textStorage.text = ratio.ToString();
            }
            else
            {
                textStorage.text = 100.ToString();
            }
        }
    }

    public const float temperatureSmoothTime = 1f;
    [AssertNotNull] public GameObject? root;
    [AssertNotNull] public TextMeshProUGUI? textHealth;
    [AssertNotNull] public TextMeshProUGUI? textPower;
    [AssertNotNull] public TextMeshProUGUI? textTemperature;
    [AssertNotNull] public TextMeshProUGUI? textTemperatureSuffix;
    [AssertNotNull] public TextMeshProUGUI? textStorage;
    public int lastHealth = int.MinValue;
    public int lastPower = int.MinValue;
    public int lastTemperature = int.MinValue;
    public float temperatureSmoothValue = float.MinValue;
    public float temperatureVelocity;
    [AssertLocalization] public const string thermometerFormatKey = "ThermometerFormat";
}