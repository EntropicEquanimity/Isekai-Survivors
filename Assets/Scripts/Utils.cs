using System;
using UnityEngine;

namespace BlondieUtils
{
    public static class Utils
    {
        /// <summary>
        /// Formats a float into hours. 1 = 1 second. Returns a string. 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string FormatTimeToHours(float time)
        {
            int hours = Mathf.RoundToInt(time / 3600f);
            int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
        /// <summary>
        /// Formats a float into minutes. Does not include hours. 1 = 1 second. Returns a string. 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string FormatTimeToMinutes(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Converts a number to a shortened string format (k for thousands, m for millions)
        /// </summary>
        /// <param name="value">The number to convert</param>
        /// <param name="minDigitsBeforeShorten">Minimum digits required before shortening (default: 6 = 100,000)</param>
        /// <param name="decimalPlaces">Number of decimal places to show when shortened (default: 1)</param>
        /// <returns>Formatted string representation of the number</returns>
        public static string FormatNumberShort(float value, int minDigitsBeforeShorten = 6, int decimalPlaces = 1)
        {
            // Handle negative numbers
            bool isNegative = value < 0;
            float absValue = Math.Abs(value);

            // Check if we should shorten based on digit count
            int digitCount = GetDigitCount((int)absValue);

            if (digitCount < minDigitsBeforeShorten)
            {
                // Return normal formatting without shortening
                if (absValue < 1000)
                {
                    return isNegative ? $"-{(int)absValue}" : ((int)absValue).ToString();
                }
                else
                {
                    // Add commas for thousands but don't shorten
                    return isNegative ? $"-{FormatWithCommas((int)absValue)}" : FormatWithCommas((int)absValue);
                }
            }

            // Determine the appropriate suffix and divisor
            string suffix;
            float divisor;

            if (absValue >= 1000000f) // Millions
            {
                suffix = "m";
                divisor = 1000000f;
            }
            else // Thousands
            {
                suffix = "k";
                divisor = 1000f;
            }

            // Calculate the shortened value
            float shortenedValue = absValue / divisor;

            // Format with appropriate decimal places
            string formatString = $"F{decimalPlaces}";
            string numberString = shortenedValue.ToString(formatString);

            // Remove trailing .0 if no decimal places are needed
            if (decimalPlaces > 0)
            {
                numberString = numberString.TrimEnd('0').TrimEnd('.');
            }

            return isNegative ? $"-{numberString}{suffix}" : $"{numberString}{suffix}";
        }

        /// <summary>
        /// Overload for integer values
        /// </summary>
        public static string FormatNumberShort(int value, int minDigitsBeforeShorten = 6, int decimalPlaces = 1)
        {
            return FormatNumberShort((float)value, minDigitsBeforeShorten, decimalPlaces);
        }

        /// <summary>
        /// Gets the number of digits in an integer
        /// </summary>
        private static int GetDigitCount(int number)
        {
            if (number == 0) return 1;
            return (int)Math.Floor(Math.Log10(number)) + 1;
        }

        /// <summary>
        /// Formats a number with comma separators for thousands
        /// </summary>
        private static string FormatWithCommas(int number)
        {
            return number.ToString("N0");
        }
    }
}