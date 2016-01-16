# Txt2Vec
Txt2Vec is a toolkit to represent text by vector. It's based on Google's word2vec project, but with some new features, such incremental training, model vector quantization and so on. For a specified term, phrase or sentence, Txt2vec is able to generate correpsonding vector according its semantics in text. And each dimension of the vector represents a feature. 

Txt2Vec is based on neural network for model encoding and cosine distance for terms similarity. Furthermore, Txt2Vec has fixed some issues of word2vec when encoding model in multiple-threading environment.

The following is the introduction about how to use console tool to train and use model. For API parts, I will update it later.

# Console tool
Txt2VecConsole tool supports four modes. Run the tool without any options, it will shows usage about modes.
Txt2VecConsole.exe  
 Txt2VecConsole for Text Distributed Representation  
 Specify the running mode:  
**-mode** <train/distance/analogy/shrink>  
<train> : train model to build vectors for words  
<distance> : calculating the similarity between two words  
<analogy> : multi-words semantic analogy  
<shrink> : shrink down the size of model  
<dump> : dump model to text format  
<buildvq> : build vector quantization model in text format  

# Train model
With train mode, you can train a word-vector model from given corpus. Note that, before you train the model, the words in training corpus should be word broken. The following are parameters for training mode  

Txt2VecConsole.exe -mode train <...>  
 Parameters for training:  
**-trainfile** <file> : Use text data from <file> to train the model  
**-modelfile** <file> : Use <file> to save the resulting word vectors / word clusters  
**-vector-size** <int> : Set size of word vectors; default is 200  
**-window** <int> : Set max skip length between words; default is 5  
**-sample** <float> : Set threshold for occurrence of words. Those that appear with higher frequency in the training data will be randomly down-sampled; default is 0 (off), useful value is 1e-5  
**-threads** <int> : the number of threads (default 1)  
**-min-count** <int> : This will discard words that appear less than <int> times; default is 5  
**-alpha** <float> : Set the starting learning rate; default is 0.025  
**-debug** <int> : Set the debug mode (default = 2 = more info during training)  
**-cbow** <int> : Use the continuous bag of words model; default is 0 (skip-gram model)  
**-vocabfile** <string> : Save vocabulary into file <string>  
**-save-step** <int> : Save model after every <int> words processed. it supports K, M and G for larger number  
**-iter** <int> : Run more training iterations (default 5)  
**-negative** <int> : Number of negative examples; default is 5, common value are 3 - 15  
**-pre-trained-modelfile** <file> : Use <file> which is pre-trained-model file  
**-only-update-corpus-word** <0/1> : Use 1 to only update corpus words, 0 to update all words  

Example:  
 Txt2VecConsole.exe -mode train -trainfile corpus.txt -modelfile vector.bin -vocabfile vocab.txt -debug 1 -vector-size 200 -window 5 -min-count 5 -sample 1e-4 -cbow 1 -threads 1 -save-step 100M -negative 15 -iter 5  

 After the training is finished. The tool will generate three files. vector.bin contains words and vector in binary format, vocab.txt contains all words with their frequency in given training corpus, and vector.bin.syn which is used for incremental model training in future.

# Incremental Model Training
After we collected some new corpus and new words, to get these new words' vector or update existing words' vector by new corpus, we need to re-train existing model in incremental model. Here is an example:  

Txt2VecConsole.exe -mode train -trainfile corpus_new.txt -modelfile vector_new.bin -vocabfile vocab_new.txt -debug 1 -window 10 -min-count 1 -sample 1e-4 -threads 4 -save-step 100M -alpha 0.1 -cbow 1 -iter 10 -pre-trained-modelfile vector_trained.bin -only-update-corpus-word 1  

We have already trained a model "vector_trained.bin" before, currently, we have collected some new corpus named "corpus_new.txt" and new words saved into "vocab_new.txt". The above command line will re-train existing model incrementally, and generate a new model file named "vector_new.bin". To get better result, the "alpha" value should be usually bigger than that in full corpus and vocabulary size training.  

