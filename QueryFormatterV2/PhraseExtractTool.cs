using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportAgent
{
    class PhraseExtractTool
    {
        //open the file and split by sentence, to see 
        //need a stopword list
        public void loadStopWords(string dir)
        {
            stopwords.Clear();
            string fileName = dir + "stop_words.txt";
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (stopwords.Contains(line.ToLower()) == false)
                {
                    stopwords.Add(line.ToLower());
                }
            }
            sr.Close();
        }
        public void loadActionSure(string dir)
        {
            actionSure.Clear();
            string fileName = dir + "action_sure.txt";
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (actionSure.Contains(line.ToLower()) == false)
                {
                    actionSure.Add(line.ToLower());
                }
            }
            sr.Close();
        }
        public void loadResource(string dir)
        {
            loadActionSure(dir);
            loadStopWords(dir);
            LoadMIModel(dir);
            stemmer = new Stemmer(dir);
        }
        public string baseDir = @"D:\work\data\queryformat\";
        HashSet<string> stopwords = new HashSet<string>();
        HashSet<string> actionSure = new HashSet<string>();
        void scanForMI(string fileName, string outDir)
        {
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/", ".", "?", ";", "!", ":" };
            //SupportAgent.Stemmer stemmer = new SupportAgent.Stemmer(baseDir);
            //init stop words 
            loadStopWords(baseDir);
            StreamReader sr = new StreamReader(fileName);
            string line = null, aline = null;
            int total = 0, i = 0, j = 0;
            while ((aline = sr.ReadLine()) != null)
            {
                string[] ts = aline.Split('\t');
                line = ts[3];
                for (i = 4; i <= 7; i++)
                {
                    if (ts[i] != "N/A") line += (". " + ts[i]);
                }
                line = line.ToLower();
                string[] senSeg = new string[] { ",", ".", ";", "!", "?" };
                string[] segs = line.Split(senSeg, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sen in segs)
                {

                    string[] words = sen.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
                    for (i = 0; i < words.Length; i++) words[i] = stemmer.GetBaseFormWord(words[i]);
                    foreach (string word in words)
                    {
                        if (stopwords.Contains(word)) continue;
                        total++;
                        if (uniGramInfo.ContainsKey(word)) uniGramInfo[word]++;
                        else uniGramInfo[word] = 1;
                    }
                    for (i = 0; i < words.Length; i++)
                    {
                        if (stopwords.Contains(words[i])) continue;
                        for (j = 0; j < words.Length; j++)
                        {
                            if (stopwords.Contains(words[j]) || words[i] == words[j]) continue;
                            if (bigramInfo.ContainsKey(words[i] + "&&&" + words[j])) bigramInfo[words[i] + "&&&" + words[j]]++;
                            else bigramInfo[words[i] + "&&&" + words[j]] = 1;
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, double> unikv in uniGramInfo.ToList())
            {
                uniGramInfoCopy[unikv.Key] = unikv.Value;
                uniGramInfo[unikv.Key] /= total;
            }
            foreach (KeyValuePair<string, double> bigram in bigramInfo.ToList())
            {
                bigramInfo[bigram.Key] /= total;
                string[] ss = new string[] { "&&&" };
                string[] tokens = bigram.Key.Split(ss, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 1 && uniGramInfo.ContainsKey(tokens[0]) && uniGramInfo.ContainsKey(tokens[1]))
                {
                    bigramInfo[bigram.Key] /= (uniGramInfo[tokens[0]] * uniGramInfo[tokens[1]]);
                }
            }
            string outFileName = outDir + "MIinfo.txt";
            StreamWriter sw = new StreamWriter(outFileName);

            foreach (KeyValuePair<string, double> bigram in bigramInfo)
            {
                sw.WriteLine(bigram.Key + ":" + bigram.Value);

            }
            sw.Close();
            sr.Close();
        }
        public void LoadMIModel(string dir)
        {
            string fileName = dir + "MIinfo.txt";
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            bigramInfo.Clear();
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(":".ToArray());
                bigramInfo[tokens[0]] = double.Parse(tokens[1]);
            }
            sr.Close();
        }
        Dictionary<string, double> uniGramInfo = new Dictionary<string, double>();
        Dictionary<string, double> bigramInfo = new Dictionary<string, double>();
        Dictionary<string, double> uniGramInfoCopy = new Dictionary<string, double>();
        sentenceClassifier sc = new sentenceClassifier();
        public SupportAgent.Stemmer stemmer = null;
        void scanForUniGramBigram(string fileName)
        {
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/", ".", "?", ";", "!", ":" };
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            double total = 0;
            double bigramTotal = 0;
            string tline = null;
            while ((tline = sr.ReadLine()) != null)
            {
                line = tline.Split('\t')[3].ToLower();
                string[] tokens = line.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;
                foreach (string token in tokens)
                {
                    total++;
                    if (uniGramInfo.ContainsKey(token) == false)
                    {
                        uniGramInfo.Add(token, 1);
                    }
                    else
                        uniGramInfo[token]++;
                    if (i != tokens.Length - 1)
                    {
                        bigramTotal++;
                        if (bigramInfo.ContainsKey(tokens[i] + "&" + tokens[i + 1]) == false)
                        {
                            bigramInfo.Add(tokens[i] + "&" + tokens[i + 1], 1);
                        }
                        else
                        {
                            bigramInfo[tokens[i] + "&" + tokens[i + 1]]++;
                        }
                    }
                    i++;
                }
            }

            foreach (KeyValuePair<string, double> unikv in uniGramInfo.ToList())
            {
                uniGramInfoCopy[unikv.Key] = unikv.Value;
                uniGramInfo[unikv.Key] /= total;
            }
            double aa = bigramTotal * bigramTotal;
            foreach (KeyValuePair<string, double> bigram in bigramInfo.ToList())
            {
                bigramInfo[bigram.Key] /= aa;
            }
            sr.Close();
        }
        private static int compare2(KeyValuePair<string, double> a, KeyValuePair<string, double> b)
        {
            return -a.Value.CompareTo(b.Value);
        }
        void sortByScore()
        {
            Dictionary<string, double> bigScore = new Dictionary<string, double>();
            foreach (string bigram in bigramInfo.Keys)
            {
                string[] seprator_ = new string[] { "&" };
                string[] tokens = bigram.Split(seprator_, StringSplitOptions.None);

                if (uniGramInfo.ContainsKey(tokens[0]) && uniGramInfo.ContainsKey(tokens[1]))
                {
                    if (uniGramInfoCopy[tokens[0]] < 100 || uniGramInfoCopy[tokens[1]] < 100)
                        continue;
                    bigScore[bigram] = bigramInfo[bigram] / (uniGramInfo[tokens[0]] * uniGramInfo[tokens[1]]);
                }
            }
            List<KeyValuePair<string, double>> bigramList = bigScore.ToList();
            bigramList.Sort(compare2);
            string outFileName = "D:\\tmp\\myphrase2.txt";
            StreamWriter sw = new StreamWriter(outFileName);
            foreach (KeyValuePair<string, double> kv in bigramList)
            {
                sw.WriteLine(kv.Key + ":" + kv.Value);
            }
            sw.Close();
        }
        public string keySentence(string line)
        {
            string[] senSeg = new string[] { ",", ".", ";", "!", "?" };
            string[] segs = line.Split(senSeg, StringSplitOptions.RemoveEmptyEntries);
            string ret = "";
            double maxScore = -10;
            foreach (string frag in segs)
            {
                double score = sc.queryScore(frag);
                if (score > maxScore) maxScore = score;
            }
            foreach (string sent in segs)
            {
                //if (sc.queryScore(sent) < 0.1) continue;
                double score = sc.queryScore(sent);
                if(segs.Length>1&&score == maxScore)
                {
                    ret+=(" "+sent);
                    continue;
                }
                ret += (" " + removeTwoWords(sent));

            }
            return ret;
        }
        public string removeWords(string line)
        {
            string[] senSeg = new string[] { ",", ".", ";", "!", "?" };
            string[] segs = line.Split(senSeg, StringSplitOptions.RemoveEmptyEntries);
            string ret = "";
            foreach (string sent in segs)
            {
                //if (sc.queryScore(sent) < 0.1) continue;
                ret += (" " + removeTwoWords(sent));

            }
            return ret;
        }
        public List<string> allSubs(string line)
        {
            line = line.ToLower();
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/", ".", "?", ";", "!", ":" };
            string[] tokens = line.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
            string[] sourceTokens = new string[tokens.Length];
            int i = 0;
            for (i = 0; i < sourceTokens.Length; i++) sourceTokens[i] = tokens[i];
            List<string> allsubs = new List<string>();
            StreamWriter sw = new StreamWriter(baseDir + "subScore.txt");
            for (i = 0; i < tokens.Length; i++) tokens[i] = stemmer.GetBaseFormWord(tokens[i].ToLower());
            for (i = (int)Math.Pow(2, tokens.Length)-1; i >= 0; i--)
            {
                string ret = "";
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (((1 << j) & i) > 0)
                    {
                        ret += " " + tokens[j];
                    }
                }
                ret = ret.Trim();
                tokens = ret.Split(' ');
                for (i = 0; i < tokens.Length; i++)
                {
                    int cnt = 0;
                    double score = 0.0;
                    for (int j = 0; j < tokens.Length; j++)
                    {
                        if (tokens[i] == tokens[j]) continue;
                        cnt++;
                        string a = tokens[i] + "&&&" + tokens[j], b = tokens[j] + "&&&" + tokens[i];
                        if (bigramInfo.ContainsKey(tokens[i] + "&&&" + tokens[j]))
                        {
                            double score1 = 0.0, score2 = 0.0;
                            if (bigramInfo.ContainsKey(a)) score1 = bigramInfo[a];
                            if (bigramInfo.ContainsKey(b)) score2 = bigramInfo[b];
                            score += Math.Max(score1, score2);
                        }

                    }
                    if (cnt != 0) score /= cnt;
                    sw.WriteLine(ret + ":" + score);
                }
                //allsubs.Add(ret);
            }
            
            
            sw.Close();
            return allsubs;
        }
        public bool canAdd(List<string> tokens,int[] selected,int i){
            if (i >= tokens.Count()) return false;
            int cnt = 0;
            double score = 0.0;
            for (int j = 0; j < tokens.Count(); j++)
            {
                if (tokens[i] == tokens[j]||selected[j]==0) continue;
                cnt++;
                string a = tokens[i] + "&&&" + tokens[j], b = tokens[j] + "&&&" + tokens[i];
                if (bigramInfo.ContainsKey(tokens[i] + "&&&" + tokens[j]))
                {
                    double score1 = 0.0, score2 = 0.0;
                    if (bigramInfo.ContainsKey(a)) score1 = bigramInfo[a];
                    if (bigramInfo.ContainsKey(b)) score2 = bigramInfo[b];
                    score += Math.Max(score1, score2);
                }

            }
            return (score / cnt) > 7;
        }
        
        public string removeTwoWords(string line)
        {
            line = line.ToLower();
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/", ".", "?", ";", "!", ":" };
            string[] tokens = line.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
            string[] sourceTokens = new string[tokens.Length];
            int i = 0;
            for(i=0;i<sourceTokens.Length;i++) sourceTokens[i] = tokens[i];
            Dictionary<string, double> strToScore = new Dictionary<string, double>();
            for (i = 0; i < tokens.Length; i++) tokens[i] = stemmer.GetBaseFormWord(tokens[i].ToLower());
            int[] selected = QueryFormatter.SelectTerms2(tokens.ToList(), -1);
            for (i = 0; i < tokens.Length; i++)
            {
                int cnt = 0;
                double score = 0.0;
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (tokens[i] == tokens[j]) continue;
                    cnt++;
                    string a = tokens[i] + "&&&" + tokens[j], b = tokens[j] + "&&&" + tokens[i];
                    if (bigramInfo.ContainsKey(tokens[i] + "&&&" + tokens[j]))
                    {
                        double score1 = 0.0, score2 = 0.0;
                        if (bigramInfo.ContainsKey(a)) score1 = bigramInfo[a];
                        if (bigramInfo.ContainsKey(b)) score2 = bigramInfo[b];
                        score += Math.Max(score1, score2);
                    }

                }
                strToScore[tokens[i]] = score / cnt;
            }
            List<KeyValuePair<string, double>> strScorelist = strToScore.ToList();
            strScorelist.Sort(compare2);
            Dictionary<string, double> stoscore = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in strScorelist)
            {
                stoscore[kv.Key] = kv.Value;
               // Console.WriteLine(kv.Key + ":" + kv.Value);
            }
            string ret = "";
            i = 0;
            foreach (string s in tokens)
            {
                if (stopwords.Contains(s)||selected[i]==-1)
                {
                    i++;
                    continue;
                }
                if ((stoscore.ContainsKey(s) && stoscore[s] > 4) || (selected[i] == 1) || (actionSure.Contains(s)) || actionSure.Contains(sourceTokens[i]))
                    ret += (" " + sourceTokens[i]);
                else if (Resource.m_dIdf.ContainsKey(tokens[i]))
                {
                    if (Resource.m_dIdf[tokens[i]] > 5)
                    {
                        ret += " " + sourceTokens[i];
                    }
                }
                else if (stoscore[s] > 0&&(Resource.m_goodDict.Contains(tokens[i]) || Resource.m_goodDict.Contains(sourceTokens[i])))
                    ret += " " + sourceTokens[i];
                i++;
            }
            //Console.WriteLine(ret);
            return ret;
        }
        static void Main()
        {
            PhraseExtractTool pet = new PhraseExtractTool();
            pet.baseDir = @"D:\zhijie\qf2\data\data\";
            //@"D:\zhijie\qf2\data\data\";
            Resource.Load(pet.baseDir);
            pet.loadResource(pet.baseDir);
            QueryFormatter.pet = pet;
            string line = null;
            while (true)
            {
                line = Console.ReadLine();
                Console.WriteLine(pet.removeWords(line));
               //QueryFormatter.m_hTerms.Clear();
                //Console.WriteLine(QueryFormatter.Format(line));
                pet.allSubs(line);
            }
            pet.scanForMI(@"D:\tmp\support_office_com_clean.tsv", "D:\\tmp\\");
            return;
            pet.scanForUniGramBigram("D:\\tmp\\support_office_com_clean.tsv");
            pet.sortByScore();
            Console.WriteLine("finished");
            while (true)
            {
                string a, b;
                a = Console.ReadLine();
                b = Console.ReadLine();
                if (pet.bigramInfo.ContainsKey(a + "&&&" + b))
                {
                    Console.WriteLine(pet.bigramInfo[a + "&" + b]);
                }
            }

        }
    }

}
