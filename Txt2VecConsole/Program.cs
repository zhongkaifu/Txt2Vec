using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Txt2VecConsole
{
    class Program
    {
        static int ArgPos(string str, string[] args)
        {
            int a;
            for (a = 0; a < args.Length; a++)
            {
                if (str == args[a])
                {
                    if (a == args.Length - 1)
                    {
                        Console.WriteLine("Argument missing for {0}", str);
                        return -1;
                    }
                    return a;
                }
            }
            return -1;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return;
            }

            for (int a = 0;a < args.Length;a++)
            {
                args[a] = args[a].ToLower();
            }

            string strRunMode = null;
            int i;
            if ((i = ArgPos("-mode", args)) >= 0) strRunMode = args[i + 1].ToLower();
            if (strRunMode == null)
            {
                Console.WriteLine("Failed: must to set running mode by -mode <train/distance>");
                Usage();
                return;
            }

            if (strRunMode == "distance" || strRunMode == "analogy")
            {
                DistanceAnalogyMode(args, strRunMode);
            }
            else if (strRunMode == "train")
            {
                TrainMode(args);
            }
            else if (strRunMode == "shrink")
            {
                ShrinkMode(args);
            }
            else if (strRunMode == "dump")
            {
                DumpMode(args);
            }
            else
            {
                Console.WriteLine("Failed: mode {0} isn't supported.", strRunMode);
            }
        }

        private static void ShrinkMode(string[] args)
        {
            int i;
            string strModelFileName = null;
            string strNewModelFileName = null;
            string strDictFileName = null;
            if ((i = ArgPos("-modelfile", args)) >= 0) strModelFileName = args[i + 1];
            if ((i = ArgPos("-newmodelfile", args)) >= 0) strNewModelFileName = args[i + 1];
            if ((i = ArgPos("-dictfile", args)) >= 0) strDictFileName = args[i + 1];

            if (strModelFileName == null)
            {
                Console.WriteLine("Failed： must to set the model file name");
                UsageShrink();
                return;
            }

            if (strNewModelFileName == null)
            {
                Console.WriteLine("Failed： must to set the new model file name");
                UsageShrink();
                return;
            }


            if (strDictFileName == null)
            {
                Console.WriteLine("Failed： must to set the dict file name");
                UsageShrink();
                return;
            }


            Txt2Vec.Shrink shrink = new Txt2Vec.Shrink();
            shrink.Run(strModelFileName, strNewModelFileName, strDictFileName);
        }



        private static void DumpMode(string[] args)
        {
            int i;
            string strModelFileName = null;
            string strTextFileName = null;
            if ((i = ArgPos("-modelfile", args)) >= 0) strModelFileName = args[i + 1];
            if ((i = ArgPos("-txtfile", args)) >= 0) strTextFileName = args[i + 1];

            if (strModelFileName == null)
            {
                Console.WriteLine("Failed： must to set the model file name");
                UsageDumpModel();
                return;
            }

            if (strTextFileName == null)
            {
                Console.WriteLine("Failed： must to set the text file name");
                UsageDumpModel();
                return;
            }

            Txt2Vec.Decoder decoder = new Txt2Vec.Decoder();
            decoder.LoadBinaryModel(strModelFileName);
            decoder.DumpModel(strTextFileName);
        }


        private static void DistanceAnalogyMode(string[] args, string strRunMode)
        {
            int i;
            string strModelFileName = null;
            int N = 40;
            bool bTxtFormat = false;

            if ((i = ArgPos("-txtmodel", args)) >= 0) bTxtFormat = (int.Parse(args[i + 1]) == 1) ? true : false;
            if ((i = ArgPos("-modelfile", args)) >= 0) strModelFileName = args[i + 1];
            if ((i = ArgPos("-maxword", args)) >= 0) N = int.Parse(args[i + 1]);


            if (strModelFileName == null)
            {
                Console.WriteLine("Failed: must to set the model file name");
                if (strRunMode == "distance")
                {
                    UsageDistance();
                }
                else
                {
                    UsageAnalogy();
                }
                return;
            }
            if (System.IO.File.Exists(strModelFileName) == false)
            {
                Console.WriteLine("Failed: model file {0} isn't existed.", strModelFileName);
                if (strRunMode == "distance")
                {
                    UsageDistance();
                }
                else
                {
                    UsageAnalogy();
                }
                return;
            }

            Txt2Vec.Decoder decoder = new Txt2Vec.Decoder();
            decoder.LoadModel(strModelFileName, bTxtFormat);

            while (true)
            {
                Console.WriteLine("Enter word or sentence (EXIT to break): ");
                string strLine = Console.ReadLine();
                if (strLine == "EXIT") break;

                string[] sents = strLine.Split('\t');

                List<Txt2Vec.Result> wsdRstList = null;
                if (strRunMode == "distance")
                {
                    if (sents.Length == 1)
                    {
                        wsdRstList = decoder.Distance(sents[0], N);
                        OutputResult(wsdRstList);
                    }
                    else
                    {
                        string[] terms1 = sents[0].Split();
                        string[] terms2 = sents[1].Split();

                        double score = decoder.Similarity(terms1, terms2);
                        Console.WriteLine("Similarity score: {0}", score);
                    }
                }
                else if (strRunMode == "analogy")
                {
                    string[] terms = strLine.Split();
                    Txt2Vec.TermOperation operation = Txt2Vec.TermOperation.ADD;
                    List<Txt2Vec.TermOP> termOPList = new List<Txt2Vec.TermOP>();
                    foreach (string item in terms)
                    {
                        if (item == "+")
                        {
                            operation = Txt2Vec.TermOperation.ADD;
                        }
                        else if (item == "-")
                        {
                            operation = Txt2Vec.TermOperation.SUB;
                        }
                        else
                        {
                            Txt2Vec.TermOP termOP = new Txt2Vec.TermOP();
                            termOP.strTerm = item;
                            termOP.operation = operation;
                            termOPList.Add(termOP);
                        }
                    }

                    wsdRstList = decoder.Distance(termOPList, N);

                    OutputResult(wsdRstList);
                }
            }
        }

        private static void OutputResult(List<Txt2Vec.Result> wsdRstList)
        {
            if (wsdRstList == null)
            {
                Console.WriteLine("No result.");
            }
            else
            {
                Console.WriteLine("Word                       Cosine distance");
                Console.WriteLine("------------------------------------------");
                foreach (Txt2Vec.Result item in wsdRstList)
                {
                    Console.WriteLine("{0}\t{1}", item.strTerm, item.score);
                }
            }
        }

        private static void TrainMode(string[] args)
        {
            Txt2Vec.Encoder encoder = new Txt2Vec.Encoder();

            string output_file = null;
            string train_file = null;
            string vocab_file = null;

            int i;
            if ((i = ArgPos("-vector-size", args)) >= 0) encoder.layer1_size = int.Parse(args[i + 1]);
            if ((i = ArgPos("-trainfile", args)) >= 0) train_file = args[i + 1];
            if ((i = ArgPos("-debug", args)) >= 0) encoder.debug_mode = int.Parse(args[i + 1]);
            if ((i = ArgPos("-cbow", args)) >= 0) encoder.cbow = int.Parse(args[i + 1]);
            if ((i = ArgPos("-alpha", args)) >= 0) encoder.starting_alpha = double.Parse(args[i + 1]);
            if ((i = ArgPos("-modelfile", args)) >= 0) output_file = args[i + 1];
            if ((i = ArgPos("-window", args)) >= 0) encoder.window = int.Parse(args[i + 1]);
            if ((i = ArgPos("-sample", args)) >= 0) encoder.sample = double.Parse(args[i + 1]);
            if ((i = ArgPos("-threads", args)) >= 0) encoder.num_threads = int.Parse(args[i + 1]);
            if ((i = ArgPos("-min-count", args)) >= 0) encoder.min_count = int.Parse(args[i + 1]);
            if ((i = ArgPos("-iter", args)) >= 0) encoder.iter = int.Parse(args[i + 1]);
            if ((i = ArgPos("-vocabfile", args)) >= 0) vocab_file = args[i + 1];
            if ((i = ArgPos("-negative", args)) >= 0) encoder.negative = int.Parse(args[i + 1]);
            if ((i = ArgPos("-pre-trained-modelfile", args)) >= 0) encoder.strPreTrainedModelFileName = args[i + 1];
            if ((i = ArgPos("-only-update-corpus-word", args)) >= 0) encoder.onlyUpdateCorpusWord = int.Parse(args[i + 1]);

            if (encoder.negative == 0)
            {
                Console.WriteLine("-negative must be larger than 0");
                return;
            }

            if (encoder.strPreTrainedModelFileName != null && ArgPos("-vector-size", args) >= 0)
            {
                Console.WriteLine("-pre-trained-modelfile cannot be used with -vector-size at the same time.");
                return;
            }


            if ((i = ArgPos("-save-step", args)) >= 0)
            {
                string str = args[i + 1].ToLower();
                if (str.EndsWith("k") == true)
                {
                    encoder.savestep = long.Parse(str.Substring(0, str.Length - 1)) * 1024;
                }
                else if (str.EndsWith("m") == true)
                {
                    encoder.savestep = long.Parse(str.Substring(0, str.Length - 1)) * 1024 * 1024;
                }
                else if (str.EndsWith("g") == true)
                {
                    encoder.savestep = long.Parse(str.Substring(0, str.Length - 1)) * 1024 * 1024 * 1024;
                }
                else
                {
                    encoder.savestep = long.Parse(str);
                }
            }

            if (train_file == null)
            {
                Console.WriteLine("-trainfile option is required");
                UsageTrain();
                return;
            }
            if (output_file == null)
            {
                Console.WriteLine("-modelfile option is required");
                UsageTrain();
                return;
            }

            Console.WriteLine("Alpha: {0}", encoder.starting_alpha);
            Console.WriteLine("CBOW: {0}", encoder.cbow);
            Console.WriteLine("Sample: {0}", encoder.sample);
            Console.WriteLine("Min Count: {0}", encoder.min_count);
            Console.WriteLine("Threads: {0}", encoder.num_threads);
            Console.WriteLine("Context Size: {0}", encoder.window);
            Console.WriteLine("Debug Mode: {0}", encoder.debug_mode);
            Console.WriteLine("Save Step: {0}K", encoder.savestep / 1024);
            Console.WriteLine("Iteration: {0}", encoder.iter);
            Console.WriteLine("Only Update Corpus Words: {0}", encoder.onlyUpdateCorpusWord);
            Console.WriteLine("Negative Examples: {0}", encoder.negative);
            if (encoder.strPreTrainedModelFileName != null)
            {
                Console.WriteLine("Pre-trained model file: {0}", encoder.strPreTrainedModelFileName);
            }
            else
            {
                Console.WriteLine("Vector Size: {0}", encoder.layer1_size);
            }

            encoder.TrainModel(train_file, output_file, vocab_file);

        }

        private static void Usage()
        {
            Console.WriteLine("Txt2VecConsole for Text Distributed Representation");
            Console.WriteLine("Specify the running mode:");
            Console.WriteLine("\t-mode <train/distance/analogy/shrink/dump>");
            Console.WriteLine("\t\t<train> is for training model");
            Console.WriteLine("\t\t<distance> is for calculating word similarity");
            Console.WriteLine("\t\t<analogy> is for word semantic analogy");
            Console.WriteLine("\t\t<shrink> is used to shrink down model");
            Console.WriteLine("\t\t<dump> is used to dump model to text format.");
        }

        private static void UsageTrain()
        {
            Console.WriteLine("Txt2VecConsole.exe -mode train <...>");
            Console.WriteLine("Parameters for training:");
            Console.WriteLine("\t-trainfile <file>");
            Console.WriteLine("\t\tUse text data from <file> to train the model");
            Console.WriteLine("\t-modelfile <file>");
            Console.WriteLine("\t\tUse <file> to save the resulting word vectors / word clusters");
            Console.WriteLine("\t-vector-size <int>");
            Console.WriteLine("\t\tSet vectorSize of word vectors; default is 200");
            Console.WriteLine("\t-window <int>");
            Console.WriteLine("\t\tSet max skip length between words; default is 5");
            Console.WriteLine("\t-sample <float>");
            Console.WriteLine("\t\tSet threshold for occurrence of words. Those that appear with higher frequency");
            Console.WriteLine(" in the training data will be randomly down-sampled; default is 0 (off), useful value is 1e-5");
            Console.WriteLine("\t-threads <int>");
            Console.WriteLine("\t\tUse <int> threads (default 1)");
            Console.WriteLine("\t-min-count <int>");
            Console.WriteLine("\t\tThis will discard words that appear less than <int> times; default is 5");
            Console.WriteLine("\t-alpha <float>");
            Console.WriteLine("\t\tSet the starting learning rate; default is 0.025");
            Console.WriteLine("\t-debug <int>");
            Console.WriteLine("\t\tSet the debug mode 0/1");
            Console.WriteLine("\t-cbow <int>");
            Console.WriteLine("\t\tUse the continuous bag of words model or skip-gram model; default is 1 (continuous bag of words model)");
            Console.WriteLine("\t-vocabfile <string>");
            Console.WriteLine("\t\tSave vocabulary into file <string>");
            Console.WriteLine("\t-save-step <int>");
            Console.WriteLine("\t\tSave model after every <int> words processed. it supports K, M and G for larger number");
            Console.WriteLine("\t-iter <int>");
            Console.WriteLine("\t\tRun more training iterations (default 5)");
            //Console.WriteLine("\t-sentence-vectors <0/1>");
            //Console.WriteLine("\t\tAssume the first token at the beginning of each line is a sentence ID. This token will be trained. Default is 0");
            //Console.WriteLine("\t\twith full sentence context instead of just the window. Use 1 to trun on.");
            Console.WriteLine("\t-negative <int>");
            Console.WriteLine("\t\tNumber of negative examples; default is 5, common value are 3 - 15");
            Console.WriteLine("\t-pre-trained-modelfile <file>");
            Console.WriteLine("\t\tUse <file> which is pre-trained-model file");
            Console.WriteLine("\t-only-update-corpus-word <0/1>");
            Console.WriteLine("\t\tUse 1 to only update corpus words, 0 to update all words");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("Training a model with corpus:");
            Console.WriteLine("Txt2VecConsole.exe -mode train -trainfile corpus.txt -modelfile vector.bin -vocabfile vocab.txt -debug 1 -vector-size 200 -window 5 -min-count 5 -sample 1e-4 -cbow 1 -threads 1 -save-step 100M -negative 15 -iter 5");
            Console.WriteLine("Training a model with pre-trained model：");
            Console.WriteLine("Txt2VecConsole.exe -mode train -trainfile corpus.txt -modelfile vector.bin -vocabfile vocab.txt -debug 1 -vector-size 200 -window 5 -min-count 5 -sample 1e-4 -threads 4 -save-step 100M -iter 100 -pre-trained-modelfile vector_trained.bin -only-update-corpus-word 1");


        }

        private static void UsageDistance()
        {
            Console.WriteLine("Txt2VecConsole.exe -mode distance <...>");
            Console.WriteLine("Parameters for calculating word similarity");
            Console.WriteLine("\t-modelfile <file>");
            Console.WriteLine("\t\tUse <file> to load encoded model");
            Console.WriteLine("\t-maxword <int>");
            Console.WriteLine("\t\t<int> is the number of closest words that will be shown");
            Console.WriteLine("\t-txtmodel <int>");
            Console.WriteLine("\t\tIndicate model format. Use 1 for text model, 0 for binary model. Default is 0");
        }

        private static void UsageAnalogy()
        {
            Console.WriteLine("Txt2VecConsole.exe -mode analogy <...>");
            Console.WriteLine("Parameters for word semantic analogy");
            Console.WriteLine("\t-modelfile <file>");
            Console.WriteLine("\t\tUse <file> to load encoded model");
            Console.WriteLine("\t-maxword <int>");
            Console.WriteLine("\t\t<int> is the number of closest words that will be shown");
            Console.WriteLine("\t-txtmodel <int>");
            Console.WriteLine("\t\tIndicate model format. Use 1 for text model, 0 for binary model. Default is 0");
        }

        private static void UsageShrink()
        {
            Console.WriteLine("Txt2VecConsole.exe -mode shrink <...>");
            Console.WriteLine("Parameters for shrinking down model");
            Console.WriteLine("\t-modelfile <file>");
            Console.WriteLine("\t\t<file> is the raw model for shrinking down");
            Console.WriteLine("\t-newmodelfile <file>");
            Console.WriteLine("\t\t<file> is for shrinked model");
            Console.WriteLine("\t-dictfile <file>");
            Console.WriteLine("\t\t<file> if lexical dictionary whose items will be kept in shrinked model");
        }

        private static void UsageDumpModel()
        {
            Console.WriteLine("Txt2VecConsole.exe -mode dump <...>");
            Console.WriteLine("Parameters for dumping model to text format");
            Console.WriteLine("\t-modelfile <file>");
            Console.WriteLine("\t\t<file> is the model file for dumping");
            Console.WriteLine("\t-txtfile <file>");
            Console.WriteLine("\t\t<file> is the text file that model dumped into");
        }

    }
}
