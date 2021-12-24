using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Enodeb;


namespace AlarmTool_eNodeB_Ericsson
{
    public partial class MainWindow : Window
    {
        AlarmsGetter nodes = null;
        public MainWindow() {
            InitializeComponent();

            nodes = new AlarmsGetter();
            nodes.GetAlarmsAsync();
            dGrid.ItemsSource = nodes.alarms;
            nodes.GetCeasedAlarmsAsync();
            dGridCeased.ItemsSource = nodes.ceasedAlarms;
        }

        private void dGrid_SourceUpdated(object sender, DataTransferEventArgs e) {
            dGrid.Items.Refresh();
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e) {

        }

        private void Filter_Button_Click(object sender, RoutedEventArgs e) {
            List<string> filterArray = new List<string> { "License Key File Fault", "Password File Fault" };
            var win = new FilterWindow();
            
            dGrid.ItemsSource = from node in nodes where !filterArray.Contains(node.AlarmName) select node;
            dGrid.Items.Refresh();
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e) {

        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs e) {

            //filter?
            nodes.GetAlarmsAsync();
            nodes.GetCeasedAlarmsAsync();
            dGrid.Items.Refresh();
        }
    }
}
