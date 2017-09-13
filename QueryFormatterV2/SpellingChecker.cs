using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace SupportAgent//for example o365 --> office 365
{

    public class SpellingChecker
    {

        public Dictionary<string, int> mydict = new Dictionary<string, int>();//must load before running

        private const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        private static IEnumerable<string> edits1(string word)
        {
            var splits = from i in Enumerable.Range(0, word.Length)
                         select new { a = word.Substring(0, i), b = word.Substring(i) };
            var deletes = from s in splits
                          where s.b != "" // we know it can't be null
                          select s.a + s.b.Substring(1);
            var transposes = from s in splits
                             where s.b.Length > 1
                             select s.a + s.b[1] + s.b[0] + s.b.Substring(2);
            var replaces = from s in splits
                           from c in alphabet
                           where s.b != ""
                           select s.a + c + s.b.Substring(1);
            var inserts = from s in splits
                          from c in alphabet
                          select s.a + c + s.b;
            var inserts1 = from s in splits
                           from c in alphabet
                           select s.a + s.b + c;

            return deletes
            .Union(transposes) // union translates into a set
            .Union(replaces)
            .Union(inserts).Union(inserts1);
        }

        private IEnumerable<string> known_edits2(string word)
        {
            return (from e1 in edits1(word)
                    from e2 in edits1(e1)
                    where mydict.ContainsKey(e2) == true
                    select e2)
                   .Distinct();
        }

        /*private  IEnumerable<string> known(IEnumerable<string> words)
        {
            return words.Where(w => m_fSpellingChecker(w) != null);
        }*/
        private IEnumerable<string> words(string text)
        {
            return Regex.Matches(text.ToLower(), @"^\w+$")//@"^\w+$" [a-z]+
                        .Cast<Match>()
                        .Select(m => m.Value);
        }
        private Func<string, int?> train(IEnumerable<string> features)
        {
            var dict = features.GroupBy(f => f)
                               .ToDictionary(g => g.Key, g => g.Count());
            // mydict = dict;

            return f => dict.ContainsKey(f) ? dict[f] : (int?)null;
        }
        
        
        public class Word
        {
            public Dictionary<int, double> suffixWord;
            public static double beta = 1;
            public int totalSuffix;
            public Word()
            {
                suffixWord = new Dictionary<int, double>();
                totalSuffix = 0;
            }
            public void addOne(int key)
            {
                totalSuffix++;
                if (suffixWord.ContainsKey(key)) suffixWord[key]++;
                else
                {
                    suffixWord.Add(key, 1);
                }
            }
            public void smooth_ngram()
            {
                foreach (KeyValuePair<int, double> kw in suffixWord.ToList())
                {
                    suffixWord[kw.Key] = (kw.Value + beta) / (totalSuffix + wordMap.Count * beta);
                }
            }
        }
        public static string corpus;
        public static string dir;
        public static int totalWords = 0;
        public static Dictionary<string, int> wordMap = new Dictionary<string, int>();
        public static Dictionary<string, double> uniGramInfo = new Dictionary<string, double>();

        public static Dictionary<string, Word> wordToSuffix = new Dictionary<string, Word>();
        public static Dictionary<string, Word> bwordToSuffix = new Dictionary<string, Word>();
        public static string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "#", "\t", "\n", "/" };
        public SpellingChecker(string cc, string dd, string dictpath)
        {
            corpus = cc;
            dir = dd;
            using (StreamReader sr = new StreamReader(dictpath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim().ToLower();
                    if (mydict.ContainsKey(line)) continue;
                    mydict.Add(line, 1);
                }
                Console.WriteLine(mydict.Count());
            }
        }
        public SpellingChecker()
        {

        }
        public SpellingChecker(string cc, string dd)
        {
            corpus = cc;
            dir = dd;
            
            //LoadSpellingChecker("D:\\work\\Dev\\SupportAgent\\resource\\SpellingCheckerFile.txt");
        }
        public void unigramSmooth()
        {
            double beta = 0.5;
            string unigramInfo = dir + "//" + "unigram.txt";
            StreamWriter sw = new StreamWriter(unigramInfo);
            double totalCount = 0.0;
            Dictionary<string, double> finalDict = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in uniGramInfo.ToList())
            {
                if (kv.Value <= 5) continue;
                finalDict[kv.Key] = kv.Value;
                totalCount += kv.Value;
            }
            foreach (KeyValuePair<string, double> kv in finalDict.ToList())
            {
                finalDict[kv.Key] = (kv.Value + beta) / (totalCount + beta * finalDict.Count());
                sw.WriteLine(kv.Key + "\t" + finalDict[kv.Key]);
            }
            sw.Close();
        }
        public void buildDict()
        {
            //HashSet<string> words = new HashSet<string>();
            string line;
            StreamReader sr = new StreamReader(corpus);
            int index = 0;
            while ((line = sr.ReadLine()) != null)
            {
                //System.Console.WriteLine(line);
                //HashSet<string> strs = new HashSet<string>();

                line = new string("^ ".ToArray()) + line + new string(" $".ToArray());
                line = line.ToLower();
                string[] innerWords = line.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s_word in innerWords)
                {
                    totalWords += 1;
                    if (wordMap.ContainsKey(s_word) == false)
                    {

                        wordMap.Add(s_word, wordMap.Count);
                        uniGramInfo.Add(s_word, 1);
                        //words.Add(s_word);
                    }
                    else
                        uniGramInfo[s_word]++;
                }
                index++;
                if (index % 1000 == 0) Console.WriteLine(index);
            }
            unigramSmooth();
            sr.Close();
        }
        public void saveModel(string dir, string dictPath,string trigramFile)
        {
            using (StreamWriter sw = new StreamWriter(dir))
            {
                StreamWriter dictWriter = new StreamWriter(dictPath);
                StreamWriter trigramWriter = new StreamWriter(trigramFile);
                foreach (KeyValuePair<string, int> kw in wordMap)
                {
                    dictWriter.WriteLine(kw.Key + " " + kw.Value);
                }
                foreach (KeyValuePair<string, Word> kw in wordToSuffix)
                {
                    string line = kw.Key;
                    line += ":";
                    foreach (KeyValuePair<int, double> kwt in kw.Value.suffixWord)
                    {
                        line += kwt.Key + "&&&&" + kwt.Value;
                        line += " ";
                    }
                    sw.WriteLine(line);
                }
                foreach(KeyValuePair<string,Word> kw in bwordToSuffix)
                {
                    string line = kw.Key;
                    line += ':';
                    foreach(KeyValuePair<int,double> kwt in kw.Value.suffixWord)
                    {
                        line += kwt.Key + "&&&&" + kwt.Value;
                        line += " ";
                    }
                    trigramWriter.WriteLine(line);
                }
                dictWriter.Close();
                trigramWriter.Close();
            }
        }
        public void loadModel(string wordSuffixPath, string idDict, string realDict, string unigram,string trigram)
        {
            loadModel(wordSuffixPath,idDict,realDict,unigram);
            bwordToSuffix.Clear();
            StreamReader sr = new StreamReader(trigram);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                //Console.WriteLine(line);
                string[] strs_tm = line.Split(@":".ToArray());
                bwordToSuffix.Add(strs_tm[0], new Word());
                string[] wordSuffixMap = strs_tm[1].Split(@" ".ToArray());
                string[] stringSeparators = new string[] { "&&&&" };
                foreach (string token in wordSuffixMap)
                {
                    if (token.Length == 0) continue;
                    string[] tmp = token.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    bwordToSuffix[strs_tm[0]].suffixWord.Add(Int32.Parse(tmp[0]), Double.Parse(tmp[1]));
                }
            }
            sr.Close();
            Console.WriteLine("load trigram finished");
        }
        public void loadModel(string wordSuffixPath, string idDict, string realDict, string unigram)
        {
            wordMap.Clear();
            //bwordToSuffix.Clear();
            wordToSuffix.Clear();
            using (StreamReader sr = new StreamReader(wordSuffixPath))
            {
                StreamReader unigramReader = new StreamReader(unigram);

                StreamReader dictReader = new StreamReader(idDict);
                string line = null;
                uniGramInfo.Clear();
                while ((line = unigramReader.ReadLine()) != null)
                {
                    string[] tokens = line.Split('\t');
                    uniGramInfo[tokens[0]] = double.Parse(tokens[1]);
                }
                unigramReader.Close();
                while ((line = dictReader.ReadLine()) != null)
                {
                    string[] segs = line.Split(@" ".ToArray());
                    //Console.WriteLine(line);
                    if(wordMap.ContainsKey(segs[0])==false)
                        wordMap.Add(segs[0], int.Parse(segs[1]));
                    /*if (mydict.ContainsKey(segs[0]) == false)
                    {
                        mydict.Add(segs[0], 1);
                    }*/
                }
                // mydict = wordMap;
                StreamReader rdr = new StreamReader(realDict);
                Console.WriteLine(realDict);
                while ((line = rdr.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    string[] spwords = line.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                    if (mydict.ContainsKey(line) == false)
                        mydict.Add(line, 1);
                    foreach (string spword in spwords)
                    {
                        if (mydict.ContainsKey(spword) == false)
                        {
                            mydict.Add(spword, 1);
                        }
                    }
                }
                rdr.Close();
                dictReader.Close();
                while ((line = sr.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    string[] strs_tm = line.Split(@":".ToArray());
                    wordToSuffix.Add(strs_tm[0], new Word());
                    string[] wordSuffixMap = strs_tm[1].Split(@" ".ToArray());
                    string[] stringSeparators = new string[] { "&&&&" };
                    foreach (string token in wordSuffixMap)
                    {
                        if (token.Length == 0) continue;
                        string[] tmp = token.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        wordToSuffix[strs_tm[0]].suffixWord.Add(Int32.Parse(tmp[0]), Double.Parse(tmp[1]));
                    }
                }

            }
            Console.WriteLine("load spelling check ok");
        }

        public void train()
        {
            buildDict();
            Console.WriteLine("buildDict is finished");
            int index = 0;
            using (StreamReader sr = new StreamReader(corpus))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    //System.Console.WriteLine(line);

                    string[] senSeperator = new string[] { ".", ";", "?", "!", ":", "," };
                    //string[] words = sentence.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                    string[] innerLines = line.Trim().Split(senSeperator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string innerLine in innerLines)
                    {
                        string tmpLine = new string("^ ".ToArray()) + innerLine + new string(" $".ToArray());

                        tmpLine = tmpLine.ToLower();
                        string[] innerWords = tmpLine.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries);//to do
                        int cnt = 0;
                        foreach (string s_word in innerWords)
                        {

                            cnt++;
                            if (cnt == innerWords.Length) break;
                            if (wordToSuffix.ContainsKey(s_word))//the word the suffix has the key s_word
                            {
                                // System.Console.WriteLine(innerWords[cnt]);
                                if (wordMap.ContainsKey(innerWords[cnt]) == false)
                                    System.Console.WriteLine(innerWords[cnt]);
                                wordToSuffix[s_word].addOne(wordMap[innerWords[cnt]]);//innerWords[cnt] is the next word , than use innerWords to get id of the word
                            }
                            else
                            {
                                wordToSuffix.Add(s_word, new SupportAgent.SpellingChecker.Word());
                                wordToSuffix[s_word].addOne(wordMap[innerWords[cnt]]);//innerWords[cnt] is the next word , than use innerWords to get id of the word
                            }
                        }
                        cnt = 0;
                        for(int i=0;i<innerWords.Length - 2; i++)
                        {
                            string b_key = innerWords[i] + "&&^^&&" + innerWords[i + 1];
                            if (bwordToSuffix.ContainsKey(b_key))
                            {
                                bwordToSuffix[b_key].addOne(wordMap[innerWords[i + 2]]);
                            }else
                            {
                                bwordToSuffix.Add(b_key,new SpellingChecker.Word());
                                bwordToSuffix[b_key].addOne(wordMap[innerWords[i + 2]]);
                            }
                        }
                    }
                    index++;
                    if (index % 1000 == 0) Console.WriteLine(index);
                }
                foreach (KeyValuePair<string, Word> kw in wordToSuffix)
                {
                    wordToSuffix[kw.Key].smooth_ngram();
                }
                foreach(KeyValuePair<string,Word> kw in bwordToSuffix)
                {
                    bwordToSuffix[kw.Key].smooth_ngram();
                }
                //mydict = wordMap;
                Console.WriteLine("before save model\n" + dir);

                saveModel(dir + "//wordSuffix.txt", dir + "//dict.txt",dir+"//trigramSuffix.txt");
                Console.WriteLine("after save model");
            }
        }

        public double getProb(List<string> strs)
        {
            if (wordToSuffix.ContainsKey(strs[0]))
            {
                if (wordMap.ContainsKey(strs[1]) == false) return Math.Pow(10.0, -18);
                int id = wordMap[strs[1]];
                if (wordToSuffix[strs[0]].suffixWord.ContainsKey(id))
                    return (wordToSuffix[strs[0]].suffixWord[id]);

            }
            return Math.Pow(10.0, -20);
        }
        public double getTriProb(List<string> strs)
        {
            if (bwordToSuffix.ContainsKey(strs[0]))
            {
                if (wordMap.ContainsKey(strs[1]) == false) return Math.Pow(10,-14);
                int id = wordMap[strs[1]];
                if (bwordToSuffix[strs[0]].suffixWord.ContainsKey(id))
                    return bwordToSuffix[strs[0]].suffixWord[id];
            }
            return Math.Pow(10.0,-15);
        }
        private static Stemmer stem = new Stemmer();
        //private bool is
        private bool needCheck(string word)
        {
            //Regex regex = new Regex(@"^\w+$");
            if (mydict.ContainsKey(word) == true || mydict.ContainsKey(stem.GetBaseFormWord(word)) == true)
                return false;
            if (fileExtMap.ContainsKey(word)) return false;
            Regex regex1 = new Regex("^[A-Za-z0-9]+$");
            if (regex1.IsMatch(word) == false) return false;
            string[] regstrs = { "^-?[1-9]\\d*$" , "^(-?\\d+)(\\.\\d+)?$", "^(d{1,2}|1dd|2[0-4]d|25[0-5]).(d{1,2}|1dd|2[0-4]d|25[0-5]).(d{1,2}|1dd|2[0-4]d|25[0-5]).(d{1,2}|1dd|2[0-4]d|25[0-5])$",
            "\\w+([-+.]\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*"};
            foreach (string reg in regstrs)
            {
                Regex regex = new Regex(reg);
                if (regex.IsMatch(word)) return false;
            }
            //string contains digit
            foreach (char c in word.ToArray())
            {
                if (c >= '0' && c <= '9') return false;
            }
            return true;
        }
        string[] SmartSplit(string sentence)
        {
            //string[] regstrs = { "^([a-zA-Z0-9]([a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])?\\.)+[a-zA-Z]{2,6}$" };
            List<string> strs = new List<string>();
            //string cur = "";
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
        public KeyValuePair<string, double> wordCorrect(string[] tokens, int i, string tocorrect)
        {
            Dictionary<string, double> wordMap = new Dictionary<string, double>();
            double maxScore = -30;
            string token = null;
            string beforeWord = tokens[i - 1], afterWord = tokens[i + 1];
            foreach (string s in edits1(tocorrect))
            {
                if (mydict.ContainsKey(s) == false) continue;

                List<string> blist = new List<string>(), alist = new List<string>();
                blist.Add(beforeWord);
                blist.Add(s);
                alist.Add(s);
                alist.Add(afterWord);
                double bs = getProb(blist), afs = getProb(alist);
                double unigramScore = Math.Pow(10, -20);
                if (uniGramInfo.ContainsKey(s))
                    unigramScore = uniGramInfo[s];
                double score = Math.Max(bs, afs);
                double score1 = Math.Min(bs, afs);

                //if (score1 > 0) score = score + 0.5 * score1;
                wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)) / 3.0;
                if (Resource.m_productList.Contains(s))// it is a production rule for the result
                    wordMap[s] += 5;
                // wordMap[s] = score;
                if (wordMap[s] > maxScore)
                {
                    maxScore = wordMap[s];
                    token = s;
                }
                //System.Console.WriteLine(s + " " + wordMap[s]);
                System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]);
            }
            double selfScore = 0.0;
            List<string> bselflist = new List<string>(), aselflist = new List<string>();
            bselflist.Add(beforeWord);
            bselflist.Add(tocorrect);
            aselflist.Add(tocorrect);
            aselflist.Add(afterWord);
            double bsSelf = getProb(bselflist), afsSelf = getProb(aselflist);
            double unigramScoreSelf = Math.Pow(10, -6);
            if (uniGramInfo.ContainsKey(tocorrect))
                unigramScoreSelf = uniGramInfo[tocorrect];
            double finalSelfScore = (Math.Log(bsSelf) + Math.Log(afsSelf) + Math.Log(unigramScoreSelf)) / 3.0;
            System.Console.WriteLine(tocorrect + "----->" + tocorrect + "\t" + bsSelf + "\t" + afsSelf + "\t" + unigramScoreSelf + "\t" + finalSelfScore);
            if (maxScore - finalSelfScore < 3)
            {
                maxScore = finalSelfScore;
                token = tocorrect;
            }
            //Console.WriteLine(maxScore);
            wordMap.Clear();
            if (maxScore < -100)
            {

                foreach (string s in known_edits2(tokens[i]))
                {
                    if (mydict.ContainsKey(s) == false) continue;

                    List<string> blist = new List<string>(), alist = new List<string>();
                    blist.Add(beforeWord);
                    blist.Add(s);
                    alist.Add(s);
                    alist.Add(afterWord);
                    double bs = getProb(blist), afs = getProb(alist);
                    double score = Math.Max(bs, afs);
                    double score1 = Math.Min(bs, afs);
                    double unigramScore = Math.Pow(10, -20);
                    if (uniGramInfo.ContainsKey(s))
                        unigramScore = uniGramInfo[s];
                    wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)) / 3.0;
                    //wordMap[s] = score1;
                    //System.Console.WriteLine(s + " " + wordMap[s]);
                    System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]);
                }
            }
            foreach (KeyValuePair<string, double> kw in wordMap.ToList())
            {
                if (kw.Value > maxScore)
                {
                    token = kw.Key; maxScore = kw.Value;
                }
            }
            return new KeyValuePair<string, double>(token, maxScore);

        }
        bool isUpperToken(string str,bool isFirst)
        {
            if (isFirst)
            {
                for (int i = 1; i < str.Length; i++) if (str[i] >= 'A' && str[i] <= 'Z')
                        return true;
                return false;
            }
            else
            {
                int cnt = 0;
                foreach(char c in str.ToArray())
                {
                    if (c >= 'A' && c <= 'Z')cnt++;
                }
                return cnt >= 2 ? true : false;
            }
        }
        void getIsUpper(bool[] isUpper, string[] tokens)
        {
            for(int i =0;i<tokens.Length;i++)
            {
                isUpper[i] = isUpperToken(tokens[i],i==0);
            }
        }
        bool isLeftAddChange(string origin, string tobe)
        {
            origin = origin.ToLower();
            tobe = tobe.ToLower();
            if (origin.Length > 2 && origin.Substring(1) == tobe)
            {
                double averageProb = 1.0 / uniGramInfo.Count();
                if (uniGramInfo.ContainsKey(tobe) && uniGramInfo[tobe] > averageProb)
                    return true;
                else return false;
            }
            else
                return false;
            
        }
        public bool correct(string sentence, bool isNgram,out string retSentence)
        {
            // string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "#", "   " };
            sentence = new string("^ ".ToArray()) + sentence + new string(" $".ToArray());

            //sentence = sentence.ToLower();
            string[] tokens = SmartSplit(sentence);
            bool[] isUpper = new bool[tokens.Length];
            getIsUpper(isUpper,tokens);
            for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].ToLower();
            List<string> strs = new List<String>();
            bool isChanged = false;
            for (int i = 1; i < tokens.Length - 1; i++)
            {
                if (needCheck(tokens[i]) == false||isUpper[i])
                {
                    strs.Add(tokens[i]);
                    continue;
                }
                else
                {

                    KeyValuePair<string, double> kw1 = wordCorrect(tokens, i, tokens[i]);
                    KeyValuePair<string, double> kw2 = wordCorrect(tokens, i, stem.GetBaseFormWord(tokens[i]));
                    //System.Console.WriteLine(tokens[i] + "=====》" + token);
                    if (kw1.Value - kw2.Value < -1.5)
                        kw1 = kw2;
                    if (kw1.Value > -15)
                    {
                        if(kw1.Key.CompareTo(tokens[i])!=0)
                            isChanged = true;
                        System.Console.WriteLine(kw1.Value);
                        strs.Add(kw1.Key);
                        System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                    }
                    else
                    {
                        if (Resource.m_productList.Contains(kw1.Key))
                        {
                            if (kw1.Key.CompareTo(tokens[i]) != 0)
                                isChanged = true;
                            System.Console.WriteLine(kw1.Value);
                            strs.Add(kw1.Key);
                            System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                        }
                        else
                            strs.Add(tokens[i]);
                    }
                }
            }
            retSentence = String.Join(" ", strs);
            return isChanged;
        }
        private Dictionary<string, string> fileExtMap = new Dictionary<string, string>();
        private  string[] sentenceToWords(string sentence)
        {
            string[] sep = new string[] { " " };
            string[] tokens = sentence.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            // Dictionary<string, string> fileExtMap = new Dictionary<string, string>();
            fileExtMap.Clear();
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
                if (token.StartsWith(".") && token.Length > 2)
                {
                    if (noGoodEnd.Contains(token[token.Length - 1] + ""))
                    {
                        string fileExt = token.Substring(0, token.Length - 1);
                        if (!fileExtMap.ContainsKey(fileExt))
                        {
                            fileExtMap[fileExt] = "$$fileext" + fileExtIndex;
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                            fileExtIndex++;
                        }
                        else
                        {
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                        }
                    }
                    else
                    {
                        string fileExt = token;
                        if (!fileExtMap.ContainsKey(fileExt))
                        {
                            fileExtMap[fileExt] = "$$fileext" + fileExtIndex;
                            tokens[index] = tokens[index].Replace(fileExt, fileExtMap[fileExt]);
                            fileExtIndex++;
                        }
                        else
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
        private bool isGoodWord(string word)
        {
            string tmp = word.ToLower();
            foreach (char c in tmp)
            {
                if (c >= 'a' && c <= 'z') return true;
            }
            return false;
        }
        private string findBeforeTwoWords(string[] words,int i)
        {
            if (i >= 2)
            {
                return words[i - 2] + "&&^^&&" + words[i - 1];
            }
            else return "<NULL><NULL>";
        }
        private string findBeforeCur(string[] words,int i,string changeto)
        {
            if (i > 0)
            {
                return words[i - 1] + "&&^^&&" + changeto;
            }else
            {
                return "<NULL><NULL>";
            }
        }
        private string findCurAfter(string[] words,int i,string changeto)
        {
            if (i < words.Length - 1)
            {
                return changeto + "&&^^&&" + words[i + 1];
            }
            return "<NULL><NULL>";
        }
        private string findBeforeWord(string[] words, int i)
        {
            //".", ";", "?", "!"
            string[] my_seperator = new string[] { ".", ";", "?", "!", ",", "^" };

            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            for (i = i - 1; i >= 0; i--)
            {
                if (noGoodEnd.Contains(words[i]))
                {
                    return "^";
                }
                else
                {
                    if (isGoodWord(words[i])) return words[i];
                }
            }
            return null;
        }
        private string findNextWord(string[] words, int i)
        {
            string[] my_seperator = new string[] { ".", ";", "?", "!", ",", "$" };

            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            for (i = i + 1; i < words.Length; i++)
            {
                if (noGoodEnd.Contains(words[i])) return "$";
                else
                {
                    if (isGoodWord(words[i])) return words[i];
                }
            }
            return null;
        }
        public static bool isAdd_or_removeSameLetter(string a,string b)
        {
            if (a.Length + 1 == b.Length) return isAdd_or_removeSameLetter(b,a);
            if (a.Length == b.Length + 1)
            {
                int i = 0, a_j = a.Length - 1, b_j = b.Length - 1;
                while (i < b.Length&&a[i] == b[i] ) i++;

                if (a.Substring(i+1) == b.Substring(i))
                {
                    if (i > 0 && a[i] == a[i - 1]) return true;
                    if (i < a.Length - 1 && a[i] == a[i + 1]) return true;
                }
                return false;

            }
            else return false;

        }
        public KeyValuePair<string, double> wordCorrect(string beforeWord, string afterWord, string tocorrect,int index,string[] words)
        {
            Dictionary<string, double> wordMap = new Dictionary<string, double>();
            double maxScore = -30;
            string token = null;
            // string beforeWord = tokens[i - 1], afterWord = tokens[i + 1];
            string beforeTwo = null, beforeCur = null, curAfter = null;
            double bs = 0, afs = 0;
            List<string> b2list = new List<string>(), bclist = new List<string>(), calist = new List<string>();
            double bts = 0, bcs = 0, cas = 0;
            foreach (string s in edits1(tocorrect))
            {
               
                if (mydict.ContainsKey(s) == false) continue;

                List<string> blist = new List<string>(), alist = new List<string>();
                b2list.Clear();bclist.Clear();calist.Clear();
                blist.Add(beforeWord);
                blist.Add(s);
                alist.Add(s);
                alist.Add(afterWord);
                beforeTwo = findBeforeTwoWords(words, index); beforeCur = findBeforeCur(words, index, s); curAfter = findCurAfter(words,index,s) ;
                bs = getProb(blist); afs = getProb(alist);
                
                b2list.Add(beforeTwo);
                b2list.Add(s);
                bclist.Add(beforeCur);
                bclist.Add(afterWord);
                calist.Add(curAfter);
                if (index + 2 < words.Length) calist.Add(words[index + 2]);
                else calist.Add("<NULL><NULL>");
                bts = getTriProb(b2list); bcs = getTriProb(bclist); cas = getTriProb(calist);
                double unigramScore = Math.Pow(10, -20);
                if (uniGramInfo.ContainsKey(s))
                    unigramScore = uniGramInfo[s];
                double score = Math.Max(bs, afs);
                double score1 = Math.Min(bs, afs);

                //if (score1 > 0) score = score + 0.5 * score1;
                wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)+Math.Log(bts)+Math.Log(bcs)+Math.Log(cas)) / 6.0;
                if (Resource.m_productList.Contains(s))// it is a production rule for the result
                {
                    if (s != "word")
                        wordMap[s] += 7;
                    else wordMap[s] += 3;
                }
                if (isAdd_or_removeSameLetter(s, tocorrect)) wordMap[s] += 3;
                // wordMap[s] = score;
                if (wordMap[s] > maxScore)
                {
                    maxScore = wordMap[s];
                    token = s;
                }
                //System.Console.WriteLine(s + " " + wordMap[s]);
                System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]+"\t"+bts+"\t"+bcs+"\t"+cas);
            }
            
            List<string> bselflist = new List<string>(), aselflist = new List<string>();
            bselflist.Add(beforeWord);
            bselflist.Add(tocorrect);
            aselflist.Add(tocorrect);
            aselflist.Add(afterWord);
            double bsSelf = getProb(bselflist), afsSelf = getProb(aselflist);
            double unigramScoreSelf = Math.Pow(10, -6);
            if (uniGramInfo.ContainsKey(tocorrect))
                unigramScoreSelf = uniGramInfo[tocorrect];
            beforeTwo = findBeforeTwoWords(words, index); beforeCur = findBeforeCur(words, index, tocorrect);
            curAfter = findCurAfter(words, index, tocorrect);
            b2list.Clear(); bclist.Clear(); calist.Clear();
            b2list.Clear();bclist.Clear();calist.Clear();
            b2list.Add(beforeTwo);
            b2list.Add(tocorrect);
            bclist.Add(beforeCur);
            bclist.Add(afterWord);
            calist.Add(curAfter);
            if (index + 2 < words.Length) calist.Add(words[index + 2]);
            else calist.Add("<NULL><NULL>");
            bts = getTriProb(b2list); bcs = getTriProb(bclist); cas = getTriProb(calist);
            double finalSelfScore = (Math.Log(bsSelf) + Math.Log(afsSelf) + Math.Log(unigramScoreSelf)+ Math.Log(bts) + Math.Log(bcs) + Math.Log(cas)) / 6.0;
            System.Console.WriteLine(tocorrect + "----->" + tocorrect + "\t" + bsSelf + "\t" + afsSelf + "\t" + unigramScoreSelf + "\t" + finalSelfScore + "\t" + bts + "\t" + bcs + "\t" + cas);
            if (maxScore - finalSelfScore < 4)
            {
                maxScore = finalSelfScore;
                token = tocorrect;
            }
            //Console.WriteLine(maxScore);
            wordMap.Clear();
            if (maxScore < -100)
            {

                foreach (string s in known_edits2(tocorrect))
                {
                    if (mydict.ContainsKey(s) == false) continue;

                    List<string> blist = new List<string>(), alist = new List<string>();
                    blist.Add(beforeWord);
                    blist.Add(s);
                    alist.Add(s);
                    alist.Add(afterWord);
                    bs = getProb(blist); afs = getProb(alist);
                    double score = Math.Max(bs, afs);
                    double score1 = Math.Min(bs, afs);
                    double unigramScore = Math.Pow(10, -20);
                    if (uniGramInfo.ContainsKey(s))
                        unigramScore = uniGramInfo[s];
                    wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)) / 3.0;
                    //wordMap[s] = score1;
                    System.Console.WriteLine(s + " " + wordMap[s]);
                    System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]);
                }
            }
            foreach (KeyValuePair<string, double> kw in wordMap.ToList())
            {
                if (kw.Value > maxScore)
                {
                    token = kw.Key; maxScore = kw.Value;
                }
            }
            return new KeyValuePair<string, double>(token, maxScore);
        }
        public KeyValuePair<string, double> wordCorrect(string beforeWord, string afterWord, string tocorrect)
        {
            Dictionary<string, double> wordMap = new Dictionary<string, double>();
            double maxScore = -30;
            string token = null;
            // string beforeWord = tokens[i - 1], afterWord = tokens[i + 1];
            foreach (string s in edits1(tocorrect))
            {
                if (mydict.ContainsKey(s) == false) continue;

                List<string> blist = new List<string>(), alist = new List<string>();
                blist.Add(beforeWord);
                blist.Add(s);
                alist.Add(s);
                alist.Add(afterWord);
                double bs = getProb(blist), afs = getProb(alist);
                double unigramScore = Math.Pow(10, -20);
                if (uniGramInfo.ContainsKey(s))
                    unigramScore = uniGramInfo[s];
                double score = Math.Max(bs, afs);
                double score1 = Math.Min(bs, afs);

                //if (score1 > 0) score = score + 0.5 * score1;
                wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)) / 3.0;
                if (Resource.m_productList.Contains(s))// it is a production rule for the result
                    wordMap[s] += 5;
                // wordMap[s] = score;
                if (wordMap[s] > maxScore)
                {
                    maxScore = wordMap[s];
                    token = s;
                }
                System.Console.WriteLine(s + " " + wordMap[s]);
                System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]);
            }
            double selfScore = 0.0;
            List<string> bselflist = new List<string>(), aselflist = new List<string>();
            bselflist.Add(beforeWord);
            bselflist.Add(tocorrect);
            aselflist.Add(tocorrect);
            aselflist.Add(afterWord);
            double bsSelf = getProb(bselflist), afsSelf = getProb(aselflist);
            double unigramScoreSelf = Math.Pow(10, -6);
            if (uniGramInfo.ContainsKey(tocorrect))
                unigramScoreSelf = uniGramInfo[tocorrect];
            double finalSelfScore = (Math.Log(bsSelf) + Math.Log(afsSelf) + Math.Log(unigramScoreSelf)) / 3.0;
            System.Console.WriteLine(tocorrect + "----->" + tocorrect + "\t" + bsSelf + "\t" + afsSelf + "\t" + unigramScoreSelf + "\t" + finalSelfScore);
            if (maxScore - finalSelfScore < 3)
            {
                maxScore = finalSelfScore;
                token = tocorrect;
            }
            Console.WriteLine(maxScore);
            wordMap.Clear();
            if (maxScore < -100)
            {

                foreach (string s in known_edits2(tocorrect))
                {
                    if (mydict.ContainsKey(s) == false) continue;

                    List<string> blist = new List<string>(), alist = new List<string>();
                    blist.Add(beforeWord);
                    blist.Add(s);
                    alist.Add(s);
                    alist.Add(afterWord);
                    double bs = getProb(blist), afs = getProb(alist);
                    double score = Math.Max(bs, afs);
                    double score1 = Math.Min(bs, afs);
                    double unigramScore = Math.Pow(10, -20);
                    if (uniGramInfo.ContainsKey(s))
                        unigramScore = uniGramInfo[s];
                    wordMap[s] = (Math.Log(score1) + Math.Log(score) + Math.Log(unigramScore)) / 3.0;
                    //wordMap[s] = score1;
                    //System.Console.WriteLine(s + " " + wordMap[s]);
                    //System.Console.WriteLine(tocorrect + "----->" + s + "\t" + bs + "\t" + afs + "\t" + unigramScore + "\t" + wordMap[s]);
                }
            }
            foreach (KeyValuePair<string, double> kw in wordMap.ToList())
            {
                if (kw.Value > maxScore)
                {
                    token = kw.Key; maxScore = kw.Value;
                }
            }
            return new KeyValuePair<string, double>(token, maxScore);
        }
        public string correct2(string sentence, bool isNgram)
        {
            string backup = sentence;
            sentence = new string("^ ".ToArray()) + sentence + new string(" $".ToArray());
            //sentence = sentence.ToLower();
            string[] tokens = sentenceToWords(sentence);
            bool[] isUpper = new bool[tokens.Length];
            getIsUpper(isUpper, tokens);
            
            List<string> backList = new List<string>(tokens);
            for (int j = 0; j < tokens.Length; j++)
            {
                tokens[j] = tokens[j].ToLower();
            }
            
            string ret = "";
            int i = 1;
            string[] my_seperator =
                new string[] { ".", ";", "?", "!", ":", "\\", ",", "(", ")", "|", "[", "]", "{", "}" };

            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            while (i < tokens.Length - 1)
            {
                if (noGoodEnd.Contains(tokens[i]))
                {
                    ret += backList[i];
                }
                else
                {
                    if (needCheck(tokens[i]) == false||isUpper[i])
                    {
                        ret += " " + backList[i];
                    }
                    else
                    {
                        string beforeWord = findBeforeWord(tokens, i), afterWord = findNextWord(tokens, i);
                        KeyValuePair<string, double> kw1 = wordCorrect(beforeWord, afterWord, tokens[i]);
                        KeyValuePair<string, double> kw2 = wordCorrect(beforeWord, afterWord, stem.GetBaseFormWord(tokens[i]));
                        //System.Console.WriteLine(tokens[i] + "=====》" + token);
                        if (kw1.Value - kw2.Value < -1.5)
                            kw1 = kw2;
                        if (kw1.Value > -15)
                        {
                            //System.Console.WriteLine(kw1.Value);
                            if (!isLeftAddChange(tokens[i], kw1.Key))
                                ret += " " + kw1.Key;
                            else ret += " " + backList[i];
                            //System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                        }
                        else
                        {
                            if (Resource.m_productList.Contains(kw1.Key) && !!isLeftAddChange(tokens[i], kw1.Key))
                                ret += " " + kw1.Key;
                            else
                                ret += " " + backList[i];
                        }
                    }
                }
                i++;
            }
            return ret.Trim();
        }
        
        public bool correct2(string sentence, bool isNgram, out string retSentence,out string changeResult)
        {
            string uu = "";
            string backup = sentence;
            sentence = new string("^ ".ToArray()) + sentence + new string(" $".ToArray());
            //sentence = sentence.ToLower();
            string[] tokens = sentenceToWords(sentence);
            bool[] isUpper = new bool[tokens.Length];
            getIsUpper(isUpper, tokens);

            List<string> backList = new List<string>(tokens);
            for (int j = 0; j < tokens.Length; j++)
            {
                tokens[j] = tokens[j].ToLower();
            }
            bool isChanged = false;
            string ret = "";
            int i = 1;
            string[] my_seperator =
                new string[] { ".", ";", "?", "!", ":", "\\", ",", "(", ")", "|", "[", "]", "{", "}" };

            HashSet<string> noGoodEnd = new HashSet<string>();
            foreach (string s in my_seperator)
            {
                noGoodEnd.Add(s);
            }
            while (i < tokens.Length - 1)
            {
                if (noGoodEnd.Contains(tokens[i]))
                {
                    ret += backList[i];
                }
                else
                {
                    if (needCheck(tokens[i]) == false || isUpper[i])
                    {
                        if (fileExtMap.ContainsKey(tokens[i]))
                            backList[i] = fileExtMap[tokens[i]];
                        ret += " " + backList[i];
                    }
                    else
                    {
                        string beforeWord = findBeforeWord(tokens, i), afterWord = findNextWord(tokens, i);
                        KeyValuePair<string, double> kw1 = wordCorrect(beforeWord, afterWord, tokens[i],i,tokens);
                        KeyValuePair<string, double> kw2 = wordCorrect(beforeWord, afterWord, stem.GetBaseFormWord(tokens[i]),i,tokens);
                        //System.Console.WriteLine(tokens[i] + "=====》" + token);
                        if (kw1.Value - kw2.Value < -1.5)
                            kw1 = kw2;
                        if (kw1.Value > -18.5)
                        {
                            //System.Console.WriteLine(kw1.Value);
                            if (!isLeftAddChange(tokens[i], kw1.Key))
                            {
                                ret += " " + kw1.Key;
                                if (tokens[i].CompareTo(kw1.Key) != 0)
                                {
                                    uu += (tokens[i]+"---->"+kw1.Key+"   ");
                                    isChanged = true;
                                }
                            }
                            else ret += " " + backList[i];
                            //System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                        }
                        else
                        {
                            if (Resource.m_productList.Contains(kw1.Key) && !!isLeftAddChange(tokens[i], kw1.Key))
                            {
                                if (tokens[i].CompareTo(kw1.Key) != 0)
                                {
                                    uu += (tokens[i] + "---->" + kw1.Key + "   ");
                                    isChanged = true;
                                }
                                ret += " " + kw1.Key;
                            }
                            else
                                ret += " " + backList[i];
                        }
                    }
                }
                i++;
            }
            retSentence = ret.Trim();
            changeResult = uu;
            return isChanged;

        }
        public string correct(string sentence, bool isNgram)
        {
            // string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "#", "   " };
            sentence = new string("^ ".ToArray()) + sentence + new string(" $".ToArray());
            sentence = sentence.ToLower();
            string[] tokens = SmartSplit(sentence);
            List<string> strs = new List<String>();
            bool isChanged = false;
            for (int i = 1; i < tokens.Length - 1; i++)
            {
                if (needCheck(tokens[i])==false)
                {
                    strs.Add(tokens[i]);
                    continue;
                }
                else
                {

                    KeyValuePair<string, double> kw1 = wordCorrect(tokens,i,tokens[i]);
                    KeyValuePair<string, double> kw2 = wordCorrect(tokens, i, stem.GetBaseFormWord(tokens[i]));
                    //System.Console.WriteLine(tokens[i] + "=====》" + token);
                    if (kw1.Value - kw2.Value < -1.5)
                        kw1 = kw2;
                    if (kw1.Value > -15)
                    {
                        System.Console.WriteLine(kw1.Value);
                        if (!isLeftAddChange(tokens[i], kw1.Key)) strs.Add(kw1.Key);
                        else strs.Add(tokens[i]);
                        //System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                    }
                    else
                    {
                        if (Resource.m_productList.Contains(kw1.Key)&&!!isLeftAddChange(tokens[i], kw1.Key))
                        {
                            System.Console.WriteLine(kw1.Value);
                            strs.Add(kw1.Key);
                            //System.Console.WriteLine(tokens[i] + "=====》" + kw1.Key);
                        }else 
                            strs.Add(tokens[i]);
                    }
                }
            }
            return String.Join(" ", strs);
        }
    }

}
