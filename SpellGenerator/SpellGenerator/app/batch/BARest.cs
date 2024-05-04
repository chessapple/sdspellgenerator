using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.batch
{
    public class BARest : BatchAction
    {
        public int restTime;
        public BARest(int restTime)
        {
            this.restTime = restTime;
        }

        public override string Description  { get => "休息"+restTime+"秒";}

        public override async Task Run()
        {
            await Task.Delay(restTime * 1000);
            success = true;
        }
    }
}
