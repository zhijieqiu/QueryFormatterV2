using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ICEClassifierLocalForSupport;
namespace SupportAgent
{
    public class feature
    {
        oldtaxonomy old;
        Parser pr;
        public void init(string dir)
        {
            old = new oldtaxonomy();
            pr = new Parser();
            string path = dir+@"data\taxonomy.txt";
            string dirpath = dir + "data";
            old.LoadTaxonomy(path);
            old.load(dirpath);
        }
        public void init()
        {
            old= new oldtaxonomy();
            pr = new Parser();
            string path = @"data\taxonomy.txt";
            string dir = @"data";
            old.LoadTaxonomy(path);
            old.load(dir);
        }
        public int oldfea(List<string> query)
        {
            //List<string> query = new List<string>() { "i want to change the subscription from small business premium to office 365 business" };          
            Issue issue = new Issue(query);
            pr.convert(ref issue);
            string result = old.getQuestion(issue);
            
            if (result.Length == 0) return 1;
            else return 0;
        }
        public class Issue
        {
            public List<string> query;
            public int themeId;
            public int m_nProductId;
            public Issue(List<string> q)
            {
                query = q;
                themeId = 0;
            }
        }

        public static Dictionary<int, Tuple<int, List<int>, string, List<string>, string, HashSet<string>>> m_DTopicTree = new Dictionary<int, Tuple<int, List<int>, string, List<string>, string, HashSet<string>>>();
        public static ICEClassifierLocalForSupport.LocalClassifier m_ICETheme;
        public static ICEClassifierLocalForSupport.LocalClassifier m_ICEProduct;
        public static Dictionary<int, string> m_dProductIds = new Dictionary<int, string>();

        public class ICEProduct
        {
            public List<int> Predict(Issue issue) //return the top one themeid
            {
                // save matched theme to class member variables
                string intput = String.Join(" ", issue.query.ToArray());
                List<Tuple<int, double>> preds = Classify(intput);

                List<int> result = new List<int>();
                int count = 0;
                foreach (var item in preds)
                {
                    if (count < 5)
                    {
                        result.Add(item.Item1);
                    }
                    count++;
                }
                return result;
                // 5:0.9812444 10:0.7877271 1:0.7445234 2:0.6496791 9:0.4155455 14:0.35734 26:0.2305129 3:0.2250429 32:0.1457802 21:0.1063464
            }

            public List<Tuple<int, double>> Classify(string query)
            {
                string result = "";
                try
                {
                    result = m_ICEProduct.Classify(query, 5, 0.01);
                    // first is your query, second is the K classes you want, third is the threshold of score
                }
                catch
                {
                    result = "";
                }

                List<Tuple<int, double>> predictions = new List<Tuple<int, double>>();
                // 5:0.9812444 10:0.7877271 1:0.7445234 2:0.6496791 9:0.4155455 14:0.35734 26:0.2305129 3:0.2250429 32:0.1457802 21:0.1063464

                string[] seperator1 = new string[] { " " };
                string[] seperator2 = new string[] { ":" };

                string[] parts = result.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string[] items = part.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length >= 2)
                    {
                        predictions.Add(Tuple.Create(Convert.ToInt32(items[0]), Convert.ToDouble(items[1])));
                    }

                }

