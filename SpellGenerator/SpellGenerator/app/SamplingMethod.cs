using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app
{
    public class SamplingMethod
    {
        public string webUIName { get; set; }
        public string novelAIName;
        public SamplingMethod(string webUIName, string novelAIName)
        {
            this.webUIName = webUIName;
            this.novelAIName = novelAIName;
        }

        public static List<SamplingMethod> samplingMethods = new List<SamplingMethod>();
        private static Dictionary<string, SamplingMethod> samplingMethodsDictionary = new Dictionary<string, SamplingMethod>();

        public static SamplingMethod k_euler_a = new SamplingMethod("Euler a", "k_euler_ancestral");
        public static SamplingMethod k_euler = new SamplingMethod("Euler", "k_euler");
        public static SamplingMethod k_lms = new SamplingMethod("LMS", "k_lms");
        public static SamplingMethod k_heun = new SamplingMethod("Heun", "k_heun");
        public static SamplingMethod k_dpm_2 = new SamplingMethod("DPM2", "k_dpm_2");
        public static SamplingMethod k_dpm_2_a = new SamplingMethod("DPM2 a", "k_dpm_2_a");
        public static SamplingMethod k_dpmpp_2s_a = new SamplingMethod("DPM++ 2S a", "k_dpmpp_2s_a");
        public static SamplingMethod k_dpmpp_2m = new SamplingMethod("DPM++ 2M", "k_dpmpp_2m");
        public static SamplingMethod k_dpm_fast = new SamplingMethod("DPM fast", "k_dpm_fast");
        public static SamplingMethod k_dpm_ad = new SamplingMethod("DPM adaptive", "k_dpm_ad");
        public static SamplingMethod k_lms_ka = new SamplingMethod("LMS Karras", "k_lms_ka");
        public static SamplingMethod k_dpm_2_ka = new SamplingMethod("DPM2 Karras", "k_dpm_2_ka");
        public static SamplingMethod k_dpm_2_a_ka = new SamplingMethod("DPM2 a Karras", "k_dpm_2_a_ka");
        public static SamplingMethod k_dpmpp_2s_a_ka = new SamplingMethod("DPM++ 2S a Karras", "k_dpmpp_2s_a_ka");
        public static SamplingMethod k_dpmpp_2m_ka = new SamplingMethod("DPM++ 2M Karras", "k_dpmpp_2m_ka");
        public static SamplingMethod ddim = new SamplingMethod("DDIM", "ddim");
        public static SamplingMethod plms = new SamplingMethod("PLMS", "plms");

        public static SamplingMethod GetSamplingMethod(string methodName)
        {
            return samplingMethodsDictionary.GetValueOrDefault(methodName, null);
        }


        static SamplingMethod() {
            Init(new SamplingMethod[]
            {
                k_euler_a, k_euler, k_lms, k_heun, k_dpm_2, k_dpm_2_a, k_dpmpp_2s_a, k_dpmpp_2m, k_dpm_fast, k_dpm_ad, k_lms_ka, k_dpm_2_ka, k_dpm_2_a_ka, k_dpmpp_2s_a_ka, k_dpmpp_2m_ka, ddim, plms
            });
        }

        static void Init(SamplingMethod[] methods)
        {
            foreach(SamplingMethod method in methods)
            {
                samplingMethods.Add(method);
                samplingMethodsDictionary[method.webUIName] = method;
            }
        }

    }
}