Incremental model training is very useful for incremental corpus and new word. In this mode, we are able to generate new words vector aligned with existing words efficiently.  

# Calculating word similarity
With distance mode, you are able to calculate the similarity between two words. Here are parameters for this mode
Txt2VecConsole.exe -mode distance <...>  
 Parameters for calculating word similarity  
**-modelfile** <file> : encoded model needs to be loaded  
**-maxword** <int> : the maximum word number in result. Default is 40  

 After the model is loaded, you can input a word from console and then the tool will return the Top-N most similar words. Here is an example:  
Txt2VecConsole.exe -mode distance -modelfile wordvec.bin  
 Enter word or sentence (EXIT to break):  
手串  
Word Cosine distance  
 -----------------------------------------------------------------------------  
手串 1  
佛珠 0.918781571749997  
小叶紫檀 0.897870467450521  
手钏 0.868526208199693  
菩提子 0.85667693515943  
紫檀 0.855529437116288  
佛珠手链 0.849541378712106  
雕件 0.847901026881494  
砗磲 0.842016069107114  
小叶檀 0.839194380950776  
星月菩提子 0.838186634277951  
檀香木 0.837212392914782  
沉香木 0.83575322205817  
星月菩提 0.83494878072285  
黄花梨 0.831824567567293  
平安扣 0.831679080640205  
紫檀木 0.830029415546653  
小叶紫檀手串 0.82838028045219  
原籽 0.823008017930358  
玉髓 0.820901374489359  
手链 0.819296636344601  
和田玉 0.818727549748641  
绿檀 0.816645224342866  
和田碧玉 0.816260936449443  
菩提根 0.813263703640439  
血珀 0.808663718785608  
海南黄花梨 0.808161968264115  
天然玛瑙 0.807949867283673  
紫檀佛珠 0.80467756598682  
包浆 0.804356427867412  
石榴石 0.803866760918248  
小叶紫檀佛珠 0.803276658873406  
沉香手串 0.803169094160751  
绿松石 0.802508849725817  
玛瑙手镯 0.802038899418962  
象牙 0.800001887548549  
和田玉籽料 0.799139422922168  
牛毛纹 0.798210503136587  
岫玉 0.797258011387281  
发晶 0.797202309084502  

You can get demo package for Chinese in [DOWNLOADS] section.  

# Shrink model
Sometimes, the size of encoded model maybe too big, so you can use shrink mode to reduce its size according a given lexical dictionary. In shrink mode, any words with its vectors will be filter out if the word isn't in the given lexical dictionary. Here is the usage  

Txt2VecConsole.exe -mode shrink <...>  
 Parameters for shrinking down model  
**-modelfile** <file> : encoded model for shrinking down  
**-newmodelfile** <file> : shrinked model  
**-dictfile** <file> : lexical dictionary. Any words with its vector isn't in the dictionary will be filter out from the model  

# Dump model
Binary model format is not friendly for human to investigation, so Txt2VecConsole provides a command to dump binary model into text format. The command line as follows:  

Txt2VecConsole.exe -mode dump <...>  
 Parameters to dump encoded model to text format  
**-modelfile** <file> : encoded binary model needs to be dumped.  
**-txtfile** <file> : dumped model in text format  

# Build vector quantization model
model vector quantization is another way to reduce model size by converting weights from float type to byte type. Currently, Txt2VecConsole supports model vector quantization in 8bits. That means the model size will be reduced to 1/4 original model size. The command line as follows:  

Txt2VecConsole.exe -mode buildvq <...>  
 Parameters to build vector quantization model  
**-modelfile** <file> : encoded model file for vector quantization  
**-vqmodelfile** <file> : output vector quantized model  

# Demo Package
In release section, a Txt2VecConsole demo package is provided. It contains Txt2VecConsole.exe source code, binary files and two encoded models. Both of two models are word-to-vector models. One model is for Chinese and the other is for English. Please enjoy it and have fun! :)
