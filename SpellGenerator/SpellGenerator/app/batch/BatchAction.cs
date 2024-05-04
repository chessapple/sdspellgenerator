using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.batch
{
    public abstract class BatchAction
    {
        public BatchGenController controller;
        public abstract string Description { get; }
        public bool success = false;

        public abstract Task Run();
    }
}
