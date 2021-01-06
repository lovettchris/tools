﻿using LovettSoftware.Utilities;
using Microsoft.Win32;
using MyFitness.Model;
using System;
using System.ComponentModel;
using System.Windows;

namespace MyFitness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();
        CalendarModel model = new CalendarModel();

        public MainWindow()
        {
            InitializeComponent();
            RestoreSettings();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            Settings.Instance.PropertyChanged += OnSettingsChanged;

            this.Loaded += OnWindowLoaded;
            this.model.PropertyChanged += OnModelPropertyChanged;
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDirty" && model.IsDirty)
            {
                saveModelPending = true;
                delayedActions.StartDelayedAction("SaveModel", OnSaveModel, TimeSpan.FromSeconds(2));
            }
        }

        private void OnSaveModel()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.FileName))
            {
                delayedActions.CancelDelayedAction("SaveModel");
                saveModelPending = false;
                model.Save(Settings.Instance.FileName);
            }
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Instance.FileName) && System.IO.File.Exists(Settings.Instance.FileName))
            {
                await model.LoadAsync(Settings.Instance.FileName);
            }

            MyCalendar.DataContext = model.GetOrCreateCurrentMonth();
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.CheckFileExists = true;
            if (fd.ShowDialog() == true)
            {
                Settings.Instance.FileName = fd.FileName;
            }
        }

        private void OnSaveFile(object sender, RoutedEventArgs e)
        {
            var filename = Settings.Instance.FileName;
            if (string.IsNullOrEmpty(filename))
            {
                OpenFileDialog fd = new OpenFileDialog();
                fd.CheckFileExists = false;
                if (fd.ShowDialog() == true)
                {
                    filename = fd.FileName;
                }
            }

            try
            {
                model.Save(filename);
                Settings.Instance.FileName = filename;
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {

        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowLocation = bounds.TopLeft;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowSize = bounds.Size;
        }

        private void RestoreSettings()
        {
            Settings settings = Settings.Instance;
            if (settings.WindowLocation.X != 0 && settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new System.Drawing.Rectangle(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Height));
                var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                bounds.Intersect(screen.WorkingArea);

                this.Left = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.X);
                this.Top = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Y);
                this.Width = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Width);
                this.Height = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Height);
            }
            this.Visibility = Visibility.Visible;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            saveSettingsPending = true;
            delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (saveSettingsPending)
            {
                // then we need to do synchronous save and cancel any delayed action.
                OnSaveSettings();
            }
            if (saveModelPending)
            {
                OnSaveModel();
            }

            base.OnClosing(e);
        }

        void OnSaveSettings()
        {
            if (saveSettingsPending)
            {
                saveSettingsPending = false;
                delayedActions.CancelDelayedAction("SaveSettings");
                if (Settings.Instance != null)
                {
                    try
                    {
                        Settings.Instance.Save();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private bool saveModelPending;
        private bool saveSettingsPending;
        
    }
}
