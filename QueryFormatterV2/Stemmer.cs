using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SupportAgent
{
    public class Stemmer
    {
        public Dictionary<string, string> m_stemmingDict = new Dictionary<string, string>();
        public Stemmer(string dir)
        {
            LoadStemmingTerms(dir + @"StemmingDict.extended.txt");
        }
        public Stemmer()
        {

        }
        public bool LoadStemmingTerms(string file)
        {
            StreamReader srStemmingTerm = new StreamReader(file);

            if (srStemmingTerm != null)
            {
                string line = "";
                while ((line = srStemmingTerm.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) continue;
                    string[] words = line.Trim().Trim().Split('\t');
                    if (words.Length == 2)
                    {
                        if (m_stemmingDict.ContainsKey(words[0]) == false)
                        {
                            m_stemmingDict.Add(words[0], words[1]);
                        }
                    }
                }
                srStemmingTerm.Close();
                return true;
            }
            else return false;
        }
        public string GetBaseFormSentence(string sentence)
        {
            string baseSentence = "";
            string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            string[] words = sentence.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i].Trim();
                if (word.Length == 0) continue;

                string baseWord = "";
                if (word == "i" || word == "need")
                    baseWord = word;
                else
                    baseWord = GetBaseFormWord(word);

                if (i == 0) baseSentence = baseWord;
                else baseSentence += " " + baseWord;
            }

            return baseSentence;
        }

        public List<string> GetBaseFormSentence(List<string> words)
        {
            string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            List<string> stemWords = new List<string>();
            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i].Trim();
                if (word.Length == 0) continue;

                string baseWord = "";
                if (word == "i" || word == "need")
                    baseWord = word;
                else
                    baseWord = GetBaseFormWord(word);
                stemWords.Add(baseWord);
            }
            return stemWords;
        }
        public string GetBaseFormWord(string word)
        {
            string baseWord = "";
            if (m_stemmingDict.ContainsKey(word) == true)
                baseWord = m_stemmingDict[word];
            else baseWord = word;

            return baseWord;
        }


    }
}