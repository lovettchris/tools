﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FileTreeMap
{
    /// <summary>
    /// A simple helper class that gives a way to run things on the UI thread.  The app must call Initialize once during app start, using inside OnLaunch.
    /// </summary>
    public class UiDispatcher
    {
        static UiDispatcher instance;
        Dispatcher dispatcher;

        public static void Initialize(Dispatcher dispatcher)
        {
            instance = new UiDispatcher()
            {
                dispatcher = dispatcher
            };
        }

        public static void RunOnUIThread(Action a)
        {
            var nowait = instance.dispatcher.BeginInvoke(a);
        }
    }
}
