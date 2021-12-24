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
    public class AlarmState
    {
        public string Name { get; set; }
        public bool State { get; set; }
        public AlarmState(in string name, bool isDisabled = false) {
            Name = name;
            State = isDisabled;
        }

        public override bool Equals(object obj) {
            return obj is AlarmState state &&
                   Name == state.Name;
        }
    }
    public partial class MainWindow : Window
    {
        AlarmsGetter nodes = null;
        public List<AlarmState> filterWords = new List<AlarmState>();
        private List<string> filterArray = new List<string>();
        public MainWindow() {
            InitializeComponent();
            fGrid.Visibility = Visibility.Hidden;
            nodes = new AlarmsGetter();
            RefreshAlarms();

            filterWords.Clear();
            foreach (var aName in nodes.Select(x => x.AlarmName).Distinct())
            {
                filterWords.Add(new AlarmState(aName, true));
            }
            fGrid.ItemsSource = filterWords;
        }

        private void RefreshAlarms() {
            nodes.GetAlarmsAsync();

            dGrid.ItemsSource = from node in nodes where !filterArray.Contains(node.AlarmName) select node;
            nodes.GetCeasedAlarmsAsync();
            dGridCeased.ItemsSource = nodes.ceasedAlarms;

            foreach (var aName in nodes.Select(x => x.AlarmName).Distinct())
            {
                bool contains = false;
                foreach(var alarm in filterWords)
                {
                    if(alarm.Name == aName)
                    {
                        contains = true;
                    }
                }
                if (!contains) {
                    filterWords.Add(new AlarmState(aName, true));
                }
            }
        }
        private void dGrid_SourceUpdated(object sender, DataTransferEventArgs e) {
            dGrid.Items.Refresh();
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e) {

        }

        private void Filter_Button_Click(object sender, RoutedEventArgs e) {
            //var win = new FilterWindow();
            fGrid.Visibility = fGrid.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            
            dGrid.Items.Refresh();
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e) {

        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs e) {

            //filter?

            RefreshAlarms();
            
            dGrid.Items.Refresh();
        }

        private void fGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {

        }

        private void Button_fOk_Click(object sender, RoutedEventArgs e) {
            filterArray.Clear();
            for (int i = 0; i < fGrid.Items.Count; i++)
            {
                CheckBox stateCheckbox = fGrid.Columns[0].GetCellContent(fGrid.Items[i]) as CheckBox;
                if (stateCheckbox != null && stateCheckbox.IsChecked == false)
                {
                    string alarmName = (fGrid.Columns[1].GetCellContent(fGrid.Items[i]) as TextBlock).Text;
                    filterArray.Add(alarmName);
                }
            }
            fGrid.Items.Refresh();
            RefreshAlarms();
        }
    }
}
