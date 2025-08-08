namespace AVS.Localization
{

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


