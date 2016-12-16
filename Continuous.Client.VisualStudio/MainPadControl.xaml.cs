using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            main.DataContext = Env;

            //
            // Enable discovery
            //
            try
            {
                Firewall.AddUdpInRuleIfNeeded(Http.DiscoveryBroadcastReceiverPort, "Continuous Coding Discovery");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FAILED TO ADD FIREWALL RULE: {0}", ex);
            }
            Env.Discovery.DevicesChanged += Discovery_DevicesChanged;
            PopulateDevices();

        }

        void PopulateDevices ()
        {
            var active = ipText.Text;

            var ds = Env.Discovery.Devices;
            ipText.Items.Clear();
            foreach (var d in ds)
            {
                ipText.Items.Add(d);
            }

            if (active == Http.DefaultHost && ds.Length > 0)
            {
                ipText.SelectedItem = ds[0];
            }
        }

        private void Discovery_DevicesChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                PopulateDevices();
            });
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
