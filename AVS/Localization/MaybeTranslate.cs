using System;

namespace AVS.Localization
{
    /// <summary>
    /// Helpers for creating <see cref="MaybeTranslate"/> instances.
    /// </summary>
    public static class Text
    {
        /// <summary>
        /// Creates a new instance of <see cref="MaybeTranslate"/> with the specified text.
        /// </summary>
        /// <param name="text">The text to be used for translation.</param>
        /// <returns>A <see cref="MaybeTranslate"/> object initialized with the provided text.</returns>
        public static MaybeTranslate Translated(string text) => new MaybeTranslate(text, true);
        /// <summary>
        /// Creates a <see cref="MaybeTranslate"/> instance representing untranslated text.
        /// </summary>
        /// <param name="text">The text to be marked as untranslated. Cannot be null.</param>
        /// <returns>A <see cref="MaybeTranslate"/> object with the specified text marked as untranslated.</returns>
        public static MaybeTranslate Untranslated(string text) => new MaybeTranslate(text, false);
    }


    /// <summary>
    /// Text that may or may not be translated.
    /// </summary>
    public readonly struct MaybeTranslate : IEquatable<MaybeTranslate>, IEquatable<string>
    {
        /// <summary>
        /// The text to display or use as key for localization.
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// True if the text should be localized, false if it should be used as is.
        /// </summary>
        public bool Localize { get; }
        /// <summary>
        /// Gets the localized or original text based on the localization setting.
        /// </summary>
        public string Rendered => string.IsNullOrEmpty(Text)
            ? "<none>"
            : Localize ? DefaultTranslator.IntlTranslate(Text) : Text;

        /// <summary>
        /// Creates a new instance of MaybeTranslate.
        /// </summary>
        /// <param name="text">The text to display or use as key for localization</param>
        /// <param name="localize">True if the text should be localized, false if it should be used as is</param>
        internal MaybeTranslate(string text, bool localize)
        {
            Text = text;
            Localize = localize;
        }

        /// <inheritdoc/>
        public override string ToString() => Rendered;

        /// <inheritdoc/>
        public bool Equals(MaybeTranslate other)
            => string.Equals(Text, other.Text, StringComparison.Ordinal) && Localize == other.Localize;

        /// <inheritdoc/>
        public bool Equals(string other)
            => string.Equals(Text, other, StringComparison.Ordinal)
            || string.Equals(Rendered, other, StringComparison.Ordinal);


        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is MaybeTranslate other && Equals(other) || obj is string str && Equals(str);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (Text?.GetHashCode() ?? 0);
            hash = hash * 31 + Localize.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public static bool operator ==(MaybeTranslate left, MaybeTranslate right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(MaybeTranslate left, MaybeTranslate right)
            => !left.Equals(right);
    }
}
