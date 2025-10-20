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


        internal static string IntlTranslate(string? key)
        {
            if (Language.isNotQuitting)
                return Language.main.Get(key);
            return key??"";
        }

        internal static string IntlTranslate<T>(string key, T a0)
        {
            if (Language.isNotQuitting)
                return Language.main.GetFormat(key, a0);
            return key;
        }


        internal static string IntlTranslate<T0, T1>(string key, T0 a0, T1 a1)
        {
            if (Language.isNotQuitting)
                return Language.main.GetFormat(key, a0, a1);
            return key;
        }


        internal static string IntlTranslate<T0, T1, T2>(string key, T0 a0, T1 a1, T2 a2)
        {
            if (Language.isNotQuitting)
                return Language.main.GetFormat(key, a0, a1, a2);
            return key;
        }


        internal static string IntlTranslate<T0, T1, T2, T3>(string key, T0 a0, T1 a1, T2 a2, T3 a3)
        {
            if (Language.isNotQuitting)
                return Language.main.GetFormat(key, a0, a1, a2, a3);
            return key;
        }

        /// <inheritdoc/>
        public string Translate(TranslationKey key)
            => IntlTranslate(GetTranslationKey(key));

        /// <inheritdoc/>
        public string Translate<T>(TranslationKey key, T a0)
            => IntlTranslate(GetTranslationKey(key), a0);

        /// <inheritdoc/>
        public string Translate<T0, T1>(TranslationKey key, T0 a0, T1 a1)
            => IntlTranslate(GetTranslationKey(key), a0, a1);

        /// <inheritdoc/>
        public string Translate<T0, T1, T2>(TranslationKey key, T0 a0, T1 a1, T2 a2)
            => IntlTranslate(GetTranslationKey(key), a0, a1, a2);

        /// <inheritdoc/>
        public string Translate<T0, T1, T2, T3>(TranslationKey key, T0 a0, T1 a1, T2 a2, T3 a3)
            => IntlTranslate(GetTranslationKey(key), a0, a1, a2, a3);
    }
}