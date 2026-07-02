using System;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace LHWMWrapperNameSpace
{
    public class LHWMWrapper
    {
        private static LHWMWrapper instance = null;

        private Dictionary<string, ISensor> sensors_map = new Dictionary<string, ISensor>();

        private Computer computer_instance = null;

        private Dictionary<string, List<Tuple<string, string, string>>> hardware_sensor_map
            = new Dictionary<string, List<Tuple<string, string, string>>>();

        public static LHWMWrapper GetInstance()
        {
            if (instance == null)
            {
                instance = new LHWMWrapper();
            }

            return instance;
        }

        private LHWMWrapper()
        {
            this.init();
            //computer.Close();
        }

        public Dictionary<string, List<Tuple<string, string, string>>> GetHardwareSensorMap()
        {
            return hardware_sensor_map;
        }

        public float GetSensorValue(string identifier)
        {
            if (sensors_map.TryGetValue(identifier, out ISensor sensor))
            {
                sensor.Hardware.Update();
                return sensors_map[identifier].Value ?? 0;
            }
            return 0;
        }

        public void SetControlValue(string identifier, float value)
        {
            if (sensors_map.TryGetValue(identifier, out ISensor sensor) && sensor.Control != null)
            {
                sensor.Control.SetSoftware(value);
            }
        }

        public void SetControlDefault(string identifier)
        {
            if (sensors_map.TryGetValue(identifier, out ISensor sensor) && sensor.Control != null)
            {
                sensor.Control.SetDefault();
            }
        }

        public string GetReport()
        {
            return computer_instance != null ? computer_instance.GetReport() : "";
        }

        private void init()
        {
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());
            computer_instance = computer;

            foreach (IHardware hardware in computer.Hardware)
            {
                string hw_key = hardware.Name + " : " + hardware.Identifier.ToString();

                hardware_sensor_map[hw_key] = new List<Tuple<string, string, string>>();

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        hardware_sensor_map[hw_key].Add(new Tuple<string, string, string>(
                            subhardware.Name + " " + sensor.Name,
                            sensor.SensorType.ToString("g"),
                            sensor.Identifier.ToString()
                        ));
                        sensors_map[sensor.Identifier.ToString()] = sensor;
                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    hardware_sensor_map[hw_key].Add(new Tuple<string, string, string>(
                        sensor.Name,
                        sensor.SensorType.ToString("g"),
                        sensor.Identifier.ToString()
                    ));
                    sensors_map[sensor.Identifier.ToString()] = sensor;
                }
            }
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
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
