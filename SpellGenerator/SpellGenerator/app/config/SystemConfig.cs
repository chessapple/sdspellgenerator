using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.config
{
    public class BackendConfig
    {
        public string engine;
        public string name;
        public string host;
        public int port;
        public string version;
    }

    public class SystemConfig
    {
        public string backendIp;
        public int backendPort;
        public string novelAIBackendIp;
        public int novelAIBackendPort;
        public List<BackendConfig> backends;
    }
}