                return predictions;
            }
        } // class end
        public class ICETheme
        {
            public List<int> Predict(Issue issue, string productPredicted) //return the top one themeid
            {
                // save matched theme to class member variables
                string intput = String.Join(" ", issue.query.ToArray());
                if (productPredicted.Length > 0)
                {
                    intput += " ";
                    intput += productPredicted;
                }

                // call ICE to classify
                List<Tuple<int, double>> preds = Classify(intput);

                // parse ICE predictions
                List<int> result = new List<int>();
                int count = 0;
                foreach (var item in preds)
                {
                    if (count < 5)
                    {
                        result.Add(item.Item1);
                    }
                    count++;
                }
                return result;
                // 5:0.9812444 10:0.7877271 1:0.7445234 2:0.6496791 9:0.4155455 14:0.35734 26:0.2305129 3:0.2250429 32:0.1457802 21:0.1063464
            }

            public List<Tuple<int, double>> Classify(string query)
            {
                string result = "";
                try
                {
                    result = m_ICETheme.Classify(query, 5, 0.01);
                    // first is your query, second is the K classes you want, third is the threshold of score
                }
                catch
                {
                    result = "";
                }

                List<Tuple<int, double>> predictions = new List<Tuple<int, double>>();
                // 5:0.9812444 10:0.7877271 1:0.7445234 2:0.6496791 9:0.4155455 14:0.35734 26:0.2305129 3:0.2250429 32:0.1457802 21:0.1063464

                string[] seperator1 = new string[] { " " };
                string[] seperator2 = new string[] { ":" };

                string[] parts = result.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string[] items = part.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length >= 2)
                    {
                        predictions.Add(Tuple.Create(Convert.ToInt32(items[0]), Convert.ToDouble(items[1])));
                    }
                }

                return predictions;
            }
        } // class end

        public class Parser
        {
            ICETheme Theme;
            ICEProduct Product;
            public Parser()
            {
                Theme = new ICETheme();
                Product = new ICEProduct();
            }
            public bool convert(ref Issue issue)
            {
                List<int> productIds = Product.Predict(issue);
                if ((productIds != null) && productIds.Count > 0)
                {
                    // return top 5 theme id
                    issue.m_nProductId = productIds.First(); //if fail, m_nThemeId=default value 0
                }
                else
                {
                    issue.m_nProductId = 0;
                }

                string productPredicted = "";
                if (m_dProductIds.ContainsKey(issue.m_nProductId) == true)
                {
                    productPredicted = m_dProductIds[issue.m_nProductId];
                }

                // Theme classification
                List<int> themes = Theme.Predict(issue, productPredicted);
                if ((themes != null) && themes.Count > 0)
                {
                    // return top 5 theme id
                    issue.themeId = themes.First(); //if fail, m_nThemeId=default value 0
                }
                else
                {
                    issue.themeId = 0;
                }
                return true;
            }
        }
        // class begin
        public class oldtaxonomy
        {

            public Boolean load(string dir)
            {
                bool success = true;
                ICEClassifierLocalForSupport.LocalClassifier.Init(dir + @"\ice");
                // init theme classifier with models
                m_ICETheme = ICEClassifierLocalForSupport.LocalClassifier.NewObject(dir + @"\ice\theme");
                m_ICEProduct = ICEClassifierLocalForSupport.LocalClassifier.NewObject(dir + @"\ice\product");
                success &= LoadProductIDs(dir + @"\ice\product\produc.label.txt");
                return success;
            }
            public bool LoadProductIDs(string path)
            {
                try
                {
                    StreamReader sr = new StreamReader(path);
                    if (sr != null)
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine().Trim();
                            if (line.Length > 0)
                            {
                                string[] tokens = line.Split('\t');
                                if (tokens.Length == 0)
                                {
                                    int producId = 0;
                                    Int32.TryParse(tokens[0], out producId);
                                    m_dProductIds.Add(producId, tokens[1]);
                                }
                            }
                        }
                        sr.Close();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError("Product ID dictionary loading exception: {0}", e.Message);
                    return false;
                }
            }
  
            
            public bool LoadTaxonomy(string path)
            {
                try
                {
                    StreamReader srTopicTree = new StreamReader(path);
                    if (srTopicTree != null)
                    {
                        while (!srTopicTree.EndOfStream)
                        {
                            string line = srTopicTree.ReadLine();
                            string[] tabseperator = new string[] { "\t" };
                            string[] items = line.Split(tabseperator, StringSplitOptions.None);

                            if (items.Length >= 7)
                            {
                                string[] seperator = new string[] { "," };
                                //id pid cid(list) topic attribute(list) question value(list)
                                // node id
                                int id = Convert.ToInt32(items[0]);

                                // parent id
                                int pid = items[1] == "" ? -1 : Convert.ToInt32(items[1].Trim());

                                // children id list
                                List<int> cid = new List<int>();
                                if (items[2].Length != 0)
                                {
                                    string[] parts = items[2].Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var part in parts)
                                    {
                                        cid.Add(Convert.ToInt32(part.Trim()));
                                    }
                                }

                                // topic label
                                string topic = items[3].ToLower();
                                int index = topic.IndexOf(":");
                                if (index >= 0)
                                {
                                    topic = topic.Substring(index + 1, topic.Length - (index + 1));
                                }

                                // attribute name
                                List<string> attribute = items[5].Length == 0 ? new List<string>() : new List<string>(items[5].Split(seperator,
                                    StringSplitOptions.RemoveEmptyEntries));

                                // question
                                string question = items[6];

                                // keyword list
                                HashSet<string> value = new HashSet<string>();
                                if (items[4].Length != 0)
                                {
                                    string[] parts = items[4].Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var part in parts)
                                    {
                                        if (!value.Contains(part))
                                        {
                                            value.Add(part);
                                        }
                                    }
                                }

                                m_DTopicTree.Add(id, Tuple.Create(pid, cid, topic, attribute, question, value));
                            }
                        }
                        srTopicTree.Close();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError("Taxonomy dictionary loading exception: {0}", e.Message);
                    return false;
                }
            }//end

            public string getQuestion(Issue issue)  //get question and update the currentid and keywords
            {
                string question = "";
                // input desc and themeid, get nextnode and update the keywords
                int nextnode = getNextOneNode(issue);
                // get child node 
                //=================
                //===the api I want====
                //==================
                if (getAllChildren(nextnode).Count == 1) //is leaf
                {
                    question = "";
                }
                else //nonleaf
                {
                    question = "asking question";
                }
                return question;
            }
            public int getNextOneNode(Issue issue )
            {
                int nextnode = 0; //init if no one found,return the theme

                Dictionary<int, int> matchedtimes = new Dictionary<int, int>();
                Dictionary<int, HashSet<string>> matchedNodes = MatchKeyword(issue.query,issue.themeId , ref matchedtimes);
                //get matched id and keywords, and the matched times  

                int matchedCount = matchedNodes.Count;
                if (matchedCount == 0)// -1:no one found
                {
                    return nextnode;
                }
                else if (matchedCount == 1) //just match one
                {
                    nextnode = matchedNodes.First().Key;
                }
                else
                {
                    // find the nextnode from several nodes through it's value    
                    Dictionary<int, int> nodevalue = new Dictionary<int, int>();
                    foreach (var node in matchedtimes)
                    {
                        int value = 0;
                        int currentnode = node.Key;
                        while (currentnode != issue.themeId)
                        {
                            if (matchedtimes.ContainsKey(currentnode))
                            {
                                value += matchedtimes[currentnode];
                            }
                            currentnode = m_DTopicTree[currentnode].Item1;
                        }

                        nodevalue.Add(node.Key, value);
                    }

                    Dictionary<int, int> sortnodevalue = nodevalue.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
                    List<int> matchednodesid = new List<int>();
                    int maxvalue = sortnodevalue.First().Value;
                    foreach (var node in sortnodevalue)
                    {
                        if (node.Value == maxvalue)
                        {
                            matchednodesid.Add(node.Key);
                        }
                    }

                    if (matchednodesid.Count == 1)
                    {
                        nextnode = matchednodesid.First();
                    }
                    else
                    {
                        nextnode = getLeastParent(matchednodesid);
                    }
                }//end else


                return nextnode;
            }

            public Dictionary<int, HashSet<string>> MatchKeyword(List<string> query, int themeid, ref Dictionary<int, int> matchedidandtimes) //matched nodes id and matched keywords
            {
                Dictionary<int, HashSet<string>> matchednodes = new Dictionary<int, HashSet<string>>();
                foreach (string input in query)
                {
                    char[] spaceSpliter = { ' ' };
                    string[] words = input.Split(spaceSpliter, StringSplitOptions.RemoveEmptyEntries);
                    int ngram = 4;// word ngram=4
                    for (int i = 0; i < words.Length; i++)
                    {
                        List<string> wordNgramlist = new List<string>();
                        int k = 0;
                        for (int j = 0; (i + j < words.Length) && k < ngram; j++)
                        {
                            wordNgramlist.Add(words[i + j]);
                            k++;
                        }

                        do
                        {
                            string wordNgram = List2String(wordNgramlist);
                            int matchedCount = 0;

                            foreach (var child in getAllChildren(themeid))
                            {
                                if (m_DTopicTree[child].Item6.Contains(wordNgram))
                                {
                                    matchedCount++;
                                    HashSet<string> keywords = new HashSet<string>();
                                    keywords.Add(wordNgram);
                                    if (!matchednodes.ContainsKey(child))
                                    {
                                        matchednodes.Add(child, keywords);
                                        //matched times of the node
                                        matchedidandtimes.Add(child, 1);
                                    }
                                    else
                                    {
                                        //update matched times of the node
                                        matchedidandtimes[child]++;
                                        if (!matchednodes[child].Contains(wordNgram))
                                        {
                                            matchednodes[child].Add(wordNgram);
                                        }
                                    }
                                }
                            }

                            if (matchedCount > 0)
                            {
                                break;
                            }
                            wordNgramlist.RemoveAt(wordNgramlist.Count - 1);
                        } while (wordNgramlist.Count > 0);

                        if (wordNgramlist.Count > 1) //==0,no one found, continue, ==1, next one
                        {
                            i = i + wordNgramlist.Count - 1;
                        }
                    }//end for

                }//end foreach        
                return matchednodes;
            }

            private string List2String(List<string> wordNgramlist)
            {
                if (wordNgramlist == null || wordNgramlist.Count == 0)
                {
                    return "";
                }
                string result = "";
                foreach (var item in wordNgramlist)
                {
                    if (result != "")
                    {
                        result += " ";
                    }
                    result += item;
                }
                return result;
            }

            bool isChild(int id1, int id2) //whether id2 is the child of id1
            {
                while (id2 != 0)
                {
                    if (m_DTopicTree.ContainsKey(id2))
                    {
                        if (m_DTopicTree[id2].Item1 == id1)
                        {
                            return true;
                        }
                        else
                        {
                            id2 = m_DTopicTree[id2].Item1;
                        }
                    }
                    else
                    {
                        return false;
                    }

                }
                return false;
            }
            public HashSet<int> getAllChildren(int nodeid) //get all children including itselft
            {
                HashSet<int> result = new HashSet<int>();
                result.Add(nodeid);
                if (m_DTopicTree.ContainsKey(nodeid))
                {
                    Queue<int> q = new Queue<int>();

                    q.Enqueue(nodeid);

                    while (q.Count != 0)
                    {
                        int currentid = q.Dequeue();
                        if (m_DTopicTree[currentid].Item2.Count != 0)
                        {
                            foreach (var item in m_DTopicTree[currentid].Item2)
                            {
                                result.Add(item);
                                q.Enqueue(item);
                            }
                        }
                    }

                    return result;

                }

                return result;
            }

            int getLeastParent(List<int> children)
            {
                int parent = children[0];
                foreach (var child in children)
                {
                    if (child != parent)
                    {
                        parent = getLeastParent(parent, child);
                    }
                }
                return parent;
            }

            int getLeastParent(int child1, int child2)
            {
                if (isChild(child1, child2)) //child2 is the chiild of child1
                {
                    return child1;
                }

                if (isChild(child2, child1))
                {
                    return child2;
                }

                return getLeastParent(m_DTopicTree[child1].Item1, child2);

            }
            
            //public static void Main(string[] args)
            //{
            //    oldtaxonomy old = new oldtaxonomy();
            //    Parser pr = new Parser();
            //    string path = @"C:\Users\v-chuhz\SentenceClassifier\Query_classifier\taxonomy.txt";
            //    string dir=@"C:\Users\v-chuhz\Dev\SupportAgent";
            //    old.LoadTaxonomy(path);
            //    old.load(dir);
            //    List<string> query = new List<string>() { "i want to change the subscription from small business premium to office 365 business" };
            //    Issue issue = new Issue(query);               
            //    pr.convert(ref issue);
            //    string result = old.getQuestion(issue);
            //    if (result.Length == 0)
            //    {
            //        Console.WriteLine("1");
            //    }
            //    else
            //    {
            //        Console.WriteLine("0");
            //    }
            //    Console.ReadKey();
            //}
        }
    }

}
