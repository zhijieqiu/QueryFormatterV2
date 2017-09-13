using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using java.io;
using edu.stanford.nlp.process;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.parser.lexparser;
using Console = System.Console;
using ICEClassifierLocalForTheme;
using System.Text.RegularExpressions;
using System.IO;

namespace SupportAgent
{
    class sentenceClassifier
    {
       
        sentenceComplete sc;
        HashSet<string> Target = new HashSet<string>();
        HashSet<string> Actions = new HashSet<string>();
        HashSet<string> productList2 = new HashSet<string>();
        HashSet<string> sureList = new HashSet<string>();
        HashSet<string> badList = new HashSet<string>();
        feature fea;
        
        Tree tree;
        //public static string basedir = null;
        public sentenceClassifier()
        {
            init();
        }
        public void init()
        {
            //ICEClassifierLocalForTheme.LocalClassifier.Init(path);
            
           //sc.init();
            Target = Resource.m_Target;
            //fea = Resource.m_fea;
            Actions = Resource.m_Actions;
            productList2 = Resource.m_productList2;
            sureList = Resource.m_sureList;
            badList = Resource.m_badList;
            //sc = Resource.m_sc;
        }
        public void init(string dir)
        {
            Resource.Load(dir + "data\\");
            string path = dir+ @"data\package";
            string actionfile = dir + @"data\action2.txt";
            string proPath = dir + @"data\product_list.txt";
            string sureFile = dir + @"data\sureList.txt";
            string[] alines = System.IO.File.ReadAllLines(actionfile);
            foreach (string line in alines)
            {
                if (!Actions.Contains(line.ToLower())) Actions.Add(line.ToLower());
            }
            alines = System.IO.File.ReadAllLines(proPath);
            foreach (string line in alines)
            {
                if (productList2.Contains(line.ToLower()) == false)
                    productList2.Add(line.ToLower());
            }
            alines = System.IO.File.ReadAllLines(sureFile);
            foreach(string line in alines)
            {
                if (line.StartsWith("\t"))
                {
                    string t = line.Substring(1);
                    string[] tokens = t.Split(":".ToArray());
                    if (sureList.Contains(tokens[0]) == false)
                        sureList.Add(tokens[0].ToLower());

                }else
                {
                    string[] tokens = line.Split(":".ToArray());
                    double prop = double.Parse(tokens[1]);
                    if (prop >= 0.9&&badList.Contains(tokens[0])==false)
                    {
                        badList.Add(tokens[0].ToLower());
                    }
                }
            }
            ICEClassifierLocalForTheme.LocalClassifier.Init(path);
            sc = new sentenceComplete();
            sc.init(dir);
            Target = sc.Target;
            fea = new feature();
            fea.init();

        }

