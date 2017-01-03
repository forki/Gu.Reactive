// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements must be documented
namespace Gu.Wpf.Reactive
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Base class for boolean to X converter
    /// </summary>
    /// <typeparam name="T">The type of the value to convert to.</typeparam>
    [Obsolete("To be removed.")]
    public abstract class BooleanToXConverter<T> : MarkupConverter<bool?, T>
    {
        /// <summary>
        /// Gets or sets the value to be returned when the converted value is true
        /// </summary>
        public T WhenTrue { get; set; }

        /// <summary>
        /// Gets or sets the value to be returned when the converted value is false
        /// </summary>
        public T WhenFalse { get; set; }

        /// <summary>
        /// Gets or sets the value to be returned when the converted value is null
        /// </summary>
        public T WhenNull { get; set; }

        protected override T Convert(bool? value, CultureInfo culture)
        {
            if (value == null)
            {
                return this.WhenNull;
            }

            return value == true ? this.WhenTrue : this.WhenFalse;
        }

        protected override bool? ConvertBack(T value, CultureInfo culture)
        {
            if (Equals(value, this.WhenTrue))
            {
                return true;
            }

            if (Equals(value, this.WhenFalse))
            {
                return false;
            }

            return null;
        }
    }
}