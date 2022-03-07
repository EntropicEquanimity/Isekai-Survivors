using System;
using UnityEngine;

namespace BlondieUtils
{
    public static class Utils
    {
        public static string FormatTimeToHours(float time)
        {
            int hours = Mathf.RoundToInt(time / 3600f);
            int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
        public static string FormatTimeToMinutes(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}