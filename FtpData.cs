using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;


namespace AlarmTool_eNodeB_Ericsson_
{
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
        public Alarm(in DateTime raiseTime, AlarmClass aClass, in string description, in string objectName) {
            RaiseTime = raiseTime;
            ObjectName = objectName;
            AlarmText = description;
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
    }
    public class FtpData
    {
        private readonly FluentFTP.FtpClient client;
        private const string logFilePath = "/c/logfiles/alarm_event/ALARM_LOG.xml";
        private List<Alarm> alarmList = null;
        private string name;

        public FtpData(in string host, in string login, in string password, in string eNodeB = null) {
            client = new FluentFTP.FtpClient
            {
                Host = host,
                Credentials = new System.Net.NetworkCredential(login, password)
            };

            name = eNodeB ?? host;
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
                    alarmList.Add(new Alarm(in dt, in alarmText, in name));
                }
            }
        }
        
        private void UpdateAlarms() {
            
            client.Connect();

            if (client.IsConnected == false)
            {
                alarmList = new List<Alarm>() { new Alarm(DateTime.Now, Alarm.AlarmClass.critical, "Host Unavaliable", name) };
                return;
            }

            byte[] logFile;
            client.Download(out logFile, logFilePath);

            ParseAlarms(ref alarmList, in logFile);
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

