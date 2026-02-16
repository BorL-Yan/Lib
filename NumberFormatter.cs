using UnityEngine;
using TMPro;
using System.Globalization;

public static class NumberFormatter
{
    public static string FormatValue(float value)
    {
        if (value >= 1000000)
        {
            return (value / 1000000f).ToString("0.##") + "M";
        }
        
        if (value >= 1000)
        {
            // "0.##" оставит до 2 знаков после запятой, если они есть
            return (value / 1000f).ToString("0.##") + "k";
        }

        return value.ToString("0");
    }
}