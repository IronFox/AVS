namespace AVS.Localization
{

    /// <summary>
    /// Translator interface for AVS.
    /// </summary>
    public interface ITranslator
    {
        /// <summary>
        /// Translates a simple translation key.
        /// </summary>
        string Translate(TranslationKey key);
        /// <summary>
        /// Translates a translation key with arguments.
        /// </summary>
        string Translate<T>(TranslationKey key, T a0);
        /// <summary>
        /// Translates a translation key with arguments.
        /// </summary>
        string Translate<T0, T1>(TranslationKey key, T0 a0, T1 a1);
        /// <summary>
        /// Translates a translation key with arguments.
        /// </summary>
        string Translate<T0, T1, T2>(TranslationKey key, T0 a0, T1 a1, T2 a2);
        /// <summary>
        /// Translates a translation key with arguments.
        /// </summary>
        string Translate<T0, T1, T2, T3>(TranslationKey key, T0 a0, T1 a1, T2 a2, T3 a3);
    }
}
