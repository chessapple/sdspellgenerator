using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.engine
{
    public class GradioCall
    {
        public int funcId;
        protected Dictionary<string, int> paramIndexs;
        public object[] defaultParams;

        public void LoadFunc(int funcId, string funcName, dynamic? func, Dictionary<int, string> elemMap, Dictionary<int, object> defaultValueMap)
        {
            paramIndexs = new Dictionary<string, int>();
            defaultParams = new object[func.inputs.Count];
            System.Diagnostics.Debug.WriteLine("Func " + funcName + "(");
            for (int i = 0; i < func.inputs.Count; i++)
            {
                string logStr = "   v" + i + " " + (int)func.inputs[i];
                //System.Diagnostics.Debug.WriteLine("   v" + i + " " + (int)func.inputs[i] + (elemMap.ContainsKey((int)func.inputs[i]) ? elemMap[(int)func.inputs[i]] : "") + (defaultValueMap.ContainsKey((int)func.inputs[i]) ? defaultValueMap[(int)func.inputs[i]] : ""));
                if (elemMap.ContainsKey((int)func.inputs[i]))
                {
                    paramIndexs[elemMap[(int)func.inputs[i]]] = i;
                    logStr += " " + elemMap[(int)func.inputs[i]];
                    //System.Diagnostics.Debug.WriteLine("   "+ elemMap[(int)func.inputs[i]] + ",");
                }

                if (defaultValueMap.ContainsKey((int)func.inputs[i]))
                {
                    defaultParams[i] = defaultValueMap[(int)func.inputs[i]];
                    logStr += " " + defaultParams[i];
                }
                System.Diagnostics.Debug.WriteLine(logStr.Replace('\n', ' ').Replace('\r', ' '));
            }
            System.Diagnostics.Debug.WriteLine(")");
        }
    }
}