        public void parse_query(string query)
        {
            tree = sc.parsequery(query);
        }
        public List<int> predictProduct(string query)
        {
            string result = ICEClassifierLocalForTheme.LocalClassifier.Classify(query, 5, 0.01);
            string[] parts = result.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<int> ls = new List<int>();
            foreach (string part in parts)
            {
                string[] newpart = part.Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                double score = double.Parse(newpart[1]);
                if (score > 0.70) ls.Add(int.Parse(newpart[0]));
            }

            return ls;
        }
        //void LoadPhrase()
        //{
        //    string phrasePath = @"data\refer\phrase.tsv";
        //    string[] lines = System.IO.File.ReadAllLines(phrasePath);
        //    foreach (string line in lines)
        //    {
        //        string[] parts = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
        //        if (parts.Length > 0)
        //            Phrase.Add(parts[0]);
        //    }
        //}
        int completeoldtax(string query)
        {
            List<string> newq = new List<string>();
            newq.Add(query);
            int result = fea.oldfea(newq);
            return result;
        }
        int ifcontainsProduct(string query)
        {
            string result = ICEClassifierLocalForTheme.LocalClassifier.Classify(query, 5, 0.01);
            string[] parts = result.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<double> ls = new List<double>();
            foreach (string part in parts)
            {
                string[] newpart = part.Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if(int.Parse(newpart[0])!=5)
                    ls.Add(double.Parse(newpart[1]));
            }
            int count = 0;
            
            foreach (double temp in ls)
            {
                if (temp > 0.9) count++;
            }
            return count;
        }
        double maxproductScore(string query)
        {
           
            double ret = -1.0;
            //bool hasProduct = false;
            query = query.ToLower();
            string[] queryWords = query.Split(" ".ToArray());
            string[] senSeperator = new string[] { ".", ";", "?", "!", ":", "," };
            HashSet<char> chars = new HashSet<char>();
            foreach(string s in senSeperator)
            {
                chars.Add(s[0]);
            }
            int i = 0;
            List<string> reserveList = new List<string>();
            foreach (string word in queryWords)
            {
                if (word.Length > 0 && chars.Contains(word[word.Length - 1]))
                    queryWords[i] = word.Substring(0,word.Length-1);
                string tw = queryWords[i].ToLower() ;
                if (badList.Contains(tw) == false && badList.Contains(stemmer.GetBaseFormWord(tw)) == false)
                    reserveList.Add(tw);
                if (productList2.Contains(stemmer.GetBaseFormWord(tw.ToLower())))
                {
                    ret = 0.94;
                    //break;
                }
                i++;
            }
            if (reserveList.Count() == 0) return -1;
            query = string.Join(" ", reserveList);
            string result = ICEClassifierLocalForTheme.LocalClassifier.Classify(query, 5, 0.01);
            string[] parts = result.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<double> ls = new List<double>();
            foreach (string part in parts)
            {
                string[] newpart = part.Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (int.Parse(newpart[0]) == 5) continue;
                else
                {
                    ret =Math.Max(double.Parse(newpart[1]),ret);
                    break;
                } 
            }
            /*if (queryWords.Length <= 4)
            {
                bool flag = true;
                foreach(string s in queryWords)
                {
                    if (productList2.Contains(s) == false && sureList.Contains(s) == false) continue;
                    else
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag) ret -= 0.06;
            }*/
           
            return ret;
        }
        static string formulate(string str)
        {
            string ret = "";
            for(int i = 1; i < str.Length - 1; i++)
            {
                if (str[i] != ' ') ret += str[i];
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /*static void numbervp2(string fileName)
        {
            var jarRoot = @"data\stanford-english-corenlp-2016-01-10-models\";
            var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";

            // Loading english PCFG parser from file
            var lp = LexicalizedParser.loadModel(modelsDirectory + @"\lexparser\englishRNN.ser.gz");
            var sent2 = "Enable audience-based targeting in a SharePoint list or library";
            var tokenizerFactory = PTBTokenizer.factory(new CoreLabelTokenFactory(), "");
            StreamWriter sw = new StreamWriter("d:\\work\\question_or_not\\myaction.txt");
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line = null;
                int cnt = 0;
                HashSet<string> actions = new HashSet<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    var sent2Reader = new java.io.StringReader(line);
                    var rawWords2 = tokenizerFactory.getTokenizer(sent2Reader).tokenize();
                    sent2Reader.close();
                    var tree = lp.apply(rawWords2);
                    string stree = tree.toString();

                    Regex reg = new Regex("\\([a-z ]+\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matches = reg.Matches(stree);
                    foreach (Match match in matches)
                    {
                        //Console.WriteLine("value：|{0}", match.Value);
                        //Console.WriteLine(match.Value.Substring(1, match.Value.Length - 2));
                        string[] tokens = match.Value.Substring(1,match.Value.Length-2).Split(' ');


                        //Console.WriteLine(match.Value);
                        if (tokens.Length >= 2 && tokens[0].StartsWith("VB"))
                        {
                            if (actions.Contains(tokens[1].ToLower()) == false)
                            {
                                sw.WriteLine(tokens[1]);
                                actions.Add(tokens[1]);
                                cnt++;
                                if (cnt % 100 == 0) Console.WriteLine(cnt);
                            }
                            
                        }
                    }
                    
                }
                sw.Close();
            }
                //Console.WriteLine(stree);
            //int count = stree.Split(new string[] { "VP" }, StringSplitOptions.None).Length - 1;
            
        }*/
        float Score(string query)
        {
            List<string> lis = new List<string>();
            query = query.Replace(" ", "+");
            //           string link = @"http://stcsrv-e57:8083/api/?q=" + query + "&w=" + strW + "&t=" + topK;
            string link = @"http://13.90.251.141:8984/api/official?q="+ query;
            //           string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=10000&z=1&f=1&idf=1";
            // string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=1000&idf=true&alpha="+alpha+"&z=true&f=false";
            //string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=10000&idf=true&alpha=" + alpha + "&z=true";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link); //
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            System.IO.Stream ResStream = response.GetResponseStream();
            System.IO.StreamReader streamReader = new System.IO.StreamReader(ResStream, Encoding.Default);

            string res = streamReader.ReadToEnd();

            JavaScriptObject json = (JavaScriptObject)JavaScriptConvert.DeserializeObject(res);
            List<string> temp = new List<string>();
            float result = 0.0f;
            try {
                if (json.ContainsKey("docs") == false) return result ;
                JavaScriptArray docs = (JavaScriptArray)json["docs"];
                if (docs.Count >= 1)
                {
                    JavaScriptObject doc = (JavaScriptObject)docs[0];
                    result = float.Parse(doc["score"].ToString());
                }
            }
            catch (KeyNotFoundException kne)
            {

            }
            catch(Exception e)
            {

            }

            return result;
        }
        float Score2(string query)
        {
            List<string> lis = new List<string>();
            float result = 0.0f;
            try
            {
                query = query.Replace(" ", "+");
                //           string link = @"http://stcsrv-e57:8083/api/?q=" + query + "&w=" + strW + "&t=" + topK;
                string link = @"http://stcsrv-e57:8991/api/official/?q=" + query;
                //           string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=10000&z=1&f=1&idf=1";
                // string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=1000&idf=true&alpha="+alpha+"&z=true&f=false";
                //string link = @"http://stcsrv-e57:8984/api/search/?q=" + query + "&t1=1000&t2=10000&idf=true&alpha=" + alpha + "&z=true";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link); //
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                System.IO.Stream ResStream = response.GetResponseStream();
                System.IO.StreamReader streamReader = new System.IO.StreamReader(ResStream, Encoding.Default);

                string res = streamReader.ReadToEnd();

                JavaScriptObject json = (JavaScriptObject)JavaScriptConvert.DeserializeObject(res);
                List<string> temp = new List<string>();
               
                if (json.ContainsKey("docs") == false) return result;
                JavaScriptArray docs = (JavaScriptArray)json["docs"];
                //JavaScriptArray docs = (JavaScriptArray)json["docs"];
                if (docs.Count >= 2)
                {
                    JavaScriptObject doc = (JavaScriptObject)docs[0];
                    JavaScriptObject doc1 = (JavaScriptObject)docs[1];
                    
                    result = (float.Parse(doc["score"].ToString()) + float.Parse(doc1["score"].ToString())) /2.0f; 
                }
            }
            catch (KeyNotFoundException kne)
            {

            }
            catch (Exception e)
            {

            }

            return result;
        }
        int isgoodLength(string query)
        {
            string[] parts = query.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            return parts.Length;
        }
        Tuple<int, int> numberActionAndTarget(string query)
        {

            int acount = 0;
            int tcount = 0;
            query = query.ToLower();
            List<string> actions = new List<string>();
            List<string> targets = new List<string>();
            string[] words = query.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                HashSet<string> addStrs = new HashSet<string>();
                for (int k = 0; k < 3; k++)
                {
                    string ph = words[i];
                    
                    for (int j = 1; j <= k && i + j < words.Length; j++)
                    {
                        ph += "_" + words[i + j];
                    }

                    if (!addStrs.Contains(ph.ToLower()) && Actions.Contains(ph.ToLower()))
                    {
                        actions.Add(ph.ToLower());
                        acount++;
                    }
                    if (!addStrs.Contains(ph.ToLower()) && Target.Contains(ph.ToLower()))
                    {
                        targets.Add(ph.ToLower());
                        tcount++;
                    }
                    addStrs.Add(ph.ToLower());
                }
            }
            foreach(string s in actions)
            {
                Console.WriteLine(s);
            }
            Console.WriteLine("_________");
            foreach (string s in targets)
            {
                Console.WriteLine(s);
            }
            Tuple<int, int> tup = new Tuple<int, int>(acount, tcount);
            return tup;
        }
        Tuple<List<string>,List<string>> actionsAndTargets(string query)
        {
            int acount = 0;
            int tcount = 0;
            query = query.ToLower();
            string[] words = query.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<string> actions = new List<string>();
            List<string> targets = new List<string>();
            for (int i = 0; i < words.Length; i++)
            {
                HashSet<string> addStrs = new HashSet<string>();
                for (int k = 0; k < 3; k++)
                {
                    string ph = words[i];
                    for (int j = 1; j <= k && i + j < words.Length; j++)
                    {
                        ph += "_" + words[i + j];
                    }
                    if (!addStrs.Contains(ph.ToLower()) && Actions.Contains(ph.ToLower()))
                        actions.Add(ph);
                    if (!addStrs.Contains(ph.ToLower()) &&Target.Contains(ph.ToLower()))
                        targets.Add(ph);

                    addStrs.Add(ph.ToLower());
                }
            }
            Tuple<List<string>, List<string>> tup = new Tuple<List<string>, List<string>>(actions, targets);
            return tup;
        }
        public Tuple<int, int> numbervp()
        {
            return sc.numbervp(tree);
        }
        int iscomplete()
        {
            return sc.ifcomplete(tree);
        }
        public List<double> logisticFeature(string query)
        {
            List<double> feaList = new List<double>();
            query = query.ToLower();
            //parse_query(query);
           
            double wordlength = (double)isgoodLength(query);
            feaList.Add(wordlength);
            double productMaxScore = (double)maxproductScore(query);
            feaList.Add(productMaxScore);
            Tuple<int, int> tup = numberActionAndTarget(query);
            double numberaction = (double)tup.Item1;
            feaList.Add(numberaction);
            double numbertarget = (double)tup.Item2;
            feaList.Add(numbertarget);
            return feaList;
        }

        public Dictionary<int, double> allfeature(string query)
        {
            Dictionary<int, double> dic = new Dictionary<int, double>();
            query = query.ToLower();
            //double productnum = (double)ifcontainsProduct(query);
            //dic.Add(0, productnum);
            //double numvp = (double)numbervp(query);
            //dic.Add(1, numvp);
            //double comp = (double)completeoldtax(query);
            //dic.Add(2, comp);
            //Tuple<int, int> tup = numberActionAndTarget(query);
            //double numberaction = (double)tup.Item1;
            //dic.Add(3, numberaction);
            //double numbertarget = (double)tup.Item2;
            //dic.Add(4, numberaction);
            //return dic;
            //double productnum = (double)ifcontainsProduct(query);
            //dic.Add(0, productnum);
            //double numvp = (double)numbervp(query);
            //dic.Add(1, numvp);
            //double comp = (double)completeoldtax(query);
            //dic.Add(2, comp);
            //double sentencecomp = (double)iscomplete(query);
            //dic.Add(3, sentencecomp);
            //Tuple<int, int> tup = numberActionAndTarget(query);
            //double numberaction = (double)tup.Item1;
            //dic.Add(4, numberaction);
            //double numbertarget = (double)tup.Item2;
            //dic.Add(5, numberaction);
            //return dic;
            //score wordLength productnum vp_count taxLeaf sentencecomplete numberaction numbertarget  vp_target_action
            parse_query(query);
            double score = Score(query);
            dic.Add(0, score);
            double wordlength = (double)isgoodLength(query);
            dic.Add(1, wordlength);
            double productMaxScore = (double)maxproductScore(query);
            dic.Add(2, productMaxScore);
            double productNum = (double)ifcontainsProduct(query);
           
            Tuple<int, int> tup_vp = numbervp();

            dic.Add(3, (double)tup_vp.Item1);
            double comp = (double)completeoldtax(query);
            dic.Add(4, comp);
            double sentencecomp = (double)iscomplete();
            dic.Add(5, sentencecomp);
            Tuple<int, int> tup = numberActionAndTarget(query);
            double numberaction = (double)tup.Item1;
            dic.Add(6, numberaction);
            double numbertarget = (double)tup.Item2;
            dic.Add(7, numbertarget);
            dic.Add(8, (double)tup_vp.Item2);
            dic.Add(9,(double)productNum);
            return dic;
        }


        //public string GetBaseFormSentence(string sentence)
        //{
        //    string baseSentence = "";
        //    string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`" };
        //    string[] words = sentence.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
        //    for (int i = 0; i < words.Length; i++)
        //    {
        //        string word = words[i].Trim();
        //        if (word.Length == 0) continue;


        //        if (i == 0) baseSentence = word;
        //        else baseSentence += " " + word;
        //    }

        //    return baseSentence;
        //}
       // can't install update because I can't close two apps
        void productList(string tt)
        {
            string result = ICEClassifierLocalForTheme.LocalClassifier.Classify(tt, 5, 0.01);
            string[] parts = result.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<double> ls = new List<double>();
            foreach (string part in parts)
            {
                //string[] newpart = part.Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine(part);
            }
        }
        void generateAllTargetAndActionInfo(string fileName,string outFileName, sentenceClassifier sc)
        {
            StreamReader sr = new StreamReader(fileName);
            StreamWriter sw = new StreamWriter(outFileName);
            int index = 0;
            string line = null;
            while ((line = sr.ReadLine())!=null)
            {
                Tuple<List<string>, List<string>> ret = sc.actionsAndTargets(line);
                int i = 0;
                foreach(string s in ret.Item1)
                {
                    if (i == 0)
                        sw.Write(s);
                    else sw.Write(" "+s);
                    i++;
                }
                if (i == 0) sw.Write("#");
                sw.Write("\t");
                i = 0;
                foreach (string s in ret.Item2)
                {
                    if (i == 0)
                        sw.Write(s);
                    else sw.Write(" " + s);
                    i++;
                }
                if (i == 0) sw.Write("#");
                sw.Write("\n");
            }
            sw.Close();
            sr.Close();

        }
        static void outLookFile(sentenceClassifier sc)
        {
            string targetFile = "D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\data\\target.txt";
            string actionFile = "D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\data\\action.txt";
            string actionScore = "D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\data\\actionScore.txt";
            string targetScore = "D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\data\\targetScore.txt";
            string[] tLines = System.IO.File.ReadAllLines(targetFile);
            string[] aLines = System.IO.File.ReadAllLines(actionFile);
            StreamWriter sw = new StreamWriter(actionScore);
            foreach (string line in aLines)
            {
                double score = sc.Score2(line);
                sw.WriteLine(line + "\t" + score);
            }
            StreamWriter sw2 = new StreamWriter(targetScore);
            foreach (string line in tLines)
            {
                double score =  sc.Score2(line);
                sw2.WriteLine(line+"\t"+score);
            }
           
            sw.Close();
            sw2.Close();
        }
        static int compare3(KeyValuePair<string,double> a, KeyValuePair<string, double> ba)
        {
            return a.Value.CompareTo(ba.Value);
        }
        static void myScoreFile(string fileName, sentenceClassifier sc)
        {
            Console.WriteLine("start");
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            Dictionary<string, double> strToScore = new Dictionary<string, double>();
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(":".ToArray());
                if (strToScore.ContainsKey(tokens[0]) == false)
                {
                    strToScore.Add(tokens[0],sc.maxproductScore(tokens[0]));
                }
            }
            List<KeyValuePair<string, double>> allKVs = strToScore.ToList();
            allKVs.Sort(compare3);
            
            sr.Close();
            StreamWriter sw = new StreamWriter("D:\\work\\data\\qornot\\tosee.txt");
            foreach (KeyValuePair<string, double> kv in allKVs)
            {
                sw.Write(kv.Key+":"+kv.Value+"\n");
            }
            sw.Close();
            
        }
        private logist logi = new logist();
        public bool isGoodQuestion(string query)
        {
            query = query.Trim();
            query = query.Replace('\r', ' ').Replace('\n', ' ');
            List<double> features = logisticFeature(query);
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };

