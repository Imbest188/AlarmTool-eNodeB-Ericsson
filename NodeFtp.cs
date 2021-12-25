using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Enodeb
{
    class AlarmsGetter : IEnumerable<Alarm>
    {
        public List<Alarm> alarms { get; private set; }
        public List<Alarm> ceasedAlarms { get; private set; }
        private List<NodeFtp> enodes;

        const string nodeFile = "nodes.txt";

        public AlarmsGetter() {
            alarms = new List<Alarm>();
            ceasedAlarms = new List<Alarm>();
            enodes = new List<NodeFtp>();
            InitNodes();
        }

        private void InitNodes() {
            FileInfo fileInf = new FileInfo(nodeFile);
            if (fileInf.Exists)
            {
                using (StreamReader sr = new StreamReader(nodeFile, System.Text.Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var data = line.Split(';');
                        if (data.Length == 4)
                        {
                            enodes.Add(new NodeFtp(data[0], data[1], data[2], data[3]));
                        }
                    }
                }
            }
        }

        private async Task<bool> CollectAlarmsFromNode(NodeFtp node) {
            alarms.AddRange(node.GetActiveAlarms());
            return true;
        }

        private async Task<bool> CollectCeasedAlarmsFromNode(NodeFtp node) {
            ceasedAlarms.AddRange(node.GetCeasedAlarms());
            return true;
        }

        public async void GetAlarmsAsync() {
            alarms.Clear();

            foreach (var node in enodes)
            {
                CollectAlarmsFromNode(node);
            }

            Task.WhenAll();
        }

        public async void GetCeasedAlarmsAsync() {
            ceasedAlarms.Clear();

            foreach (var node in enodes)
            {
                CollectCeasedAlarmsFromNode(node);
            }

            Task.WhenAll();
        }



        private void SaveNodeToFile(in string host, in string login, in string password, in string name) {
            string nodeAuthData = $"{host};{login};{password};{name}\n";
            if (File.Exists(nodeFile))
                File.AppendAllText(nodeFile, nodeAuthData);
            else
                File.WriteAllText(nodeFile, nodeAuthData);
        }

        public int AddEnode(in string host, in string login, in string password, in string name) {
            for (int i = 0; i < enodes.Count; i++)
            {
                if(enodes[i].Host == host)
                {
                    enodes[i] = new NodeFtp(host, login, password);
                    SaveNodeToFile(host, login, password, name);
                    return 1;
                }
            }
            enodes.Add(new NodeFtp(host, login, password));
            SaveNodeToFile(host, login, password, name);
            return 0;
        }

        public bool RemoveEnode(in string host = null, in string name = null) {
            if (host != null)
            {
                for (int i = 0; i < enodes.Count; i++)
                {
                    if (enodes[i].Host == host || enodes[i].Name == name)
                    {
                        enodes.Remove(enodes[i]);
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerator<Alarm> GetEnumerator() {
            return ((IEnumerable<Alarm>)alarms).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((System.Collections.IEnumerable)alarms).GetEnumerator();
        }
    }
    public class Alarm
    {
        public enum AlarmClass
        {
            minor = 4,
            critical = 2,
            major = 3,
            warning = 5,
            ceased = 6
        }

        public DateTime RaiseTime { get; private set; }
        public AlarmClass Class { get; private set; }
        public string ObjectName { get; private set; }
        public int AlarmId { get; private set; }
        public string AlarmName { get; private set; }
        public string AlarmText { get; private set; }

        public Alarm(in DateTime raiseTime, in string description, in string objectName) {
            RaiseTime = raiseTime;
            ObjectName = objectName;
            ParseDescription(description);
        }
        public Alarm(in DateTime raiseTime, AlarmClass aClass, in string alarmName, in string alarmText, in string objectName) {
            RaiseTime = raiseTime;
            ObjectName = objectName;
            AlarmName = alarmName;
            AlarmText = alarmText;
            Class = aClass;
        }

        private void ParseDescription(in string description) {
            const char Separator = ';';
            string[] csvParts = description.Split(Separator);
            if (csvParts.Length > 14)
            {
                Class = (AlarmClass)int.Parse(csvParts[9]);
                AlarmName = csvParts[10];
                AlarmText = csvParts[12];
                AlarmId = int.Parse(csvParts[13].Replace('_', ' '));
            }
        }

        override public string ToString() {

            return $"{RaiseTime} {AlarmName}\n{AlarmText}";
        }

        public override bool Equals(object obj) {
            return (obj is Alarm alarm &&
                   AlarmName == alarm.AlarmName)
                   || (obj is string t_string &&
                   AlarmName == t_string);
        }

        public override int GetHashCode() {
            return 1628282889 + EqualityComparer<string>.Default.GetHashCode(AlarmName);
        }
    }
    public class NodeFtp
    {
        private readonly FluentFTP.FtpClient client;
        private const string logFilePath = "/c/logfiles/alarm_event/ALARM_LOG.xml";
        private List<Alarm> alarmList = null;
        public string Name { get; set; }
        public string Host { get; set; }

        public NodeFtp(in string host, in string login, in string password, in string eNodeB = null) {
            Host = host;
            client = new FluentFTP.FtpClient
            {
                Host = host,
                Credentials = new System.Net.NetworkCredential(login, password)
            };

            Name = eNodeB ?? host;
        }

        private void ParseAlarms(ref List<Alarm> alarmList, in byte[] logFileData) {
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Parse
            };

            System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
                new MemoryStream(buffer: logFileData),
                settings
            );
            reader.MoveToContent();

            DateTime dt = default;

            if(alarmList == null)
            {
                alarmList = new List<Alarm>();
            }

            if(alarmList.Count > 0)
            {
                alarmList.Clear();
            }

            while (reader.Read())
            {
                if (reader.Name == "TimeStamp")
                {
                    var subtree = reader.ReadSubtree();

                    Dictionary<string, int> dict = new Dictionary<string, int>();

                    subtree.MoveToContent();
                    while (subtree.Read())
                    {
                        if (subtree.Name.Length > 0 && !subtree.Name.Contains("Element") && !subtree.Name.Contains("Stamp"))
                        {
                            dict.Add(subtree.Name, int.Parse(subtree.ReadElementContentAsString()));
                        }
                    }

                    int year;
                    dict.TryGetValue("year", out year);
                    int month;
                    dict.TryGetValue("month", out month);
                    int day;
                    dict.TryGetValue("day", out day);
                    int hour;
                    dict.TryGetValue("hour", out hour);
                    int minute;
                    dict.TryGetValue("minute", out minute);
                    int second;
                    dict.TryGetValue("second", out second);

                    dt = new DateTime
                    (
                        year: year,
                        month: month,
                        day: day,
                        hour: hour,
                        minute: minute,
                        second: second
                    );
                }
                if (reader.Name == "RecordContent")
                {
                    string alarmText = reader.ReadElementContentAsString().Trim();
                    alarmList.Add(new Alarm(in dt, in alarmText, Name));
                }
            }
        }

        private bool TryToConnect() {
            try
            {
                client.Connect();
            }
            catch (System.TimeoutException)
            {
                alarmList = new List<Alarm>() { new Alarm(
                    DateTime.Now, Alarm.AlarmClass.critical, "Host Unavaliable",
                    "Could not connect to the remote host", Name) };
                return false;
            }
            if (client.IsConnected == false)
            {
                alarmList = new List<Alarm>() { new Alarm(
                    DateTime.Now, Alarm.AlarmClass.critical, "Host Unavaliable",
                    "Could not connect to the remote host", Name) };
                return false;
            }
            if (client.IsAuthenticated == false)
            {
                alarmList = new List<Alarm>() { new Alarm(
                    DateTime.Now, Alarm.AlarmClass.critical, "Auth exception",
                    "Combination of login/password is wrong", Name) };
                return false;
            }

            return true;
        }
        
        private void UpdateAlarms() {

            if (TryToConnect())
            {
                byte[] logFile;
                client.Download(out logFile, logFilePath);

                ParseAlarms(ref alarmList, in logFile);
            }
        }

        public List<Alarm> GetAlarms(bool update = false) {
            if (alarmList == null || update)
            {
                UpdateAlarms();
            }
            return alarmList;
        }

        public List<Alarm> GetActiveAlarms(bool update = false) {
            List<Alarm> allAlarms = GetAlarms(update).ToList();
            List<Alarm> activeAlarms = new List<Alarm>();
            var alarmsById = allAlarms.GroupBy(aId => aId.AlarmId);
            foreach (var alarm in alarmsById)
            {
                var lastAlarm = alarm.Last();
                if (lastAlarm.Class != Alarm.AlarmClass.ceased)
                {
                    activeAlarms.Add(lastAlarm);
                }
            }

            return activeAlarms.ToList();
        }
        public List<Alarm> GetCeasedAlarms(bool update = false) {
            List<Alarm> allAlarms = GetAlarms(update).ToList();
            List<Alarm> ceasedAlarms = new List<Alarm>();
            var alarmsById = allAlarms.GroupBy(aId => aId.AlarmId);
            foreach (var alarm in alarmsById)
            {
                var lastAlarm = alarm.Last();
                if (lastAlarm.Class == Alarm.AlarmClass.ceased)
                {
                    ceasedAlarms.Add(lastAlarm);
                }
            }

            return ceasedAlarms.ToList();
        }
    }
}

