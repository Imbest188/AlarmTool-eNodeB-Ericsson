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
using System.Windows.Shapes;

namespace AlarmTool_eNodeB_Ericsson
{
    /// <summary>
    /// Логика взаимодействия для FilterWindow.xaml
    /// </summary>
    /// 

    class AlarmName
    {
        public string Name { get; set; }
        public bool State { get; set; }
        public AlarmName(in string name, bool isDisabled = false) {
            Name = name;
            State = isDisabled;
        }
    }
    public partial class FilterWindow : Window
    {
        
        public FilterWindow() {
            InitializeComponent();
            List<AlarmName> alarms = new List<AlarmName>();
            alarms.Add(new AlarmName("123", true));
            alarms.Add(new AlarmName("124", false));
            alarms.Add(new AlarmName("125", true));
            fGrid.ItemsSource = alarms;
        }
    }
}
