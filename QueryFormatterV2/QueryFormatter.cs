using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportAgent
{
    class QueryFormatter
    {
        public static double idfThresholdHigh = 5.0;
        public static double idfThresholdLow = 2.5;
        public static HashSet<string> m_hTerms = new HashSet<string>();
        public static PhraseExtractTool pet = null;
        public static sentenceClassifier sc = new sentenceClassifier();
        QueryFormatter(PhraseExtractTool pp)
        {
            pet = pp;
        }
        public static string FormatKeySentence(string input)
        {
            Console.WriteLine("before");
            //string spellingInput = Resource.m_spellingChecker.correct(input,true);

            //Console.WriteLine(input+"-->"+spellingInput);
            // input = spellingInput ;

            List<string> frags = Seperate(input.ToLower());
            List<string> finalQuery = new List<string>();
            List<string> queryComponents = new List<string>();
            string[] seperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            string[] mytokens = input.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            if (mytokens.Length <= 3) return string.Join(" ", mytokens);
            Stemmer stem = new Stemmer();
            double maxScore = -10;
            foreach (string frag in frags)
            {
                double score = sc.queryScore(frag);
                if (score > maxScore) maxScore = score;
            }

            foreach (string frag in frags)
            {
                //string frag = Resource.m_spellingChecker.correct(frag_2,true);
                //frag = frag_2;
                double score = sc.queryScore(frag);
                if (frags.Count()>1&&maxScore == score)
                {
                    finalQuery.Add(frag);
                    continue;
                }
                List<string> words = stem.GetBaseFormSentence(frag).Split().ToList();
                Tuple<int, int, bool> indicatorIdx = IndicatorIndex(words);
                if (indicatorIdx.Item2 == -1)
                {
                    finalQuery.Add(SelectTerms(words, idfThresholdHigh));
                }
                else
                {
                    List<string> wordsHigh = words.Take(indicatorIdx.Item1).ToList();
                    List<string> wordsLow = words.Skip(indicatorIdx.Item2).ToList();
                    finalQuery.Add(SelectTerms(wordsHigh, idfThresholdHigh));
                    if (indicatorIdx.Item3)
                        finalQuery.Add("not");
                    finalQuery.Add(SelectTerms(wordsLow, idfThresholdLow));
                }
                //finalQuery.Add(" # ");
            }
            m_hTerms.Clear();
            return string.Join(" ", finalQuery);
        }
        public static string Format(string input)
        {
            Console.WriteLine("before");
            //string spellingInput = Resource.m_spellingChecker.correct(input,true);
            
            //Console.WriteLine(input+"-->"+spellingInput);
           // input = spellingInput ;
            
            List<string> frags = Seperate(input.ToLower());
            List<string> finalQuery = new List<string>();
            List<string> queryComponents = new List<string>();
            string[] seperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/"};
            string[] mytokens = input.Split(seperator,StringSplitOptions.RemoveEmptyEntries);
            if (mytokens.Length <= 3) return string.Join(" ",mytokens);
            Stemmer stem = new Stemmer();
            foreach(string frag in frags)
            {
                //string frag = Resource.m_spellingChecker.correct(frag_2,true);
                //frag = frag_2;
                List<string>  words = stem.GetBaseFormSentence(frag).Split().ToList();
                Tuple<int, int, bool> indicatorIdx = IndicatorIndex(words);
                if(indicatorIdx.Item2 == -1)
                {
                    finalQuery.Add(SelectTerms(words, idfThresholdHigh));
                }
                else
                {
                    List<string> wordsHigh = words.Take(indicatorIdx.Item1).ToList();
                    List<string> wordsLow = words.Skip(indicatorIdx.Item2).ToList();
                    finalQuery.Add(SelectTerms(wordsHigh, idfThresholdHigh));
                    if (indicatorIdx.Item3)
                        finalQuery.Add("not");
                    finalQuery.Add(SelectTerms(wordsLow, idfThresholdLow));
                }
                //finalQuery.Add(" # ");
            }
            m_hTerms.Clear();
            return string.Join(" ", finalQuery);
        }
        public static string FormatNochange(string input){
            return input;
        }
        public static string Format2(string input)
        {
            Console.WriteLine("before");
            //string spellingInput = Resource.m_spellingChecker.correct(input,true);

            //Console.WriteLine(input+"-->"+spellingInput);
            // input = spellingInput ;

            List<string> frags = Seperate(input.ToLower());
            List<string> finalQuery = new List<string>();
            List<string> queryComponents = new List<string>();
            string[] seperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            string[] mytokens = input.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            if (mytokens.Length <= 3) return string.Join(" ", mytokens);
            Stemmer stem = new Stemmer();
            string ret = "";
            foreach (string frag in frags)
            {
                //string frag = Resource.m_spellingChecker.correct(frag_2,true);
                //frag = frag_2;
                List<string> words = stem.GetBaseFormSentence(frag).Split().ToList();
                foreach (string word in words)
                {
                    //if(Resource.m_i)
                    if (Resource.m_dIdf.ContainsKey(word))
                    {
                        if(Resource.m_dIdf[word] > 4)
                            ret+=(" "+word);
                    }
                    else
                    {
                        ret += (" " + word);
                    }
                }
                //finalQuery.Add(" # ");
            }
            m_hTerms.Clear();
            return ret.Trim();
        }
        public static string Format3(string input)
        {
            Console.WriteLine("before");
            //string spellingInput = Resource.m_spellingChecker.correct(input,true);

            //Console.WriteLine(input+"-->"+spellingInput);
            // input = spellingInput ;

            List<string> frags = Seperate(input.ToLower());
            List<string> finalQuery = new List<string>();
            List<string> queryComponents = new List<string>();
            string[] seperator = new string[] { " ", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            string[] mytokens = input.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            if (mytokens.Length <= 3) return string.Join(" ", mytokens);
            Stemmer stem = new Stemmer();
            string ret = "";
            foreach (string frag in frags)
            {
                //string frag = Resource.m_spellingChecker.correct(frag_2,true);
                //frag = frag_2;
                List<string> words = stem.GetBaseFormSentence(frag).Split().ToList();
                Tuple<int, int, bool> indicatorIdx = IndicatorIndex(words);
                if (indicatorIdx.Item2 == -1)
                {
                    finalQuery.Add(selectIdf(words, idfThresholdHigh));
                }
                else
                {
                    List<string> wordsHigh = words.Take(indicatorIdx.Item1).ToList();
                    List<string> wordsLow = words.Skip(indicatorIdx.Item2).ToList();
                    finalQuery.Add(selectIdf(wordsHigh, idfThresholdHigh));
                    if (indicatorIdx.Item3)
                        finalQuery.Add("not");
                    finalQuery.Add(selectIdf(wordsLow, idfThresholdLow));
                }
                //finalQuery.Add(" # ");
            }
            m_hTerms.Clear();
            return string.Join(" ",finalQuery).Trim();
        }
        private static string selectIdf(List<string> words, double idfth)
        {
            List<string> ret = new List<string>();
            int[] selected = new int[words.Count];
            for (int i = 0; i < selected.Length; i++)
            {
                selected[i] = 0;
            }

            for (int i = 0; i < words.Count;i++)
            {
                if (Resource.m_dIdf.ContainsKey(words[i]))
                {
                    if (Resource.m_dIdf[words[i]] > idfth)
                    {
                        selected[i] = 1;
                    }
                }
                //else selected[i] = 1;
            }

            
            for (int i = 0; i < words.Count; i++)
            {
                if (selected[i] == 1 && !Resource.m_hStopWords.Contains(words[i]))
                    ret.Add(words[i]);
            }
            return string.Join(" ", ret);
        }
        private static List<string> Seperate(string input)
        {
            string[] seperator = new string[] { ";", "?", "!", ":", "."};
            List<string> ret = input.Trim().Split(seperator, StringSplitOptions.RemoveEmptyEntries).ToList();

            return ret;
        }

        private static Tuple<int, int, bool> IndicatorIndex(List<string> words)
        {
            int beginIndex = -1;
            int endIndex = -1;
            bool hasNot = false;
            int ngram = 3;
            List<Tuple<int, int>> wordsNgramList = new List<Tuple<int, int>>();

            for(int k = ngram; k > 0; k--)
            {
                for (int i = 0; i < words.Count && i + k <= words.Count; i++ )
                {
                    wordsNgramList.Add(new Tuple<int, int>(i, k));
                }
            }

            foreach(var ngramWord in wordsNgramList)
            {
                string phrase = string.Join(" ", words.Skip(ngramWord.Item1).Take(ngramWord.Item2));
                if(Resource.m_hVerbSet.Contains(phrase))
                {
                    beginIndex = ngramWord.Item1;
                    endIndex = beginIndex + ngramWord.Item2;
                    if (phrase.IndexOf("not") != -1 || phrase.IndexOf(@"n't") != -1)
                        hasNot = true;
                    break;
                }
            }

            return new Tuple<int, int, bool>(beginIndex, endIndex, hasNot);
        }
        public static int[] SelectTerms2(List<string> words, double idfThreshold)
        {
            int[] selected = new int[words.Count];
            for (int i = 0; i < selected.Length; i++)
            {
                selected[i] = 0;
            }
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
                        if (!m_hTerms.Contains(phrase))
                        {
                            m_hTerms.Add(phrase);
                            for (int j = i; j < i + k; j++)
                            {
                                selected[j] = 1;
                            }
                        }
                        break;
                    }
                }
                i += Math.Max(k, 1);
            }
            for (int i = 0; i < words.Count; )
            {
                int k;
                for (k = ngram; k > 0; k--)
                {
                    if (i + k > words.Count)
                        continue;
                    string phrase = string.Join(" ", words.Skip(i).Take(k));
                    if (Resource.m_moveList.Contains(phrase))
                    {
                        for (int j = i; j < i + k; j++)
                        {
                            selected[j] = -1;
                        }
                        break;
                    }
                }
                i += Math.Max(k, 1);
            }
            return selected;
        }
        static bool canAdd(List<string> word,int[] selected, int index)
        {
            return pet.canAdd(word,selected,index);

        }
        private static string SelectTerms(List<string> words, double idfThreshold)
        {
            List<string> ret = new List<string>();
            int[] selected = new int[words.Count];
            for(int i = 0; i < selected.Length; i++)
            {
                selected[i] = 0;
            }
            int ngram = 4;

            for (int i = 0; i < words.Count; )
            {
                int k;
                for(k = ngram; k > 0; k--)
                {
                    if (i + k > words.Count)
                        continue;
                    string phrase = string.Join(" ", words.Skip(i).Take(k));
                    if(Resource.m_hTaxonomyKeys.Contains(phrase))
                    {
                        if(!m_hTerms.Contains(phrase))
                        {
                            m_hTerms.Add(phrase);
                            for(int j = i; j < i + k; j++)
                            {
                                selected[j] = 1;
                            }
                        }
                        else
                        {
                            for(int j = i; j < i + k; j++)
                            {
                                selected[j] = 2;
                            }
                        }
                        break;
                    }
                }
                i += Math.Max(k, 1);
            }

            for (int i = 0; i < words.Count; i++ )
            {
                if(selected[i] == 0 && !Resource.m_hStopWords.Contains(words[i]) && !m_hTerms.Contains(words[i]) )
                {
                    if(Resource.m_dIdf.ContainsKey(words[i]))
                    {
                        if(Resource.m_dIdf[words[i]] > idfThreshold)
                        {
                            selected[i] = 1;
                        }
                    }
                    else
                    {
                        selected[i] = 1;
                    }
                    if (selected[i] == 1)
                        m_hTerms.Add(words[i]);
                }
            }
            for(int i=0;i<words.Count;i++)
            {
                if (words[i].CompareTo("unable")==0)
                    selected[i] = 1;
            }
            int[] selectedBack= new int[selected.Length];
            for (int i = 0; i < selected.Length; i++) selectedBack[i] = selected[i];
            /*for (int j = 0; j < words.Count(); j++)
            {
                if (selected[j] == 0 && canAdd(words, selectedBack, j))
                {
                    selected[j] = 1;
                }
            }   */
            for (int i = 0; i < words.Count; i++ )
            {
                if (selected[i] == 1 && !Resource.m_hStopWords.Contains(words[i]))
                    ret.Add(words[i]);
            }
            return string.Join(" ", ret);
        }
    }
}
