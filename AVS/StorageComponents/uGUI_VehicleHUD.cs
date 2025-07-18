using AVS.BaseVehicle;
using TMPro;
using UnityEngine;


namespace AVS
{
    public class uGUI_VehicleHUD : MonoBehaviour
    {
        public enum HUDChoice
        {
            Normal,
            Storage
        }

        public HUDChoice HUDType = HUDChoice.Normal;
        private bool IsStorageHUD()
        {
            return textStorage != null;
        }
        private bool HasMvStorage(AvsVehicle mv)
        {
            return mv.Com.InnateStorages != null || ModularStorageInput.GetAllModularStorageContainers(mv).Count > 0;
        }

        private void DeactivateAll()
        {
            root!.SetActive(false);
        }
        private bool ShouldIDie(AvsVehicle? mv, PDA? pda)
        {
            if (mv == null || pda == null)
            {
                // show nothing if we're not in an MV
                // or if PDA isn't available
                return true;
            }

            if (IsStorageHUD())
            {
                if (HasMvStorage(mv))
                {
                    switch (HUDType)
                    {
                        case HUDChoice.Normal:
                            // I'm the storage HUD, and I can be displayed, but the user wants the normal HUD. I should die.
                            return true;
                        case HUDChoice.Storage:
                            // I'm the storage HUD, and I can be displayed, and the user wants me. I should live.
                            return false;
                    }
                }
                else
                {
                    // I'm the storage HUD, but I can't be displayed. I should die.
                    return true;
                }
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
            if (Player.main == null)
            {
                DeactivateAll();
                return;
            }
            var mv = Player.main.GetModVehicle();
            PDA pda = Player.main.GetPDA();
            if (ShouldIDie(mv, pda))
            {
                DeactivateAll();
                return;
            }

            root!.transform.localPosition = Vector3.zero;

            bool mvflag = !pda.isInUse;
            if (root.activeSelf != mvflag)
            {
                root.SetActive(mvflag);
            }
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
            var mv = Player.main.GetModVehicle();
            if (mv != null)
            {
                mv.GetHUDValues(out float num, out float num2);
                int num3 = Mathf.CeilToInt(num * 100f);
                if (lastHealth != num3)
                {
                    lastHealth = num3;
                    textHealth!.text = IntStringCache.GetStringForInt(lastHealth);
                }
            }
        }
        public void UpdateTemperature()
        {
            var mv = Player.main.GetModVehicle();
            if (mv != null)
            {
                float temperature = mv.GetTemperature();
                temperatureSmoothValue = ((temperatureSmoothValue < -10000f) ? temperature : Mathf.SmoothDamp(temperatureSmoothValue, temperature, ref temperatureVelocity, 1f));
                int tempNum;
                //if (MainPatcher.NautilusConfig.IsFahrenheit)
                //{
                //    tempNum = Mathf.CeilToInt(temperatureSmoothValue * 1.8f + 32);
                //}
                //else
                {
                    tempNum = Mathf.CeilToInt(temperatureSmoothValue);
                }
                if (lastTemperature != tempNum)
                {
                    lastTemperature = tempNum;
                    textTemperature!.text = IntStringCache.GetStringForInt(lastTemperature);
                    textTemperatureSuffix!.color = new Color32(byte.MaxValue, 220, 0, byte.MaxValue);
                    //if (MainPatcher.NautilusConfig.IsFahrenheit)
                    //{
                    //    textTemperatureSuffix.text = "°F";
                    //}
                    //else
                    {
                        textTemperatureSuffix.text = Language.main.GetFormat("ThermometerFormat");
                    }
                }
            }
        }
        public void UpdatePower()
        {
            var mv = Player.main.GetModVehicle();
            if (mv != null)
            {
                mv.GetHUDValues(out float num, out float num2);
                int num4 = Mathf.CeilToInt(num2 * 100f);
                if (lastPower != num4)
                {
                    lastPower = num4;
                    textPower!.text = IntStringCache.GetStringForInt(lastPower);
                }
            }
        }

        public void UpdateStorage()
        {
            if (textStorage == null)
            {
                return;
            }
            var mv = Player.main.GetModVehicle();
            if (mv != null)
            {
                mv.GetStorageValues(out int stored, out int capacity);
                if (capacity > 0)
                {
                    int ratio = (100 * stored) / capacity;
                    textStorage.text = ratio.ToString();
                }
                else
                {
                    textStorage.text = 100.ToString();
                }
            }
        }
        public const float temperatureSmoothTime = 1f;
        [AssertNotNull]
        public GameObject? root;
        [AssertNotNull]
        public TextMeshProUGUI? textHealth;
        [AssertNotNull]
        public TextMeshProUGUI? textPower;
        [AssertNotNull]
        public TextMeshProUGUI? textTemperature;
        [AssertNotNull]
        public TextMeshProUGUI? textTemperatureSuffix;
        [AssertNotNull]
        public TextMeshProUGUI? textStorage;
        public int lastHealth = int.MinValue;
        public int lastPower = int.MinValue;
        public int lastTemperature = int.MinValue;
        public float temperatureSmoothValue = float.MinValue;
        public float temperatureVelocity;
        [AssertLocalization]
        public const string thermometerFormatKey = "ThermometerFormat";
    }
}