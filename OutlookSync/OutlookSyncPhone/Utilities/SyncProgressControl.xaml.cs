﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace OutlookSyncPhone.Utilities
{
    public partial class SyncProgressControl : UserControl
    {
        public SyncProgressControl()
        {
            InitializeComponent();
            OnCurrentChanged();
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(SyncProgressControl), new PropertyMetadata(0, new PropertyChangedCallback(OnMaximumChanged)));

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnMaximumChanged();
        }

        private void OnMaximumChanged()
        {
            ContactCount.Text = Maximum.ToString();
            UpdateFillHeight();
        }

        void UpdateFillHeight()
        {
            double fullHeight = OutlineCylinder.CylinderHeight;
            if (Maximum > 0)
            {
                if (Current == Maximum)
                {
                    FillCylinder.CylinderHeight = fullHeight;
                }
                else
                {
                    FillCylinder.CylinderHeight = (double)Current * fullHeight  / (double)Maximum;
                }
            }
            else
            {
                FillCylinder.CylinderHeight = 0;
            }
        }



        public int Current
        {
            get { return (int)GetValue(CurrentProperty); }
            set { SetValue(CurrentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Current.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentProperty =
            DependencyProperty.Register("Current", typeof(int), typeof(SyncProgressControl), new PropertyMetadata(0, new PropertyChangedCallback(OnCurrentChanged)));

        private static void OnCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnCurrentChanged();
        }

        private void OnCurrentChanged()
        {
            UpdateFillHeight();
        }


        
    }
}