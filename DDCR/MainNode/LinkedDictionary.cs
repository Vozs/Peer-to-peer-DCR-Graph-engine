using System.Collections.Generic;

namespace DDCR
{
    /// <summary>
    /// Consists of two Dictionaries, which are linked together by the first dictionary's value, and second dictionary's key - which are identical.
    /// </summary>
    public class LinkedDictionary
    {
        private Dictionary<string, HashSet<string>> dictionary;
        private Dictionary<string, HashSet<string>> subDictionary;

        /// <summary>
        /// The first dictionary contained in LinkedDictionary
        /// </summary>
        public Dictionary<string, HashSet<string>> Dictionary
        {
            get => dictionary;
            set => dictionary = value;
        }

        /// <summary>
        /// The second dictionary contained in LinkedDictionary
        /// </summary>
        public Dictionary<string, HashSet<string>> SubDictionary
        {
            get => subDictionary;
            set => subDictionary = value;
        }

        public LinkedDictionary()
        {
            Dictionary = new Dictionary<string, HashSet<string>>();
            SubDictionary = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Adds an item to the LinkedDictionary.
        /// </summary>
        public void Add(string key, string subKey, string content)
        {
            if (Dictionary.ContainsKey(key))
                Dictionary[key].Add(subKey);
            else
                Dictionary.Add(key, new HashSet<string> { subKey });

            if (SubDictionary.ContainsKey(subKey))
                SubDictionary[subKey].Add(content);
            else
                SubDictionary.Add(subKey, new HashSet<string> { content });
        }

        /// <summary>
        /// Clears the LinkedDictionary.
        /// </summary>
        public void Clear()
        {
            dictionary.Clear();
            subDictionary.Clear();
        }
    }
}