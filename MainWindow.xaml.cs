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
        List<Alarm> AlarmList;
        public MainWindow() {
            InitializeComponent();

            var ftpall = new AlarmsGetter();
            /*10.70.196.7
            10.70.196.66
            10.70.196.67*/
            //ftpall.AddEnode("10.70.196.7", "rbs", "rbs", "1090");
            //ftpall.AddEnode("10.70.196.66", "rbs", "rbs", "eNB3076");
            //ftpall.AddEnode("10.70.196.67", "rbs", "rbs", "eNB812");
            ftpall.GetAlarmsAsync();
            dGrid.ItemsSource = ftpall.alarms;
            ftpall.GetCeasedAlarmsAsync();
            dGridCeased.ItemsSource = ftpall.ceasedAlarms;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
           
            //AlarmList.Add(new Alarm(DateTime.Now, Alarm.AlarmClass.critical, "321", "123", "123"));
            dGrid.Items.Refresh();
        }

        private void dGrid_SourceUpdated(object sender, DataTransferEventArgs e) {
            dGrid.Items.Refresh();
        }
    }
}
