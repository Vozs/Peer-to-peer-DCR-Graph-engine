using System;
using System.IO;
using System.Reflection;
using Node.Interfaces;
namespace Node
{
    public class Config : IConfig
    {
        public string Name { get => name; set => name = value; }

        public IClient MainNode { get => mainNode; set => mainNode = value; }
        public int ClientTimeoutMs { get => clientTimeoutMs; set => clientTimeoutMs = value; }

        public string MainNodeIP { get => mainNodeIP; set => mainNodeIP = value; }

        public int MainNodePort { get => mainNodePort; set => mainNodePort = value; }

        private string name;
        private IClient mainNode;
        private string mainNodeIP;
        private int mainNodePort;
        private int clientTimeoutMs;

        /// <summary>
        /// Constructs an instance of Config by reading from the file designated by the "path" parameter.
        /// </summary>
        /// <param name="path">The path of the config file</param>
        public Config(string path)
        {
            StreamReader reader = new StreamReader(path);
            string file = reader.ReadToEnd();
            reader.Close();

            string[] lines = file.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i != lines.Length)
            {
                switch (lines[i])
                {
                    case "[Properties]":
                        i = SetProperties(lines, i + 1);
                        break;
                }
            }
            mainNode = new Client(this);
        }
        /// <summary>
        /// Sets the properties matching the ones in the .ini file by converting the input to the type of the property.
        /// </summary>
        /// <param name="lines">All the lines of the .ini document</param>
        /// <param name="i">The line to start at, and iterate with</param>
        /// <returns>The line where the properties end</returns>
        private int SetProperties(string[] lines, int i)
        {
            Type type = this.GetType();
            while (i != lines.Length && lines[i][0] != '[')
            {
                string pName = lines[i].Substring(0, lines[i].IndexOf('='));
                string pValue = lines[i].Substring(lines[i].IndexOf('=') + 1);
                PropertyInfo pInfo = type.GetProperty(pName);
                i++;
                switch (pInfo.PropertyType.Name)
                {
                    case "String":
                        pInfo.SetValue(this, pValue);
                        break;

                    case "Int32":
                        pInfo.SetValue(this, Convert.ToInt32(pValue));
                        break;

                    case "IClient":
                        this.mainNodePort = Convert.ToInt32(pValue.Substring(pValue.IndexOf(':') +1));
                        this.mainNodeIP = (pValue.Substring(0, pValue.IndexOf(':')));
                        break;
                }
            }
            return i;
        }
    }


}
