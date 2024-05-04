using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SpellGenerator.app.file;

namespace SpellGenerator.app.batch
{
    public class BatchGenController
    {
        public string outputPath;
        public int roundCount = 1;
        public int imageNumPerRound;
        public int restTime;
        public int roundSwitch;
        public List<string> models;
        public List<SamplingMethod> algorithms;
        public bool useRefImage;
        public GenImageInfo? refImage;
        public List<GenImageInfo> imageResults;
        protected GenConfig baseGenConfig;
        protected DialogBatchGenStatus dialogBatchGenStatus;

        protected long totalTime;
        protected int round;
        public bool stop = false;

        public void Run()
        {
            baseGenConfig = new GenConfig();
            baseGenConfig.CopyFrom(AppCore.Instance.genConfig);
            dialogBatchGenStatus = new DialogBatchGenStatus();
            dialogBatchGenStatus.Owner = (Application.Current.MainWindow as MainWindow);
            dialogBatchGenStatus.batchGenController = this;
            dialogBatchGenStatus.Show();
            stop = false;
            (Application.Current.MainWindow as MainWindow).IsEnabled = false;

            _ = RunAsync();
        }

        async Task DoAction(BatchAction action)
        {
            action.controller = this;
            dialogBatchGenStatus.SetStatus("正在" + action.Description + "...");
            System.Diagnostics.Debug.WriteLine(action.Description);
            await action.Run();
            if(!action.success)
            {
                int retryCount = 1;
                do
                {
                    if (stop)
                    {
                        break;
                    }

                    dialogBatchGenStatus.SetStatus(action.Description + "失败，将于10秒后重试，第" + retryCount + "次...");
                    await Task.Delay(10000);
                    retryCount++;
                    await action.Run();

                } while (!action.success);
            }
        }

        async Task RunAsync()
        {
            long startSeed = baseGenConfig.seed;
            if(startSeed == -1)
            {
                startSeed = new Random().Next();
            }
            int totalRoundCount = roundCount * models.Count * algorithms.Count;
            int perIterRound = models.Count * algorithms.Count;
            string positivePrompt = AppCore.Instance.activePositivePrompt.spell;
            string negativePrompt = AppCore.Instance.activeNegativePrompt.spell;
            totalTime = 0;
            round = 0;
            long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long pRound = 0;
            dialogBatchGenStatus.SetProgress("已完成轮次0/" + totalRoundCount, 0);
            while(pRound < roundCount)
            {
                int sRound = 0;
                foreach (string model in models)
                {
                    long mSeed = startSeed + pRound*imageNumPerRound;
                    int mRound = 0;
                    int tRound = 0;
                    if(AppCore.Instance.GetGenerateEngine().CanChooseModel() && (models.Count > 0 || pRound == 0))
                    {
                        await DoAction(new BAChooseModel(model));
                    }

                    if (stop)
                    {
                        break;
                    }
                    while (mRound < roundSwitch && tRound + pRound < roundCount)
                    {
                        foreach (var samplingMethod in algorithms)
                        {
                            GenConfig genConfig = new GenConfig();
                            genConfig.CopyFrom(baseGenConfig);
                            genConfig.samplingMethod = samplingMethod.webUIName;
                            genConfig.seed = mSeed + tRound * imageNumPerRound;

                            if(useRefImage)
                            {
                                await DoAction(new BAImg2Img(genConfig, refImage, imageNumPerRound, positivePrompt, negativePrompt));
                            }
                            else
                            {
                                await DoAction(new BATxt2Img(genConfig, imageNumPerRound, positivePrompt, negativePrompt));
                            }
                            await DoAction(new BASaveImages(imageResults, genConfig.seed, model, samplingMethod.webUIName, outputPath));

                            round++;

                            if (stop)
                            {
                                break;
                            }
                            if(restTime>0)
                            {
                                await DoAction(new BARest(restTime));
                            }
                            if (stop)
                            {
                                break;
                            }
                            long nT = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;
                            long eltimateTime = nT * (totalRoundCount - round) / round;
                            TimeSpan span = TimeSpan.FromMilliseconds(eltimateTime);
                            string timeStr = span.TotalHours+":"+span.ToString(@"mm\:ss");
                            dialogBatchGenStatus.SetProgress("已完成轮次"+round+"/" + totalRoundCount+"，预估剩余"+timeStr, ((double)round)*100/totalRoundCount);
                        }
                        mRound += algorithms.Count;
                        tRound++;
                    }
                    if(sRound == 0)
                    {
                        sRound = tRound;
                    }
                }
                pRound += sRound;
                if (stop)
                {
                    break;
                }
            }

            (Application.Current.MainWindow as MainWindow).IsEnabled = true;
            dialogBatchGenStatus.canClose = true;
            dialogBatchGenStatus.Close();
        }
    }
}
