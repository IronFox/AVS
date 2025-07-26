namespace AVS.Localization
{
    /// <summary>
    /// Default implementation of the ITranslator interface for AVS.
    /// </summary>
    public class DefaultTranslator : ITranslator
    {
        /// <summary>
        /// Retrieves the translation key for a given TranslationKey enum value.
        /// </summary>
        public virtual string GetTranslationKey(TranslationKey key)
            => "AVS." + key;
        /// <inheritdoc/>
        public string Translate(TranslationKey key)
            => Language.main.Get(GetTranslationKey(key));

        /// <inheritdoc/>
        public string Translate<T>(TranslationKey key, T a0)
            => Language.main.GetFormat(GetTranslationKey(key), a0);

        /// <inheritdoc/>
        public string Translate<T0, T1>(TranslationKey key, T0 a0, T1 a1)
            => Language.main.GetFormat(GetTranslationKey(key), a0, a1);

        /// <inheritdoc/>
        public string Translate<T0, T1, T2>(TranslationKey key, T0 a0, T1 a1, T2 a2)
            => Language.main.GetFormat(GetTranslationKey(key), a0, a1, a2);

        /// <inheritdoc/>
        public string Translate<T0, T1, T2, T3>(TranslationKey key, T0 a0, T1 a1, T2 a2, T3 a3)
            => Language.main.GetFormat(GetTranslationKey(key), a0, a1, a2, a3);
    }
}