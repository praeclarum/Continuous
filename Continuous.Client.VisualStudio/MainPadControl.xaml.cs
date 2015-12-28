using System;
using System.Collections.ObjectModel;
using System.Linq;

using System.Windows;
using System.Windows.Controls;

namespace Continuous.Client.VisualStudio
{
    public partial class MainPadControl : UserControl
    {
        readonly ObservableCollection<TypeCode> dependenciesStore = new ObservableCollection<TypeCode> ();

        public MainPadControl()
        {
            this.InitializeComponent();
            Loaded += MainPadControl_Loaded;
        }

        private void MainPadControl_Loaded (object sender, RoutedEventArgs e)
        {
            Env.LinkedMonitoredCode += Env_LinkedMonitoredCode;
        }

        private void Env_LinkedMonitoredCode (LinkedCode obj)
        {
            dependenciesStore.Clear ();
            var q = obj.Types.OrderBy (x => x.Name);
            foreach (var t in q) {
                dependenciesStore.Add (t);
            }
        }

        protected ContinuousEnv Env { get { return ContinuousEnv.Shared; } }

        async void HandleSetType (object sender, RoutedEventArgs e)
        {
            try {
                await Env.VisualizeAsync ();
            }
            catch (Exception ex) {
                Log (ex);
            }
        }
        async void HandleRefresh(object sender, RoutedEventArgs e)
        {
            try {
                await Env.VisualizeMonitoredTypeAsync (forceEval: true, showError: true);
            }
            catch (Exception ex) {
                Log (ex);
            }
        }
        async void HandleClearEdits (object sender, RoutedEventArgs e)
        {
            try {
                TypeCode.ClearEdits ();
                await Env.VisualizeMonitoredTypeAsync (forceEval: false, showError: false);
            }
            catch (Exception ex) {
                Log (ex);
            }
        }
        async void HandleStop (object sender, RoutedEventArgs e)
        {
            try {
                await Env.StopVisualizingAsync ();
                dependenciesStore.Clear ();
            }
            catch (Exception ex) {
                Log (ex);
            }
        }

        void Log(Exception ex)
        {
            MessageBox.Show(ex.ToString (), "Continuous Coding Error");
        }
    }
}
