using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.batch
{
    public class BAChooseModel : BatchAction
    {
        public string model;
        public BAChooseModel(string model)
        {
            this.model = model;
        }

        public override string Description  { get => "切换模型"+model;}

        public override async Task Run()
        {
            try
            {
                await AppCore.Instance.GetGenerateEngine().ChooseModelDo(model);
                success = true;
            }catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
    }
}
