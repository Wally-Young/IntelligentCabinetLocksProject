using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tools
{
    public class ConfigLoad
    {
        public ConfigLoad(string conFilePath)
        {
            this.conFilePath = conFilePath;
            ReadConfig();
        }

        /// <summary>
        /// Set Config File Path
        /// </summary>
        #region ConFilePath

        string conFilePath;

        public string ConFilePath
        {
            set { conFilePath = value; }
            get { return conFilePath; }
        }
        #endregion

        /// <summary>
        /// Content of Config file
        /// </summary>
        private List<string> configName = new List<string>(); //List of Names
        private List<string> configValue = new List<string>(); //List of Values

        /// <summary>
        /// Read Config File
        /// </summary>
        /// <returns></returns>
        public bool ReadConfig()
        {
            //check if the file is existed
            if (!File.Exists(this.conFilePath))
            {
                return false;
            }

            StreamReader sr = new StreamReader(this.conFilePath,Encoding.Default);
            string line;
            while ((line=sr.ReadLine()) != null)
            {
                line = line.Trim();
                string cName, cValues;
                string[] cLine = line.Split('=');
                if (cLine.Length == 2)
                {
                    cName = cLine[0];
                    cValues = cLine[1];
                    configName.Add(cName);
                    configValue.Add(cValues);
                }
            }
            sr.Close();
            return true;
        }

        #region GetConfigValue
        /// <summary>
        /// Get config value which is a string
        /// </summary>
        /// <param name="cName"></param>
        /// <returns></returns>
        public string GetStringValue(string cName)
        {
            for (int i = 0; i < configName.Count; i++)
            {
                if (configName[i].Equals(cName))
                {
                    return configValue[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Get config value which is an int
        /// </summary>
        /// <param name="cName"></param>
        /// <returns></returns>
        public int GetIntValue(string cName)
        {
            for (int i = 0; i < configName.Count; i++)
            {
                if (configName[i].Equals(cName))
                {
                    int result;
                    if (int.TryParse(configValue[i], out result))
                    {
                        return result;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Get config value which is a float
        /// </summary>
        /// <param name="cName"></param>
        /// <returns></returns>
        public float GetFloatValue(string cName)
        {
            for (int i = 0; i < configName.Count; i++)
            {
                if (configName[i].Equals(cName))
                {
                    float result;
                    if (float.TryParse(configValue[i], out result))
                    {
                        return result;
                    }
                }
            }
            return 0;
        }
        #endregion

    }

    public class ConfigSet
    {
        public ConfigSet(string configFilePath, bool isRead)
        {
            this.configFilePath = configFilePath;
            if (File.Exists(this.configFilePath) && isRead)
            {
                ReadConfig();
            }
        }

        public ConfigSet(string configFilePath)
        {
            this.configFilePath = configFilePath;
            if (File.Exists(this.configFilePath))
            {
                ReadConfig();
            }
        }

        private string configFilePath;
        private List<string> configName = new List<string>(); //List of names
        private List<string> configValue = new List<string>(); //List of values

        public void SetConfigValue(string cName, string cValue)
        {
            bool ishere = false;

            //check if it is already existed
            if (configName.Count != 0)
            {
                for (int i = 0; i < configName.Count; i++)
                {
                    if (configName[i].Equals(cName))
                    {
                        configValue[i] = cValue;
                        ishere = true;
                    }
                }
            }
            if (!ishere)
            {
                configName.Add(cName);
                configValue.Add(cValue);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cf"></param>
        /// <returns></returns>
        public bool WriteConfigToFile(ConfigFile cf)
        {
            StreamWriter sw;

            switch (cf)
            {
                case ConfigFile.newFile:
                {
                    sw = new StreamWriter(this.configFilePath, false);
                    break;
                }
                case ConfigFile.appendFile:
                {
                    sw = new StreamWriter(this.configFilePath,true);
                    break;
                }
                default:
                {
                    sw = new StreamWriter(this.configFilePath);
                    break;
                }
            }
            try
            {
                for (int i = 0; i < configName.Count; i++)
                {
                    sw.WriteLine("{0}={1}", configName[i], configValue[i]);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                sw.Close();
            }
            return true;
        }

        /// <summary>
        /// Read config
        /// </summary>
        /// <returns></returns>
        private bool ReadConfig()
        {
            StreamReader sr = new StreamReader(this.configFilePath,Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                string cName, cValue;
                string[] cLine = line.Split('=');
                if (cLine.Length == 2)
                {
                    cName = cLine[0];
                    cValue = cLine[1];
                    configName.Add(cName);
                    configValue.Add(cValue);
                }
            }
            sr.Close();
            return true;
        }
    }

    /// <summary>
    /// File attribute
    /// </summary>
    public enum ConfigFile { newFile, appendFile }
}
