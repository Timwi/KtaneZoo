using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Zoo
{
    /// <summary>
    /// Some helper extensions methods for the KMBombInfo class.
    /// </summary>
    public static class KMBombInfoExtensions
    {
        #region JSON Types

        private class IndicatorJSON
        {
            public string label = null;
            public string on = null;

            public bool IsOn()
            {
                bool isOn = false;
                bool.TryParse(on, out isOn);
                return isOn;
            }
        }

        private class BatteryJSON
        {
            public int numbatteries = 0;
        }

        private class PortsJSON
        {
            public string[] presentPorts = null;
        }

        private class SerialNumberJSON
        {
            public string serial = null;
        }

        #endregion

        #region Helpers

        private static IEnumerable<T> GetJSONEntries<T>(KMBombInfo bombInfo, string queryKey, string queryInfo) where T : new()
        {
            return bombInfo.QueryWidgets(queryKey, queryInfo).Select(delegate (string queryEntry)
            {
                return JsonConvert.DeserializeObject<T>(queryEntry);
            });
        }

        private static IEnumerable<IndicatorJSON> GetIndicatorEntries(KMBombInfo bombInfo)
        {
            return GetJSONEntries<IndicatorJSON>(bombInfo, KMBombInfo.QUERYKEY_GET_INDICATOR, null);
        }

        private static IEnumerable<BatteryJSON> GetBatteryEntries(KMBombInfo bombInfo)
        {
            return GetJSONEntries<BatteryJSON>(bombInfo, KMBombInfo.QUERYKEY_GET_BATTERIES, null);
        }

        private static IEnumerable<PortsJSON> GetPortEntries(KMBombInfo bombInfo)
        {
            return GetJSONEntries<PortsJSON>(bombInfo, KMBombInfo.QUERYKEY_GET_PORTS, null);
        }

        private static IEnumerable<SerialNumberJSON> GetSerialNumberEntries(KMBombInfo bombInfo)
        {
            return GetJSONEntries<SerialNumberJSON>(bombInfo, KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        }

        #endregion

        #region Public Extensions

        public static bool IsIndicatorPresent(this KMBombInfo bombInfo, IndicatorLabel indicatorLabel)
        {
            return bombInfo.IsIndicatorPresent(indicatorLabel.ToString());
        }

        public static bool IsIndicatorPresent(this KMBombInfo bombInfo, string indicatorLabel)
        {
            return GetIndicatorEntries(bombInfo).Any((x) => indicatorLabel.Equals(x.label));
        }

        public static bool IsIndicatorOn(this KMBombInfo bombInfo, IndicatorLabel indicatorLabel)
        {
            return bombInfo.IsIndicatorOn(indicatorLabel.ToString());
        }

        public static bool IsIndicatorOn(this KMBombInfo bombInfo, string indicatorLabel)
        {
            return GetIndicatorEntries(bombInfo).Any((x) => x.IsOn() && indicatorLabel.Equals(x.label));
        }

        public static bool IsIndicatorOff(this KMBombInfo bombInfo, IndicatorLabel indicatorLabel)
        {
            return bombInfo.IsIndicatorOff(indicatorLabel.ToString());
        }

        public static bool IsIndicatorOff(this KMBombInfo bombInfo, string indicatorLabel)
        {
            return GetIndicatorEntries(bombInfo).Any((x) => !x.IsOn() && indicatorLabel.Equals(x.label));
        }

        public static IEnumerable<string> GetIndicators(this KMBombInfo bombInfo)
        {
            return GetIndicatorEntries(bombInfo).Select((x) => x.label);
        }

        public static IEnumerable<string> GetOnIndicators(this KMBombInfo bombInfo)
        {
            return GetIndicatorEntries(bombInfo).Where((x) => x.IsOn()).Select((x) => x.label);
        }

        public static IEnumerable<string> GetOffIndicators(this KMBombInfo bombInfo)
        {
            return GetIndicatorEntries(bombInfo).Where((x) => !x.IsOn()).Select((x) => x.label);
        }

        public static int GetBatteryCount(this KMBombInfo bombInfo)
        {
            return GetBatteryEntries(bombInfo).Sum((x) => x.numbatteries);
        }

        public static int GetBatteryCount(this KMBombInfo bombInfo, BatteryType batteryType)
        {
            return GetBatteryEntries(bombInfo).Where((x) => x.numbatteries == (int) batteryType).Sum((x) => x.numbatteries);
        }

        public static int GetBatteryHolderCount(this KMBombInfo bombInfo)
        {
            return GetBatteryEntries(bombInfo).Count();
        }

        public static int GetPortCount(this KMBombInfo bombInfo)
        {
            return GetPortEntries(bombInfo).Sum((x) => x.presentPorts.Length);
        }

        public static int GetPortCount(this KMBombInfo bombInfo, PortType portType)
        {
            return bombInfo.GetPortCount(portType.ToString());
        }

        public static int GetPortCount(this KMBombInfo bombInfo, string portType)
        {
            return GetPortEntries(bombInfo).Sum((x) => x.presentPorts.Count((y) => portType.Equals(y)));
        }

        public static int GetPortPlateCount(this KMBombInfo bombInfo)
        {
            return GetPortEntries(bombInfo).Count();
        }

        public static IEnumerable<string> GetPorts(this KMBombInfo bombInfo)
        {
            return GetPortEntries(bombInfo).SelectMany((x) => x.presentPorts);
        }

        public static IEnumerable<string[]> GetPortPlates(this KMBombInfo bombInfo)
        {
            return GetPortEntries(bombInfo).Select((x) => x.presentPorts);
        }

        public static bool IsPortPresent(this KMBombInfo bombInfo, PortType portType)
        {
            return bombInfo.IsPortPresent(portType.ToString());
        }

        public static bool IsPortPresent(this KMBombInfo bombInfo, string portType)
        {
            return GetPortEntries(bombInfo).Any((x) => x.presentPorts != null && x.presentPorts.Any((y) => portType.Equals(y)));
        }

        public static string GetSerialNumber(this KMBombInfo bombInfo)
        {
            var ret = GetSerialNumberEntries(bombInfo).FirstOrDefault();
            return ret == null ? null : ret.serial;
        }

        public static IEnumerable<char> GetSerialNumberLetters(this KMBombInfo bombInfo)
        {
            return GetSerialNumber(bombInfo).Where((x) => x < '0' || x > '9');
        }

        public static IEnumerable<int> GetSerialNumberNumbers(this KMBombInfo bombInfo)
        {
            return GetSerialNumber(bombInfo).Where((x) => x >= '0' && x <= '9').Select((y) => int.Parse("" + y));
        }

        #endregion
    }
}
