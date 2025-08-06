namespace AVS.Localization
{
    /// <summary>
    /// All AVS translation keys.
    /// </summary>
    public enum TranslationKey
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// The localized default name of the vehicle created by AVS.
        /// </summary>
        VehicleDefaultName,

        /// <summary>
        /// Produces the current charge status.
        /// The first argument is the current charge fraction.
        /// The second is the absolute charge value.
        /// The third is the maximum charge value.
        /// </summary>
        Reactor_BatteryCharge,

        Fabricator_Node_Root,
        Fabricator_Node_General,
        Fabricator_Node_DepthModules,

        Fabricator_DisplayName,
        Fabricator_Description,

        ColorPicker_Tab_Exterior,
        ColorPicker_Tab_PrimaryAccent,
        ColorPicker_Tab_SecondaryAccent,
        ColorPicker_Tab_Name,

        /// <summary>
        /// Headline when listing the reactor's whitelist
        /// with power potentials.
        /// </summary>
        Reactor_WhitelistWithPowerValue,
        /// <summary>
        /// Headline when listing the reactor's whitelist
        /// without power potentials.
        /// </summary>
        Reactor_WhitelistPlain,

        /// <summary>
        /// No vehicle was found to undock.
        /// </summary>
        Command_NothingToUndock,
        /// <summary>
        /// The undock console command did undock something.
        /// </summary>
        Command_UndockingVehicle,

        HandHover_ControlPanel_Empty,
        HandHover_ControlPanel_HeadLights,
        HandHover_ControlPanel_FloodLights,
        HandHover_ControlPanel_NavLights,
        HandHover_ControlPanel_InteriorLights,
        HandHover_ControlPanel_DefaultColor,
        HandHover_ControlPanel_Power,
        HandHover_ControlPanel_Autopilot,

        /// <summary>
        /// Text displayed when the player hovers over a battery slot.
        /// </summary>
        HandOver_BatterySlot,
        HandOver_AutoPilotBatterySlot,

        /// <summary>
        /// Text displayed when the player hovers over a storage component.
        /// The first argument is the given display name of the storage component (which may also be localized).
        /// </summary>
        HandHover_OpenStorage,

        HandHover_Vehicle_Enter,
        HandHover_Vehicle_Exit,


        /// <summary>
        /// Displayed over a vehicle's helm control
        /// </summary>
        HandHover_Vehicle_StartHelmControl,

        /// <summary>
        /// The string to display over a material reactor.
        /// Expected to produce a string like "*: 500/1000".
        /// The first argument is the absolute current charge,
        /// the second is the maximum charge.
        /// </summary>
        HandHover_Reactor_Charge,
        /// <summary>
        /// The translation key that hints that the user can show the reactor's item-whitelist by right-clicking.
        /// </summary>
        HandHoverSub_Reactor_ShowWhitelist,

        /// <summary>
        /// The vehicle status when the vehicle is scuttled, showing the percentage of its deconstruction.
        /// </summary>
        HandHover_Vehicle_DesconstructionPercent,

        /// <summary>
        /// The vehicle status when hovering while docked and the vehicle is fully charged.
        /// The first argument is the vehicle health fraction.
        /// </summary>
        HandHover_Docked_StatusCharged,
        /// <summary>
        /// Represents the status of a hand hover event when the device is docked and charging.
        /// The first argument is the vehicle health fraction.
        /// The second argument is the current charge level fraction.
        /// </summary>
        HandHover_Docked_StatusCharging,

        Module_Depth1_DisplayName,
        Module_Depth2_DisplayName,
        Module_Depth3_DisplayName,
        Module_Depth1_Description,
        Module_Depth2_Description,
        Module_Depth3_Description,

        /// <summary>
        /// An item was added to the storage.
        /// </summary>
        Report_AddedToStorage,

        /// <summary>
        /// When trying to remove an upgrade that is not removable because its storage is not empty.
        /// </summary>
        Error_UpgradeNotRemovable_StorageNotEmpty,
        /// <summary>
        /// When the user tries to add an upgrade that is not compatible with the vehicle.
        /// </summary>
        Error_UpgradeNotAddable_Incompatible,
        /// <summary>
        /// Then trying to add an item to a storage that is full.
        /// </summary>
        Error_CannotAdd_StorageFull,
        /// <summary>
        /// Exiting the vehicle is not allowed at the moment.
        /// </summary>
        Error_CannotExitVehicle,

        /// <summary>
        /// Error message shown when the player tries to remove materials from a reactor.
        /// The first argument is the reactor's interactText field.
        /// </summary>
        Error_CannotRemoveMaterialsFromReactor,

        /// <summary>
        /// If the player tries to add a dead fish to the mobile water park.
        /// The first parameter is the tech type name of the fish.
        /// </summary>
        Error_MobileWaterPark_CannotAdd_FishIsDead,

        /// <summary>
        /// If the player tries to add a hatched egg to the mobile water park.
        /// The first parameter is the tech type name of the egg.
        /// </summary>
        Error_MobileWaterPark_CannotAdd_EggIsHatched,
        /// <summary>
        /// If the player tries to add something that is not a fish or an egg to the mobile water park.
        /// The first parameter is the tech type name of the item.
        /// </summary>
        Error_MobileWaterPark_CannotAdd_Incompatible,
        /// <summary>
        /// If the player tries to remove an egg from the mobile water park that is in the process of hatching.
        /// </summary>
        Error_MobileWaterPark_CannotRemove_HatchingEgg,


#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Global localization utility for AVS.
    /// </summary>
    public static class Translator
    {
        /// <summary>
        /// The replaceable implementation for translating keys.
        /// </summary>
        public static ITranslator Implementation { get; set; } = new DefaultTranslator();


        internal static string Get(TranslationKey key)
            => Implementation.Translate(key);
        internal static string GetFormatted<T>(TranslationKey key, T a0)
            => Implementation.Translate(key, a0);
        internal static string GetFormatted<T0, T1>(TranslationKey key, T0 a0, T1 a1)
            => Implementation.Translate(key, a0, a1);
        internal static string GetFormatted<T0, T1, T2>(TranslationKey key, T0 a0, T1 a1, T2 a2)
            => Implementation.Translate(key, a0, a1, a2);
        internal static string GetFormatted<T0, T1, T2, T3>(TranslationKey key, T0 a0, T1 a1, T2 a2, T3 a3)
            => Implementation.Translate(key, a0, a1, a2, a3);
    }

}


