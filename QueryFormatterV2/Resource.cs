using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using CDSSVectorTagger;

namespace SupportAgent
{
    /// <summary>
    /// Class for static resource (templates and dictionary files)
    /// </summary>
    public static class Resource
    {
        public static bool Load(string dir)
        {
            bool success = true;
            success &= LoadStemmingTerms(dir + @"StemmingDict.extended.txt");
            success &= LoadStopWords(dir + "stop_words.txt");
            success &= LoadVerbSet(dir + "verb_dict.txt");
            success &= LoadIdf(dir + "idf_small.tsv");
            success &= LoadProductList(dir+"product_list.txt");
            success &= LoadTaxonomyKeys(dir + "taxonomy_keys.txt");
            success &= LoadMoveList(dir+"move_things.txt");
            success &= LoadCdssm(dir + @"cdssm\DSSM.10.model.Query", dir + @"cdssm\DSSM.10.model.Setting", dir + @"cdssm\l3g.txt");
            success &= LoadQC(dir);
            success &= LoadGoodDict(dir+@"dict.final.txt");
           // success &= LoadProductList(dir+"goodList.txt");
            //success &= LoadSpellingResource(dir+"dict.final.txt",dir+"id_dict.txt",dir+"spelling_checker_wordSuffix2.txt",
                //dir+"unigram.txt",dir+"trigramSuffix.txt");
            return success;
        }

        private static bool LoadQC(string dir)
        {
            string path = dir + @"qc\data\package";
            string actionfile = dir + @"qc\data\action_sure.txt";
            string targetfile = dir + @"qc\data\target.txt";
            string proPath = dir + @"qc\data\product_list.txt";
            string sureFile = dir + @"qc\data\sureList.txt";
            string[] alines = System.IO.File.ReadAllLines(actionfile);
            foreach (string line in alines)
            {
                if (!m_Actions.Contains(line.ToLower())) m_Actions.Add(line.ToLower());
            }

            alines = System.IO.File.ReadAllLines(targetfile);
            foreach (string line in alines)
            {
                if (!m_Target.Contains(line.ToLower())) m_Target.Add(line.ToLower());
            }
            alines = System.IO.File.ReadAllLines(proPath);
            foreach (string line in alines)
            {
                if (m_productList2.Contains(line.ToLower()) == false)
                    m_productList2.Add(line.ToLower());
            }
            alines = System.IO.File.ReadAllLines(sureFile);
            foreach (string line in alines)
            {
                if (line.StartsWith("\t"))
                {
                    string t = line.Substring(1);
                    string[] tokens = t.Split(":".ToArray());
                    if (m_sureList.Contains(tokens[0]) == false)
                        m_sureList.Add(tokens[0].ToLower());

                }
                else
                {
                    string[] tokens = line.Split(":".ToArray());
                    double prop = double.Parse(tokens[1]);
                    if (prop >= 0.9 && m_badList.Contains(tokens[0]) == false)
                    {
                        m_badList.Add(tokens[0].ToLower());
                    }
                }
            }
            ICEClassifierLocalForTheme.LocalClassifier.Init(path);
            //m_fea.init(dir+"qc\\");
            //m_sc.init(dir+"qc\\");
            return true;
        }
        private static bool LoadStemmingTerms(string file)
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

        private static bool LoadStopWords(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while((line = sr.ReadLine()) != null)
                {
                    m_hStopWords.Add(line);
                }
            }
            return true;
        }

        private static bool LoadVerbSet(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    m_hVerbSet.Add(line);
                }
            }
            return true;
        }

        private static bool LoadIdf(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if(m_dIdf.ContainsKey(parts[0])==false) 
                        m_dIdf.Add(parts[0], Convert.ToDouble(parts[1]));

                }
            }
            return true;
        }

        private static bool LoadTaxonomyKeys(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    m_hTaxonomyKeys.Add(line);
                }
            }
            return true;
        }

        private static bool LoadCdssm(string pathModel, string pathSetting, string pathVoc)
        {
            try
            {
                m_vwCDSSMVector = new VectorWrapper(pathModel, pathSetting, pathVoc);
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("CDSSM loading failure: {0}", e.Message);
            }

            return true;
        }
        private static bool LoadSpellingResource(string realDict,string dictId,string wordSuffix_2gram,string unigram)
        {

            m_spellingChecker = new SupportAgent.SpellingChecker();
            m_spellingChecker.loadModel(wordSuffix_2gram, dictId, realDict,unigram);

            return true;
        }
        private static bool LoadSpellingResource(string realDict, string dictId, string wordSuffix_2gram, string unigram,string wordSuffix_trigram)
        {

            m_spellingChecker = new SupportAgent.SpellingChecker();
            m_spellingChecker.loadModel(wordSuffix_2gram, dictId, realDict, unigram,wordSuffix_trigram);

            return true;
        }
        private static bool LoadProductList(string productFile)
        {
            using (StreamReader sr = new StreamReader(productFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (m_productList.Contains(line.ToLower()) == false)
                        m_productList.Add(line.ToLower());
                }
            }
            return true;
        }
        private static bool LoadMoveList(string moveFileName)
        {
            m_moveList.Clear();
            using (StreamReader sr = new StreamReader(moveFileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (m_moveList.Contains(line.ToLower()) == false)
                        m_moveList.Add(line.ToLower());
                }
            }
            return true;
        }
        private static bool LoadPosTaggerBigram(string file)
        {

            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    string[] strs_tm = line.Split(@":".ToArray());
                    posTaggerBigram.Add(strs_tm[0],new Dictionary<string,double>());
                    string[] wordSuffixMap = strs_tm[1].Split(@"\t".ToArray());
                    string[] stringSeparators = new string[] { "&" };
                    foreach (string token in wordSuffixMap)
                    {
                        if (token.Length == 0) continue;
                        string[] tmp = token.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        posTaggerBigram[strs_tm[0]].Add(tmp[0], double.Parse(tmp[1]));
                    }
                }
            }
            return true;
        }
        private static bool LoadGoodDict(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    if (m_goodDict.Contains(line.ToLower()) == false)
                    {
                        m_goodDict.Add(line.ToLower());
                    }
                }
            }
            return true;
        }
        public static Dictionary<string, string> m_stemmingDict = new Dictionary<string, string>();
        public static HashSet<string> m_hVerbSet = new HashSet<string>();
        public static HashSet<string> m_hStopWords = new HashSet<string>();
        public static Dictionary<string, double> m_dIdf = new Dictionary<string, double>();
        public static HashSet<string> m_hTaxonomyKeys = new HashSet<string>();
        public static VectorWrapper m_vwCDSSMVector;
        public static SpellingChecker m_spellingChecker;
        public static HashSet<string> m_productList=new HashSet<string>();
        public static Dictionary<string, Dictionary<string, double>> posTaggerBigram = new Dictionary<string, Dictionary<string, double>>();
        public static HashSet<string> m_Actions = new HashSet<string>();
        public static HashSet<string> m_Phrase = new HashSet<string>();
        public static HashSet<string> m_Target = new HashSet<string>();
        public static HashSet<string> m_productList2 = new HashSet<string>();
        public static HashSet<string> m_sureList = new HashSet<string>();
        public static HashSet<string> m_badList = new HashSet<string>();
        public static HashSet<string> m_moveList = new HashSet<string>();
        public static HashSet<string> m_goodDict = new HashSet<string>();
    }

}