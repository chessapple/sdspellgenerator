
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.controller
{
    public class AppController
    {
        public static AppController Instance { get; } = new AppController();

        public delegate void CommonEvent();
        public event CommonEvent? onEngineBaseDataLoadedChange;
        public void InvokeOnEngineBaseDataLoadedChange()
        {
            onEngineBaseDataLoadedChange?.Invoke();
        }
    }
}
