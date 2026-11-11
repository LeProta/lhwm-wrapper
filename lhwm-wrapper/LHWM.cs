using System;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace LHWMWrapperNameSpace
{
    public class LHWMWrapper
    {
        // Several plugins call in from their own threads: one lock serialises init, updates and controls.
        private static readonly object sync = new object();
        private static LHWMWrapper instance;
        private readonly Dictionary<string, ISensor> sensors_map = new Dictionary<string, ISensor>();
        private readonly Dictionary<string, List<Tuple<string, string, string>>> hardware_sensor_map
            = new Dictionary<string, List<Tuple<string, string, string>>>();
        private Computer computer_instance;

        // Hardware.Update() refreshes every sensor of that hardware: do it at most once per window,
        // not once per value read (a plugin reading 14 fan values would otherwise update 14 times).
        private const long UpdateWindowMs = 250;
        private readonly Dictionary<IHardware, long> last_update_ms = new Dictionary<IHardware, long>();
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        public static LHWMWrapper GetInstance()
        {
            lock (sync)
            {
                return instance ?? (instance = new LHWMWrapper());
            }
        }

        private LHWMWrapper()
        {
            init();
        }

        public Dictionary<string, List<Tuple<string, string, string>>> GetHardwareSensorMap()
        {
            return hardware_sensor_map;
        }

        public float GetSensorValue(string identifier)
        {
            lock (sync)
            {
                if (!sensors_map.TryGetValue(identifier, out ISensor sensor))
                    return 0;

                long now = clock.ElapsedMilliseconds;
                if (!last_update_ms.TryGetValue(sensor.Hardware, out long last) || now - last >= UpdateWindowMs)
                {
                    sensor.Hardware.Update();
                    last_update_ms[sensor.Hardware] = now;
                }
                return sensor.Value ?? 0;
            }
        }

        public void SetControlValue(string identifier, float value)
        {
            lock (sync)
            {
                if (sensors_map.TryGetValue(identifier, out ISensor sensor) && sensor.Control != null)
                {
                    sensor.Control.SetSoftware(value);
                }
            }
        }

        public void SetControlDefault(string identifier)
        {
            lock (sync)
            {
                if (sensors_map.TryGetValue(identifier, out ISensor sensor) && sensor.Control != null)
                {
                    sensor.Control.SetDefault();
                }
            }
        }

        public string GetReport()
        {
            lock (sync)
            {
                return computer_instance != null ? computer_instance.GetReport() : "";
            }
        }

        private void init()
        {
            // LHM 0.9.5+ talks to the hardware through PawnIO (Microsoft-signed, anti-cheat
            // safe). No WinRing0 / inpoutx64 fallback, so every category can stay enabled.
            // Requires PawnIO installed: https://pawnio.eu
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());
            computer_instance = computer;

            foreach (IHardware hardware in computer.Hardware)
            {
                string hw_key = hardware.Name + " : " + hardware.Identifier;
                hardware_sensor_map[hw_key] = new List<Tuple<string, string, string>>();

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        addSensor(hw_key, subhardware.Name + " " + sensor.Name, sensor);
                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    addSensor(hw_key, sensor.Name, sensor);
                }
            }
        }

        private void addSensor(string hardware, string name, ISensor sensor)
        {
            string identifier = sensor.Identifier.ToString();
            hardware_sensor_map[hardware].Add(new Tuple<string, string, string>(
                name,
                sensor.SensorType.ToString("g"),
                identifier));
            sensors_map[identifier] = sensor;
        }
    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
