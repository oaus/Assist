﻿using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assist.Game.Services
{
    internal class ModulesViewNavigationController
    {
        public static ModulePage CurrentPage;
        public static TransitioningContentControl ContentControl = new TransitioningContentControl();

        public static void Change(UserControl c) => ContentControl.Content = c;

        
    }
    public enum ModulePage
    {
        SELECTION,
        RPC,
        DODGE,
    }
}