            if (features[1] < 0.90)
            {
                Console.WriteLine("product information is not sure");
                return (features[1]-0.3)>0.9;
            }
            double score = logi.cal_sim(features);
            string[] tokens = query.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
            bool flag = false;
            foreach (string token in tokens)
            {
                if (Actions.Contains(token.ToLower()) || Actions.Contains(stemmer.GetBaseFormWord(token.ToLower())))
                {
                    Console.WriteLine(token.ToLower());
                    flag = true;
                    break;
                }
            }
            for(int i = 0; i < tokens.Length - 1; i++)
            {
                string curStr = tokens[i].ToLower() + " " + tokens[i + 1].ToLower();
                string curStr2 = stemmer.GetBaseFormWord(tokens[i]).ToLower() + " " + stemmer.GetBaseFormWord(tokens[i + 1]).ToLower();
                if (Actions.Contains(curStr) || Actions.Contains(curStr2))
                {
                    Console.WriteLine(curStr.ToLower());
                    flag = true;
                }
            }
            if (!flag) score -= 0.2;
            //Console.WriteLine("final score is " + score);
            if (score > 0.9) return true;
            else return false;
        }
        public double queryScore(string query)
        {
            query = query.Trim();
            query = query.Replace('\r', ' ').Replace('\n',' ');
            List<double> features = logisticFeature(query);
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };

