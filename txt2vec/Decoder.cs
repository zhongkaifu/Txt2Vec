using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Txt2Vec
{
    public enum TermOperation { ADD, SUB };

    public class TermOP
    {
        public string strTerm;
        public TermOperation operation;
    }

    public class Result : IComparable<Result>
    {
        public string strTerm;
        public double score;

        public Result()
        {
            strTerm = null;
            score = -1;
        }

        int IComparable<Result>.CompareTo(Result other)
        {
            return other.score.CompareTo(score);
        }
    }

    public class Term
    {
        public string strTerm;
        public double[] vector;
    }

    public class Decoder
    {
        int BLOCK_N = 16;
        Dictionary<string, Term> term2vector;
        List<Term> entireTermList;
        public int vectorSize;
        object locker = new object();

        ParallelOptions parallelOption;
        public Decoder()
        {
            parallelOption = new ParallelOptions();
        }

        public string[] GetAllTerms()
        {
            return term2vector.Keys.ToArray();
        }

        public Term GetTerm(string strTerm)
        {
            if (term2vector.ContainsKey(strTerm) == false)
            {
                return null;
            }

            return term2vector[strTerm];
        }

        public bool DumpModel(string strFileName)
        {
            if (entireTermList == null || entireTermList.Count == 0)
            {
                return false;
            }

            StreamWriter sw = new StreamWriter(strFileName);
            foreach (Term term in entireTermList)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(term.strTerm);
                sb.Append("\t");

                foreach (double v in term.vector)
                {
                    sb.Append(v);
                    sb.Append("\t");
                }

                sw.WriteLine(sb.ToString().Trim());
            }
            sw.Close();

            return true;
        }

        public void LoadModel(string strFileName, bool bTextFormat)
        {
            if (bTextFormat == true)
            {
                LoadTextModel(strFileName);
            }
            else
            {
                LoadBinaryModel(strFileName);
            }
        }

        public void LoadTextModel(string strFileName)
        {
            term2vector = new Dictionary<string, Term>();
            entireTermList = new List<Term>();
            vectorSize = 0;

            StreamReader sr = new StreamReader(strFileName);
            string strLine = null;
            while ((strLine = sr.ReadLine()) != null)
            {
                //the format is "word \t vector
                Term term = new Term();
                string[] items = strLine.Split('\t');
                term.strTerm = items[0];

                string[] strVector = items[1].Split();
                term.vector = new double[strVector.Length];
                for (int i = 0; i < strVector.Length; i++)
                {
                    term.vector[i] = double.Parse(strVector[i]);
                }

                if (vectorSize > 0 && vectorSize != strVector.Length)
                {
                    throw new InvalidDataException(String.Format("Invalidated data : {0} . The length of vector must be fixed (current length {1} != previous length {2}).", strLine, strVector.Length, vectorSize));
                }

                vectorSize = strVector.Length;
                term.vector = NormalizeVector(term.vector);
                term2vector.Add(term.strTerm, term);
                entireTermList.Add(term);
            }

            sr.Close();
        }

        public void LoadBinaryModel(string strFileName)
        {
            StreamReader sr = new StreamReader(strFileName);
            BinaryReader br = new BinaryReader(sr.BaseStream);

            //The number of words
            int words = br.ReadInt32();

            //The size of vector
            vectorSize = br.ReadInt32();

            term2vector = new Dictionary<string, Term>();
            entireTermList = new List<Term>();

            for (int b = 0; b < words; b++)
            {
                Term term = new Term();
                term.strTerm = br.ReadString();
                term.vector = new double[vectorSize];

                for (int i = 0; i < vectorSize; i++)
                {
                    term.vector[i] = br.ReadSingle();
                }

                double len = 0;
                for (int i = 0; i < vectorSize; i++)
                {
                    len += term.vector[i] * term.vector[i];
                }
                len = Math.Sqrt(len);
                for (int i = 0; i < vectorSize; i++)
                {
                    term.vector[i] /= len;
                }

                term2vector.Add(term.strTerm, term);
                entireTermList.Add(term);

            }
            sr.Close();
        }

        public int GetVectorSize()
        {
            return vectorSize;
        }

        public double[] GetVector(string strTerm)
        {
            if (term2vector.ContainsKey(strTerm) == true)
            {
                return term2vector[strTerm].vector;
            }

            return null;
        }

        public double[] GetVector(List<TermOP> termList)
        {
            double[] vec = new double[vectorSize];

            //Calculate input terms' vector
            for (int b = 0; b < termList.Count; b++)
            {
                if (term2vector.ContainsKey(termList[b].strTerm) == true)
                {
                    Term term = term2vector[termList[b].strTerm];
                    if (termList[b].operation == TermOperation.ADD)
                    {
                        for (int a = 0; a < vectorSize; a++)
                        {
                            vec[a] += term.vector[a];
                        }
                    }
                    else if (termList[b].operation == TermOperation.SUB)
                    {
                        for (int a = 0; a < vectorSize; a++)
                        {
                            vec[a] -= term.vector[a];
                        }
                    }
                }
            }
            return vec;
        }

        private double[] NormalizeVector(double[] vec)
        {
            //Normalize the vector
            double len = 0;
            for (int a = 0; a < vectorSize; a++)
            {
                len += vec[a] * vec[a];
            }
            len = Math.Sqrt(len);
            for (int a = 0; a < vectorSize; a++)
            {
                vec[a] /= len;
            }

            return vec;
        }

        private List<TermOP> GenerateTermOP(string[] termList)
        {
            List<TermOP> termOPList = new List<TermOP>();
            foreach (string term in termList)
            {
                TermOP termOP = new TermOP();
                termOP.strTerm = term;
                termOP.operation = TermOperation.ADD;

                termOPList.Add(termOP);
            }

            return termOPList;
        }

        public double Similarity(string[] tokens1, string[] tokens2)
        {
            double score = 0;
            List<TermOP> termOPList1 = GenerateTermOP(tokens1);
            List<TermOP> termOPList2 = GenerateTermOP(tokens2);
            double[] vec1 = GetVector(termOPList1);
            double[] vec2 = GetVector(termOPList2);

            vec1 = NormalizeVector(vec1);
            vec2 = NormalizeVector(vec2);

            //Cosine distance
            for (int i = 0; i < vectorSize; i++)
            {
                score += vec1[i] * vec2[i];
            }

            return score;
        }

        public List<Result> Distance(string strTerm, int N = 40)
        {
            string[] termList = new string[1];
            termList[0] = strTerm;

            return Distance(termList, N);
        }

        //N is the number of closest words that will be shown
        public List<Result> Distance(string[] termList, int N = 40)
        {
            List<TermOP> termOPList = new List<TermOP>();
            foreach (string term in termList)
            {
                TermOP termOP = new TermOP();
                termOP.strTerm = term;
                termOP.operation = TermOperation.ADD;

                termOPList.Add(termOP);
            }

            return Distance(termOPList, N);
        }

        public List<Result> Distance(List<TermOP> termList, int N = 40)
        {
            long termCount = termList.Count;

            for (int i = 0; i < termCount; i++)
            {
                if (term2vector.ContainsKey(termList[i].strTerm) == false)
                {
                    //The term is OOV, no result
                    return null;
                }
            }


            //Calculate input terms' vector
            double[] vec = GetVector(termList);
            //Normalize the vector
            vec = NormalizeVector(vec);

            int candidateWordCount = entireTermList.Count;
            //Calculate the distance betweens words in parallel
            int size_per_block = candidateWordCount / BLOCK_N;
            List<Result> rstList = new List<Result>();
            Parallel.For<List<Result>>(0, BLOCK_N + 1, parallelOption, () => new List<Result>(), (k, loop, subtotal) =>
            {
                for (int c = (int)(k * size_per_block); c < (k + 1) * size_per_block && c < candidateWordCount; c++)
                {
                    //Calculate the distance
                    double dist = 0;
                    for (int a = 0; a < vectorSize; a++)
                    {
                        dist += vec[a] * entireTermList[c].vector[a];
                    }

                    //Save the result
                    Result rst = new Result();
                    rst.strTerm = entireTermList[c].strTerm;
                    rst.score = dist;

                    subtotal.Add(rst);
                }

                return subtotal;
            },
           (subtotal) => // lock free accumulator
           {
               //Mereg the result from different threads
               lock (locker)
               {
                   rstList.AddRange(subtotal);
               }
           });

            //Sort the result according the distance
            rstList.Sort();

            int maxN = Math.Min(N, rstList.Count);


            return rstList.GetRange(0, maxN);
        }

    }
}
