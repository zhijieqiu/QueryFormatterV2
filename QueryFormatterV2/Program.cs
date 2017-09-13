using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Web;
using CDSSVectorTagger;
using System.Text.RegularExpressions;

namespace SupportAgent
{
    class Program
    {
        /*
         * 需要一个好的词典
         * to address the problem add bad words to dict, we need to check a the adding words's frequent must be greater than a threshold
         * add a dict which good enongh, in fact we need to check the fil
         * add a entity recognization tool 
         * any word wich is composed of digits not to modify
         * any word with - not to modify 
         * may be the unigram can help
         */
        public static string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "#", "\t", "\n", "/" };
        private static void testSpellingChecker(SpellingChecker sc, string file, string outFile, string changeFile)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                StreamWriter sw = new StreamWriter(outFile);
                StreamWriter changeSw = new StreamWriter(changeFile);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] tokens = line.Split("\t".ToArray());
                    if (line.Contains("&&&&"))
                    {
                        string[] sep = new string[] { "&&&&" };
                        tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    }

                    string outStr = null, changeResult = null;
                    bool isChanged = sc.correct2(tokens[0], true, out outStr, out changeResult);
                    sw.WriteLine(tokens[0] + "-------->\n" + outStr + "\n" + changeResult);
                    if (isChanged)
                    {
                        changeSw.WriteLine(tokens[0] + "-------->\n" + outStr + "\n" + changeResult);
                    }
                }
                sw.Close();
                changeSw.Close();
            }
        }
        static string[] SmartSplit(string sentence)
        {
            //string[] regstrs = { "^([a-zA-Z0-9]([a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])?\\.)+[a-zA-Z]{2,6}$" };
            List<string> strs = new List<string>();
            string cur = "";
            int i = 0;
            string[] segs = sentence.Split(" ".ToArray());
            string[] my_seperator =
                new string[] { ".", ";", "?", "!", ":", "\\", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "#", "\t", "\n", "/" };
            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            foreach (string seg in segs)
            {
                if (seg.Length > 0)
                {
                    char c = seg[seg.Length - 1];
                    string s = "" + c;
                    if (noGoodEnd.Contains(s))
                    {
                        string token = seg.Substring(0, seg.Length - 1);
                        if (token.Length > 0)
                            strs.Add(token);
                    }
                    else
                    {
                        //if(seg.Length>0)
                        strs.Add(seg);
                    }
                }
            }
            return strs.ToArray();
        }
        private static string[] sentenceToWords(string sentence)
        {
            string[] sep = new string[] { " " };
            string[] tokens = sentence.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> fileExtMap = new Dictionary<string, string>();

            List<string> strs = new List<string>();
            string[] my_seperator =
                new string[] { ".", ";", "?", "!", ":", "\\", ",", "(", ")", "|", "[", "]", "{", "}" };

            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            int fileExtIndex = 0, index = 0;
            foreach (string token in tokens)
            {
                if (token.StartsWith(".") && tokens.Length > 2)
                {
                    if (noGoodEnd.Contains(token[token.Length - 1] + ""))
                    {
                        string fileExt = token.Substring(0, token.Length - 1);
                        if (!fileExtMap.ContainsKey(fileExt))
                        {
                            fileExtMap[fileExt] = "$$FILEEXT" + fileExtIndex++;
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                        }
                        else
                        {
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                        }
                    } else
                    {
                        string fileExt = token;
                        if (!fileExtMap.ContainsKey(fileExt))
                        {
                            fileExtMap[fileExt] = "$$FILEEXT" + fileExtIndex++;
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                        } else
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                    }
                }
                index++;
            }
            foreach (string s in tokens)
            {
                if (s.Length == 0) continue;
                if (s.Length == 1)
                {
                    strs.Add(s);
                    continue;
                }
                string begin = s.Substring(0, 1), end = s.Substring(s.Length - 1, 1);
                bool isBegin = noGoodEnd.Contains(begin), isEnd = noGoodEnd.Contains(end);
                if (isBegin && !isEnd)
                {
                    strs.Add(begin);
                    strs.Add(s.Substring(1));
                }
                else if (isBegin && isEnd)
                {
                    strs.Add(begin);
                    strs.Add(s.Substring(1, s.Length - 2));
                    strs.Add(end);
                }
                else if (!isBegin && isEnd)
                {
                    strs.Add(s.Substring(0, s.Length - 1));
                    strs.Add(end);
                }
                else
                    strs.Add(s);
            }
            return strs.ToArray();

        }
        static Dictionary<int, Dictionary<string, double>> queryToLabels = new Dictionary<int, Dictionary<string, double>>();
        private static void loadQueryToLabes(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            while((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split("\t".ToArray());
                int queryId = int.Parse(tokens[0]);
                if (queryToLabels.ContainsKey(queryId) == false)
                {
                    queryToLabels.Add(queryId,new Dictionary<string,double>());
                    
                }
                if (queryToLabels[queryId].ContainsKey(tokens[1]) == false)
                    queryToLabels[queryId].Add(tokens[1], double.Parse(tokens[2]));
            }
            sr.Close();
        }
        static int compare(KeyValuePair<string,double> a,KeyValuePair<string,double> b)
        {
            return -a.Value.CompareTo(b.Value);
        }
        static double NDCGScore(List<Doc> docs,int queryId,int N=1)
        {
            Dictionary<int, double> scoreToGain = new Dictionary<int, double>();
            scoreToGain[5] = 10;
            scoreToGain[4] = 7;
            scoreToGain[3] = 3;
            scoreToGain[2] = 1;
            scoreToGain[1] = 0;
            int i = 0;
            if (queryToLabels.ContainsKey(queryId) == false) return -1;
            Dictionary<string, double> queryMap = queryToLabels[queryId];
            if (N > queryMap.Count())
            {
                Console.WriteLine("N is to big");
                return -1;
            }
            List<KeyValuePair<string, double>> kvs = queryMap.ToList();
            kvs.Sort(compare);
            List<double> goodDCG = new List<double>();
            for(i = 0; i < kvs.Count(); i++)
            {
                if (i == 0)
                {
                    goodDCG.Add(scoreToGain[(int)kvs[i].Value]*(Math.Log(2)/Math.Log(i+1+1)));
                }else
                {
                    goodDCG.Add(goodDCG[goodDCG.Count()-1]+scoreToGain[(int)kvs[i].Value] * (Math.Log(2) / Math.Log(i + 1 + 1)));
                }
            }
            double score = 0.0;
            i = 0;
            for(int j = 0; j < docs.Count()&&i<N; j++)
            {
                if (queryMap.ContainsKey(docs[j].UID) == false) continue;
                score += scoreToGain[(int)queryMap[docs[j].UID]] * (Math.Log(2) / Math.Log(i + 1 + 1));
                i++;
            }
            return score/goodDCG[N-1];
        }
        static string formulate(string str)
        {
            return str;
        }
        static double formulateAndCalculate(string query,int queryId){
            string f_query = formulate(query);
            List<Doc> docs = GetSearchResult(f_query);
            double score = NDCGScore(docs,queryId);
            return score; 
        }
        static PhraseExtractTool pet = new PhraseExtractTool();
        static void Main2(string[] args)
        {
            
            /*while (true)
            {
                string a, b;
                a = Console.ReadLine();
                b = Console.ReadLine();
                if (SpellingChecker.isAdd_or_removeSameLetter(a, b))
                    Console.WriteLine(true);
                else Console.WriteLine(false);
            }*/
            /*while (true)
            {
                string sentence = Console.ReadLine();
                string[] tokens = SmartSplit(sentence);
                for(int i = 0; i < tokens.Length; i++)
                {
                    Console.WriteLine(tokens[i]);
                }
            }*/
            string baseDir = @"\\STCSRV-B11\spellingchecker_traindata\";
            baseDir = @"C:\\work\spellingchecker_data\data\";
            
            /*while (true)
            {
                string line = null;
                line = Console.ReadLine();
                bool isChanged = false;
                string ret = null;
                string changeResult = null;
                isChanged = Resource.m_spellingChecker.correct2(line, true, out ret, out changeResult);
                Console.WriteLine(ret + "\n" + changeResult);
            }
            SpellingChecker sc = new SpellingChecker(baseDir+@"2.txt", baseDir+@"data\");
            testSpellingChecker(Resource.m_spellingChecker, "C:\\work\\question_or_not\\title_all.txt", "C:\\work\\question_or_not\\tmp\\spellingchecker.result6",
                "C:\\work\\question_or_not\\tmp\\change.result6");
            Console.WriteLine("trainFinished");
            Console.ReadLine();
            
            //sc.loadModel("D:\\work\\ngraminfo2\\wordSuffix.txt", "D:\\work\\ngraminfo2\\dict.txt", "C:\\Users\\zhiq\\Downloads\\english-words-master\\english-words-master\\words2.txt");
            sc.train();*/
            pet.baseDir = @"D:\zhijie\qf2\data\data\";
            //Resource.Load(pet.baseDir);
            sentenceClassifier sc = new sentenceClassifier();
            //sc.init();
            /*Console.WriteLine("please input:");
            while (true)
            {
                string line = Console.ReadLine();
                
                Console.WriteLine(score);
            }*/
            pet.loadResource(pet.baseDir);
            QueryFormatter.pet = pet;
            Console.WriteLine("trainFinished");
            //Console.ReadLine();

            Console.WriteLine("main");
            
            string dir =pet.baseDir;
            //ReadTestFile3(dir+ "ConciergeBaseData_query.txt");
            //return;
            Resource.Load(dir);
            
            //testSpellingChecker(Resource.m_spellingChecker, "C:\\work\\question_or_not\\title_all.txt", "C:\\work\\question_or_not\\tmp\\spellingchecker.result",
                //"C:\\work\\question_or_not\\tmp\\change.result");
            //Console.ReadLine();
            string testFile = dir + "query.tsv";
            string mytestFile = "D:\\work\\a.txt";
            string jackTestFile = dir + "spellingchecker_test_file.txt";
            string simpleOutputFile = dir + "output/simple_output.txt";
            string formatterOutputFile = dir + "output/formatter_output_MI_keysent.txt";
            string spellingOutputFile = dir + "spellingChecker.txt";
            
            List<string> inputs = ReadTestFile(testFile);
            
            //TestSimple(inputs, simpleOutputFile);
            //for (double x = 2; x <= 4.5; x += 0.5 )
            //{
            //    for(double y = 5; y <= 10; y += 0.5)
            //    {
            //        QueryFormatter.idfThresholdLow = x;
            //        QueryFormatter.idfThresholdHigh = y;
            //        string formatterOutputFile2 = formatterOutputFile + "." + x + "-" + y;
            //        TestFormatter(inputs, formatterOutputFile2);
            //    }
            //}
            //spellingCheckerTest(inputs,spellingOutputFile);
            //Console.ReadLine();
           TestFormatter(inputs, formatterOutputFile);
           Console.ReadLine();
        }

        public static string SimpleFormat(string input)
        {
            input = Resource.m_spellingChecker.correct(input, true);
            List<string> ret = new List<string>();
            HashSet<string> hTerms = new HashSet<string>();
            Stemmer stem = new Stemmer();
            input = stem.GetBaseFormSentence(input.ToLower());
            List<string> words = input.Split().ToList();
            int ngram = 4;
            for (int i = 0; i < words.Count; )
            {
                int k;
                for (k = ngram; k > 0; k--)
                {
                    if (i + k > words.Count)
                        continue;
                    string phrase = string.Join(" ", words.Skip(i).Take(k));
                    if (Resource.m_hTaxonomyKeys.Contains(phrase))
                    {
                        if (!hTerms.Contains(phrase))
                        {
                            hTerms.Add(phrase);
                            ret.Add(phrase);
                        }
                        break;
                    }
                }
                i += Math.Max(k, 1);
            }
            return string.Join(" ", ret);
        }

        public static List<string> ReadTestFile(string file)
        {
            List<string> ret = new List<string>();
            using(StreamReader sr = new StreamReader(file))
            {
                string line;
                while((line = sr.ReadLine()) != null)
                {
                    string[] seperator = new string[] { "###" };
                    string[] sents = line.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                    ret.Add(sents[0]);
                }
            }
            return ret;
        }
        public static List<string> ReadTestFile2(string file)
        {
            List<string> ret = new List<string>();
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    ret.Add(line);
                }
            }
            return ret;
        }
        public static void ReadTestFile3(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                StreamWriter sw = new StreamWriter("D:\\work\\queryForLabel.txt");
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] seperator = new string[] { "###" };
                    string[] sents = line.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                    if (sents.Length > 0)
                        sw.WriteLine(sents[0]);
                }
            }
        }
        public static void TestSimple(List<string> inputs, string file)
        {
            float totalScore = 0;
            float totalScore1 = 0;
            float totalScore2 = 0;
            int count = 0;
            using(StreamWriter sw = new StreamWriter(file))
            {
                foreach(string input in inputs)
                {
                    count++;
                    string query = SimpleFormat(input);
                    float score = 0;
                    float score1 = 0;
                    float score2 = 0;
                    if (query.Length > 0)
                    {
                        score1 = GetSearchEngineScore(query);
                        score2 = GetSimScore(input, query);
                        if (score1 > 0 && score2 > 0)
                        {
                            score = score1 * score2 / (score1 + score2);
                        }
                    }
                    sw.Write(input + "\t" + query + "\t" + score + "\r");
                    totalScore += score;
                    totalScore1 += score1;
                    totalScore2 += score2;
                }
                sw.WriteLine("Average Score: " + totalScore / count + " " + totalScore1/count + " " + totalScore2 / count);
            }
        }
        public static void spellingCheckerTest(List<string> inputs,string file)
        {
            using (StreamWriter sw = new StreamWriter(file))
            {
                foreach (string input in inputs)
                {
                    string spellingInput = Resource.m_spellingChecker.correct(input, true);
                    Console.WriteLine(input + "-->" + spellingInput);
                    //input = spellingInput;
                    sw.WriteLine(input + "-->" + spellingInput);
                }
            }
        }
        private static bool isGoodQuery(int queryId)
        {
            if (queryToLabels.ContainsKey(queryId) == false) return false;
            foreach(var kv in queryToLabels[queryId])
            {
                if (kv.Value >= 4.0) return true;
            }
            return false;
        }
        public static void TestFormatter2(List<string> inputs,string file)
        {
            loadQueryToLabes(@"D:\zhijie\qf2\data\data\rates.tsv");
            double totalScore = 0;
            int count = 0;
            using (StreamWriter sw = new StreamWriter(file))
            {
                //int i = 1;
                foreach (string input in inputs)
                {
                    //string[] tokens = input.Split("\t".ToArray());
                    //int qid = int.Parse(tokens[0]);
                   // if (!isGoodQuery(qid)) continue;
                    //input = Resource.m_spellingChecker.correct(input);
                    string[] tokens = input.Split("\t".ToArray());
                    int qid = int.Parse(tokens[0]);
                    if (!isGoodQuery(qid)) continue;
                    count++;

                    //string query = QueryFormatter.Format(input);
                    string query = pet.removeWords(tokens[1]);
                    //List<Doc> docs = GetSearchResult(query);
                    // double score = NDCGScore(docs, qid);
                    /* string[] querySeg = new string[]{ "&&&&" };
                     string[] inputTokens = input.Split(querySeg, StringSplitOptions.RemoveEmptyEntries);
                     if (inputTokens.Length < 2) continue;
                     string query = inputTokens[1];
                     Console.WriteLine(query);*/
                    float score = 0;

                    if (query.Length > 0)
                     {
                         float score1 = GetSearchEngineScore(query);
                         float score2 = GetSimScore(input, query);
                         if (score1 > 0 && score2 > 0)
                         {
                             score = score1 * score2 / (score1 + score2);
                         }
                     }
                    sw.Write(input + "\t" + query + "\t" + score + "\r");
                   /* foreach (string uid in queryToLabels[qid].Keys)
                    {
                        sw.Write(uid + ":" + queryToLabels[qid][uid] + "\t");
                    }
                    sw.WriteLine();
                    foreach (Doc doc in docs)
                    {
                        sw.WriteLine(doc.UID);
                    }*/
                    totalScore += score;
                }
                sw.WriteLine("Average Score: " + totalScore / count);
            }
        }
        public static void TestFormatter(List<string> inputs, string file)
        {
            loadQueryToLabes(@"D:\zhijie\qf2\data\data\rates.tsv");
            double totalScore = 0;
            int count = 0;
            using (StreamWriter sw = new StreamWriter(file))
            {
                //int i = 1;
                foreach (string input in inputs)
                {
                    string[] tokens = input.Split("\t".ToArray());
                    int qid = int.Parse(tokens[0]);
                    if (!isGoodQuery(qid)) continue;
                    //input = Resource.m_spellingChecker.correct(input);
                    count++;

                    //string query = QueryFormatter.Format(tokens[1]);
                    //string query = pet.removeWords(tokens[1]);
                   // string query = QueryFormatter.FormatKeySentence(tokens[1]);
                    //string query = QueryFormatter.FormatNochange(tokens[1]);
                   // string query = QueryFormatter.Format3(tokens[1]);
                    string query = pet.keySentence(tokens[1]);
                    List<Doc> docs = GetSearchResult(query);
                    double score = NDCGScore(docs,qid);
                    /*string[] querySeg = new string[]{ "&&&&" };
                    string[] inputTokens = input.Split(querySeg, StringSplitOptions.RemoveEmptyEntries);
                    if (inputTokens.Length < 2) continue;
                    string query = inputTokens[1];
                    Console.WriteLine(query);*/
                    
                   /* if (query.Length > 0)
                    {
                        float score1 = GetSearchEngineScore(query);
                        float score2 = GetSimScore(input, query);
                        if (score1 > 0 && score2 > 0)
                        {
                            score = score1 * score2 / (score1 + score2);
                        }
                    }*/
                    sw.Write(input + "\t" + query + "\t" + score + "\r");
                    foreach(string uid in queryToLabels[qid].Keys)
                    {
                        sw.Write(uid+":"+queryToLabels[qid][uid]+"\t");
                    }
                    sw.WriteLine();
                    foreach(Doc doc in docs)
                    {
                        sw.WriteLine(doc.UID);
                    }
                    totalScore += score;
                }
                sw.WriteLine("Average Score: " + totalScore / count);
            }
        }
        private static List<Doc> GetSearchResult(string query)
        {
            int topX = 1;
            double w1 = 0.7;
            double w2 = 0.2;
            double w3 = 0.1;
            double t1 = 100;
            //string apiUrl = string.Format("http://13.90.251.141:8984/api/search/?q={0}&t1={1}&t2={2}&w1={3}&w2={4}&w3={5}",
            //query, t1, topX, w1, w2, w3);
            string uri = "http://13.90.251.141:8998/api/official";
            string apiUrl = string.Format(uri+"?q={0}", query);

            //query, t1, topX, w1, w2, w3);
            string plainJson = "";
            try
            {
                HttpClient client = new HttpClient();
                plainJson = client.GetStringAsync(apiUrl).Result;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Bing search failure: {0}", e.Message);
                return new List<Doc>();
            }

            if (!string.IsNullOrEmpty(plainJson))
            {
                // parse jason results to extract tuple <name, url, snippet>
                SearchEngineResponse result = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<SearchEngineResponse>(plainJson);
                return result.docs;
            }
            return new List<Doc>() ;
        }
        private static float GetSearchEngineScore(string query)
        {
            int topX = 1;
            double w1 = 0.7;
            double w2 = 0.2;
            double w3 = 0.1;
            double t1 = 100;
            //string apiUrl = string.Format("http://13.90.251.141:8984/api/search/?q={0}&t1={1}&t2={2}&w1={3}&w2={4}&w3={5}",
            //query, t1, topX, w1, w2, w3);
            string apiUrl = string.Format("http://13.90.251.141:8998/api/official/?q={0}",query);
            //query, t1, topX, w1, w2, w3);
            string plainJson = "";
            try
            {
                HttpClient client = new HttpClient();
                plainJson = client.GetStringAsync(apiUrl).Result;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Bing search failure: {0}", e.Message);
                return 0;
            }

            if (!string.IsNullOrEmpty(plainJson))
            {
                // parse jason results to extract tuple <name, url, snippet>
                SearchEngineResponse result = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<SearchEngineResponse>(plainJson);
                if (result.docs.Count > 0)
                    return result.docs[0].score;
            }
            return 0;
        }

        private static float GetSimScore(string input, string query)
        {
            float[] v1 = Resource.m_vwCDSSMVector.GetVector(input, Resource.m_vwCDSSMVector.vocab_list);
            float[] v2 = Resource.m_vwCDSSMVector.GetVector(query, Resource.m_vwCDSSMVector.vocab_list);
            return Similarity(v1, v2, input, query);
        }

        private static float Similarity(float[] query1, float[] query2, string input, string query)
        {
            double similarity = 0.0f;
            float a = 0.0f, b = 0.0f, c = 0.0f;

            for (int i = 0; i < query1.Length; ++i)
            {
                a += query1[i] * query2[i];
                b += query1[i] * query1[i];
                c += query2[i] * query2[i];
            }
            similarity = a / (Math.Sqrt(b) * Math.Sqrt(c));
            return (float)similarity;
        }
    }
}
