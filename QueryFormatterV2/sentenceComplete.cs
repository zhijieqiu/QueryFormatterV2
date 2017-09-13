using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.io;
using edu.stanford.nlp.process;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.parser.lexparser;
using Console = System.Console;
using System.Text.RegularExpressions;

namespace SupportAgent
{
    public class sentenceComplete
    {
        LexicalizedParser lp;
        PennTreebankLanguagePack tlp;
        GrammaticalStructureFactory gsf;
        public HashSet<string> Target = new HashSet<string>();
        public void init(string dir)
        {
            var jarRoot = dir + @"data\stanford-english-corenlp-2016-01-10-models\";
            var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";
            lp = LexicalizedParser.loadModel(modelsDirectory + @"\lexparser\englishRNN.ser.gz");
            tlp = new PennTreebankLanguagePack();
            gsf = tlp.grammaticalStructureFactory();
            Target = Resource.m_Target;
        }
        public void init()
        {
            var jarRoot =  @"data\stanford-english-corenlp-2016-01-10-models\";
            var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";
            lp = LexicalizedParser.loadModel(modelsDirectory + @"\lexparser\englishRNN.ser.gz");
            tlp = new PennTreebankLanguagePack();
            gsf = tlp.grammaticalStructureFactory();
            string targetfile =  @"data\target.txt";
            string[] tlines = System.IO.File.ReadAllLines(targetfile);
            foreach (string line in tlines)
            {
                if (!Target.Contains(line.ToLower())) Target.Add(line.ToLower());
            }
        }
        public Boolean isvb(string q)
        {
            List<string> ls = new List<string>() { "VB", "VBD", "VBG", "VBN", "VBP", "VBZ" };
            if (ls.Contains(q)) return true;
            else return false;
        }
        public Tree parsequery(string query)
        {
            var tokenizerFactory = PTBTokenizer.factory(new CoreLabelTokenFactory(), "");
            var sent2Reader = new StringReader(query);
            var rawWords2 = tokenizerFactory.getTokenizer(sent2Reader).tokenize();
            sent2Reader.close();
            var tree2 = lp.apply(rawWords2);
            return tree2;
        }
        public Tuple<int, int> numbervp(Tree tree)
        {
            //var jarRoot = @"C:\Users\v-chuhz\Parser\stanford-english-corenlp-2016-01-10-models\";
            //var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";

            //// Loading english PCFG parser from file
            //var lp = LexicalizedParser.loadModel(modelsDirectory + @"\lexparser\englishRNN.ser.gz");
            string stree = tree.toString();
            int count = stree.Split(new string[] { "VP" }, StringSplitOptions.None).Length - 1;

            //isvobject
            int i = -1;
            int vo = 0;
            while ((i = (stree.IndexOf("(VP", i + 1))) >= 0)
            {
                int j = i + 1;
                int flag = 1;
                while (flag != 0)
                {
                    if (stree[j] == '(') flag++;
                    else if (stree[j] == ')') flag--;
                    j++;
                }
                string temp = stree.Substring(i, j - i);
                if (temp.IndexOf("NP") != -1 && temp.IndexOf("VP", 3) == -1)
                {
                    string[] seperator = new string[] { " ", "(", ")" };
                    string[] parts = temp.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string target in Target)
                    {
                        if (parts.Contains(target)) vo++;
                    }
                }
            }
            Tuple<int, int> tup = new Tuple<int, int>(count, vo);
            return tup;
        }
        public int ifcomplete(Tree tree2)
        {
            string stree = tree2.toString();
            var gs = gsf.newGrammaticalStructure(tree2);
            var tdl = gs.typedDependenciesCCprocessed();
            Regex reg = new Regex(@"(\w+-\d)");
            for (int i = 0; i < tdl.size(); i++)
            {
                string str = tdl.get(i).ToString();
                if (str.Contains("nsubj"))
                {
                    try
                    {
                        Match matc = reg.Match(str);
                        string[] parts = matc.Value.Split("-".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        string sep = @"(\w+\s" + parts[0] + ")";
                        Regex re = new Regex(sep);
                        Match match = re.Match(stree);
                        string[] attr = match.Value.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (isvb(attr[0])) return 1;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }

            }
            return 0;

        }
    }
}
