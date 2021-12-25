using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
    public partial class MainWindow : Window, IDisposable
    {
        AlarmsGetter nodes = null;
        public List<AlarmState> filterWords = new List<AlarmState>();
        private List<string> filterArray = new List<string>();
        public MainWindow() {
            Environment.CurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            InitializeComponent();           
            fGrid.Visibility = Visibility.Hidden;
            Filter_ok_button.Visibility = Visibility.Hidden;
            AddBox.Visibility = Visibility.Hidden;
            nodes = new AlarmsGetter();
            RefreshAlarms();
            filterWords.Clear();
            foreach (var aName in nodes.Select(x => x.AlarmName).Distinct())
            {
                filterWords.Add(new AlarmState(aName, true));
            }
            TryToReadFilter();
            fGrid.ItemsSource = filterWords;
            fGrid.Items.Refresh();
            dGrid.ItemsSource = from node in nodes where !filterArray.Contains(node.AlarmName) select node;
            dGrid.Items.Refresh();
            RunSched();
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

        private void RunSched() {
            System.Timers.Timer taskTimer = new System.Timers.Timer(30000);
            taskTimer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            taskTimer.Start();
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            RefreshAlarms();
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e) {
            AddBox.Visibility = AddBox.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void Filter_Button_Click(object sender, RoutedEventArgs e) {
            //var win = new FilterWindow();
            var visible = fGrid.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

            fGrid.Visibility = visible;
            Filter_ok_button.Visibility = visible;


            dGrid.Items.Refresh();
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e) {

        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs e) {

            //filter?

            RefreshAlarms();
            
            dGrid.Items.Refresh();
        }

        private void TryToReadFilter() {
            FileInfo fileInf = new FileInfo("/filter.txt");
            if (fileInf.Exists)
            {
                using (StreamReader sr = new StreamReader("/filter.txt", System.Text.Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Replace("\n", "");
                        if (line.Length > 0)
                        {
                            foreach (var alarm in filterWords)
                            {
                                if (alarm.Name == line)
                                {
                                    alarm.State = false;
                                    filterArray.Add(line);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
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
            fGrid.Visibility = Visibility.Hidden;
            Filter_ok_button.Visibility = Visibility.Hidden;
            WriteFilterChanges();
        }

        private void WriteFilterChanges() {
            StringBuilder filterData = new StringBuilder();
            foreach (string alarm in filterArray.ToArray())
            {
                filterData.Append(alarm + '\n');
            }

            if (File.Exists("/filter.txt"))
                File.AppendAllText("/filter.txt", filterData.ToString());
            else
                File.WriteAllText("/filter.txt", filterData.ToString());
        }

        public void Dispose() {
            //WriteFilterChanges();
        }

        private void add_close_Click(object sender, RoutedEventArgs e) {
            AddBox.Visibility = Visibility.Hidden;
        }

        private void add_ok_Click(object sender, RoutedEventArgs e) {
            if(add_host.Text.Length > 0 && add_login.Text.Length > 0
                && add_pwd.Text.Length > 0 && add_name.Text.Length > 0)
            {
                if (nodes.AddEnode(add_host.Text, add_login.Text, add_pwd.Text, add_name.Text) == 1)
                {
                    MessageBox.Show("Найдено совпадение", $"Данные для {add_host.Text} обновлены");
                }
                AddBox.Visibility = Visibility.Hidden;
                RefreshAlarms();
            }
            else
            {
                MessageBox.Show("Проверка", "Не все поля заполнены");
            }
        }
    }
}
