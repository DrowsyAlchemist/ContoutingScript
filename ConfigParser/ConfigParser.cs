using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Contouring
{
    static class ConfigParser
    {
        private const string ConfigFileExtention = ".cfg";
        public static void SetConfig(Type configType)
        {
            string fileName = configType.Name + ConfigFileExtention;
            string pathToConfig = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(pathToConfig);
            string fileFullName = null;

            foreach (var file in files)
                if (file.Contains(fileName))
                    fileFullName = file;

            if (fileFullName == null)
                CreateConfigFile(pathToConfig + "/" + fileName, configType);
            else
                GetValuesFromConfigFile(fileFullName, configType);
        }

        private static void GetValuesFromConfigFile(string fileName, Type configType)
        {
            PropertyInfo[] configProperties = configType.GetProperties();
            string[] lines = File.ReadAllLines(fileName);

            foreach (var property in configProperties)
            {
                string value = GetValueFromLines(property.Name, lines);
                SetValueToProperty(value, property, configType);
                Logger.WriteWarning(property.Name + " = " + property.GetValue(configType));////
            }
        }

        private static void SetValueToProperty(string value, PropertyInfo property, Type configType)
        {
            try
            {
                object convertedValue = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(configType, convertedValue);
            }
            catch (Exception exception)
            {
                throw new Exception($"Can not set value \"{value}\" to property \"{property.Name}\".\n" + exception);
            }
        }

        private static string GetValueFromLines(string valueName, string[] lines)
        {
            foreach (var line in lines)
            {
                string clearLine = Regex.Replace(line, " ", "");
                string[] arrayLine = clearLine.Split('=');
                string valueInLineName = arrayLine[0];
                string valueInLine = arrayLine[1];

                if (valueName.Equals(valueInLineName))
                    return valueInLine;
            }
            throw new Exception("Can not find " + valueName + " in config file.");
        }

        private static void CreateConfigFile(string fileFullName, Type configType)
        {
            PropertyInfo[] properties = configType.GetProperties();
            List<string> lines = new List<string>();

            foreach (var property in properties)
                lines.Add(property.Name + " = " + property.GetValue(typeof(Config)));

            File.WriteAllLines(fileFullName, lines);
        }
    }
}