            if (features[1] < 0.90)
            {
                //Console.WriteLine("product information is not sure");
                //features[1] -= 0.3;
            }
            features[0] /= 10.0;
            double score = logi.cal_sim(features);
            string[] tokens = query.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
            bool flag = false;
            foreach (string token in tokens)
            {
                if (Actions.Contains(token.ToLower()) || Actions.Contains(stemmer.GetBaseFormWord(token.ToLower())))
                {
                    flag = true;
                    break;
                }
            }
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                string curStr = tokens[i].ToLower() + " " + tokens[i + 1].ToLower();
                string curStr2 = stemmer.GetBaseFormWord(tokens[i]).ToLower() + " " + stemmer.GetBaseFormWord(tokens[i + 1]).ToLower();
                if (Actions.Contains(curStr) || Actions.Contains(curStr2))
                    flag = true;
            }
            if (!flag) score -= 0.1;
            if (features[1] < 0.9) score -= 0.3;
            //Console.WriteLine("final score is " + score);
            return score;
        }
        public double getScore(string query)
        {
            //Console.WriteLine(query);
            List<double> features = logisticFeature(query);
            string[] innerseperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
           
            if (features[1] < 0.90)
            {
                Console.WriteLine("product information is not sure");
                return -1;
            }
            
            foreach(double f in features)
            {
                Console.Write(f+"\t");
            }
            
            Console.WriteLine();
            double score = logi.cal_sim(features);
            string[] tokens = query.Split(innerseperator, StringSplitOptions.RemoveEmptyEntries);
            bool flag = false;
            foreach (string token in tokens)
            {
                if (Actions.Contains(token.ToLower())||Actions.Contains(stemmer.GetBaseFormWord(token.ToLower())))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag) score -= 0.2;
            Console.WriteLine("final score is "+score);
            if (score > 0.9) return 1;
            else return -1;
        }
        static string basedir = "D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\";
        static Stemmer stemmer = new Stemmer();
        static void removeDup()
        {
            string fileName = @"D:\work\question_or_not\labelData.new\labelData.new\labelData\labelData\bin\Debug\qc\data\target.txt";
            string fileName2 = @"D:\work\question_or_not\labelData.new\labelData.new\labelData\labelData\bin\Debug\qc\data\targets_nod.txt";
            HashSet<string> strs = new HashSet<string>();
            StreamReader sr = new StreamReader(fileName);
            StreamWriter sw = new StreamWriter(fileName2);
            string line = null;
            while((line = sr.ReadLine())!=null)
            {
                if (strs.Contains(line.ToLower()) == false)
                {
                    strs.Add(line.ToLower());
                    sw.WriteLine(line.ToLower());
                }
            }
            sw.Close();
            sr.Close();
        }
        static void test()
        {
            removeDup();
            Console.WriteLine("finshed");
            Console.ReadLine();
            Resource.Load(basedir);
            sentenceClassifier sc = new sentenceClassifier();
            sc.init();
            while (true)
            {
                string line = Console.ReadLine();
                if (sc.isGoodQuestion(line))
                {
                    Console.WriteLine("good no question");
                }
                else
                {
                    Console.WriteLine("no good, need question");
                }
            }
            sc.init("D:\\work\\question_or_not\\labelData.new\\labelData.new\\labelData\\labelData\\bin\\Debug\\");
            while (true)
            {
                string line = Console.ReadLine();
                
                sc.parse_query(line);
                Console.WriteLine(sc.tree.toString());
                //sc.productList(line);
                //sc.productList(line);
                //Console.WriteLine(sc.tree.toString());
               
            }
            
            //myScoreFile("D:\\work\\data\\qornot\\scoreFile.txt",sc);
            //sc.generateAllTargetAndActionInfo("D:\\tmp\\tmp.txt","D:\\tmp\\a_t_result.txt",sc);
            Console.WriteLine("finished");
           // Console.ReadLine();
           // outLookFile(sc);
            Console.WriteLine("finished");
           // Console.ReadLine();
            
            string basepath = @"data\test\";
            string[] files = new string[] { "neg.manually2.txt", "neg.manually.label_5.txt", "pos.nodup2.txt", "pos.manually.label_5.txt", "testFile.txt", "testFeature.txt" };
            string[] afiles = new string[] { "trainForUse.txt","trainFeatures.txt"};
            //string[] files = new string[] { "testFile.txt", "testFeature.txt" };\
            for (int k = 0; k < afiles.Length; k += 2)
            {
                
                string output = basepath + afiles[k + 1];
                string fullpath = basepath + afiles[k];
                string[] lines = System.IO.File.ReadAllLines(fullpath);
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(output))
                {
                    int index = 0;
                    foreach (string line in lines)
                    {
                        try
                        {
                            Console.WriteLine(line);
                                string[] tokens = line.Split('\t');
                                List<double> dic = sc.logisticFeature(tokens[1]);
                                string res = line + "\t" + tokens[0];
                                foreach (var fea in dic)
                                {
                                    res += "\t" + fea.ToString();
                                }
                                res = res.Trim();
                                sw.WriteLine(res);
                            

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        Console.WriteLine(index);
                        index++;
                    }

                }
            }
            return;
            bool isTestFile = false;
            for (int k = 0; k < files.Length; k += 2)
            {
                if (k == 4)
                    isTestFile = true;
                string output = basepath + files[k + 1];
                string fullpath = basepath + files[k];
                string[] lines = System.IO.File.ReadAllLines(fullpath);
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(output))
                {
                    int index = 0;
                    foreach (string line in lines)
                    {
                        try
                        {
                            if (isTestFile == false)
                            {
                                Dictionary<int, double> dic = sc.allfeature(line);
                                string res = line+"\t0";
                                if (k == 2) res = line+"\t1";
                                foreach (var pair in dic)
                                {
                                    res += "\t" + pair.Value.ToString();
                                }
                                Console.WriteLine(res);
                                res = res.Trim();
                                sw.WriteLine(res);
                                if (index % 20 == 0) sw.Flush();
                            }
                            else
                            {
                                string[] tokens = line.Split('\t');
                                Dictionary<int, double> dic = sc.allfeature(tokens[0]);
                                string res = line+"\t"+tokens[1];
                                foreach (var pair in dic)
                                {
                                    res += "\t" + pair.Value.ToString();
                                }
                                res = res.Trim();
                                sw.WriteLine(res);
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        Console.WriteLine(index);
                        index++;
                    }

                }
            }
        }
        //static void Main(string[] args)
        //{
        //    sentenceClassifier sc = new sentenceClassifier();
        //    sc.init();
        //    //sc.LoadPhrase();
        //    //string query = "i want to change the subscription from small business premium to office 365 business";
        //    //Boolean result = sc.isgoodquery(query);
        //    string path = @"C:\Users\v-chuhz\SentenceClassifier\Query_classifier\Data_batch\batch_34";
        //    string tempposdataset = @"C:\Users\v-chuhz\SentenceClassifier\Query_classifier\tempposdataset4.txt";
        //    string tempnegdataset = @"C:\Users\v-chuhz\SentenceClassifier\Query_classifier\tempnegdataset4.txt";
        //    string[] lines = System.IO.File.ReadAllLines(path);
        //    int i = 0;
        //    using (System.IO.StreamWriter psw = new System.IO.StreamWriter(tempposdataset))
        //    {
        //        System.IO.StreamWriter nsw = new System.IO.StreamWriter(tempnegdataset);
        //        foreach (string line in lines)
        //        {
        //            if (i >= 0)
        //            {
        //                try
        //                {
        //                    string query = sc.GetBaseFormSentence(line);
        //                    Tuple<int, string> labelallFeature = sc.extractfeature(query);
        //                    if (labelallFeature.Item1 == 1)
        //                    {
        //                        psw.WriteLine(query + "\t" + 1 + "\t" + labelallFeature.Item2);
        //                    }
        //                    else nsw.WriteLine(query + "\t" + 0 + "\t" + labelallFeature.Item2);

        //                }
        //                catch (Exception e)
        //                {
        //                    Console.WriteLine(e.Message);
        //                }
        //            }

        //            Console.WriteLine("success:" + i);
        //            i++;
        //        }
        //        nsw.Close();

        //    }

        //    Console.ReadKey();
        //string basepath = @"C:\Users\v-chuhz\SentenceClassifier\";
        //string inputfile = basepath + @"lighttrainset.txt";
        //string outputfile = basepath + @"groundTruth3.txt";
        //string[] lines = System.IO.File.ReadAllLines(inputfile);
        //sentenceClassifier sc=new sentenceClassifier();
        //sc.init();
        //System.IO.StreamWriter sw = new System.IO.StreamWriter(outputfile);
        //foreach (string line in lines)
        //{
        //    string[] parts = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
        //    if (sc.isgoodquery(parts[2]))
        //    {
        //        sw.WriteLine("1" + "\t" + parts[2]);
        //    }
        //    else
        //    {
        //        sw.WriteLine("0" + "\t" + parts[2]);
        //    }
        //}
        //sw.Close();
        //}
    }
}
