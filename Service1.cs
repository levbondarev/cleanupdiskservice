using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Configuration;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using System.Reflection;
using Microsoft.VisualBasic.MyServices;
using Microsoft.Win32;

namespace CleanupDiskService_STC_ControlSystems
{
    public partial class CleanupService : ServiceBase
    {
    static Configuration thisConfiguration; 
        public List<FileInfo> cleanupFileList = new List<FileInfo>();
        public Timer serviceTimer = new Timer();
        public int checkingInterval = 1;
        public string checkingIntervalType = "Days";
        public string[] checkingFolders= { "C:\\"};
        public List <long> diskLimitsCheck;
        public List<long> diskLimitsDelete;
        public DateTime timeLastLaunch;
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public CleanupService()
        {
            InitializeComponent();
        }
        string[] MultiStringToArray(string multiString)
        {
            return multiString.TrimEnd(';').Replace("%",System.IO.Path.GetTempPath()).Split(';');
        }
        Boolean CheckFolderRootDiskCongested(int folderIndex)
        {
            var folderRoot = System.IO.Directory.GetDirectoryRoot(checkingFolders[folderIndex]).Replace(":","").Replace("\\","").Replace("/","");
            if (diskLimitsCheck.Count == 0 )// в том случае, если конфига пока просто нет!
            {//Минимально допустимый размер свободного места на системном диске в NT-системах вроде ХР и выше.
                if (new DriveInfo(folderRoot).AvailableFreeSpace < 268435456) return true; else return false;
            }
            else
            {
                log.Info(diskLimitsCheck[folderIndex].ToString() + "\\" + new DriveInfo(folderRoot).AvailableFreeSpace);
                    if (diskLimitsCheck[folderIndex] > new DriveInfo(folderRoot).AvailableFreeSpace)
                    {
                        return true;
                    }
                    else return false;

            }
        }
        void StartCleanUp(string folder,int folderIndex) {
            SummonDirectoryLineman(folder);
            log.Info("Построено виртуальное дерево файлов для сравнения. В дереве элементов: " + cleanupFileList.Count);
            DateTime deletedUponTime=DateTime.Now;
            foreach (FileInfo deletionCandidate in cleanupFileList.OrderBy(d => d.LastWriteTime)) {
                deletedUponTime = deletionCandidate.LastWriteTime;
                if (CheckExitConditions(folderIndex))
                {
                    log.Info("Состояние нехватки места на диске успешно устранено! Папка "+folder+" очищена аж по "+deletedUponTime);
                    return;
                }
                else {
                    try
                    {
                        deletionCandidate.Delete();
                    }
                    catch (Exception e)
                    {
                        log.Error("Не удалось удалить файл " + deletionCandidate.FullName + " по причине отказа в доступе либо иным причинам, перечисленным ниже:\r\n" + e.Message);
                        //throw;
                    }
                }
            }
            log.Fatal("Не удалось восстановить свободное место на диске! В Папке " + folder + " слишком мало доступных для удаления файлов!");
        }
        Boolean CheckExitConditions(int folderIndex) {
            if (diskLimitsDelete.Count == 0 )//В том случае если конфига пока просто нет!
            { //Минимально допустимый объём свободного места на системном диске в Windows NT-системах выше ХР
                if (new DriveInfo(System.IO.Path.GetPathRoot(checkingFolders[folderIndex]).Replace(":", "").Replace("\\", "").Replace("/", "")).AvailableFreeSpace > 268435456) return true; else return false;
            }
            else {
                if (new DriveInfo( System.IO.Path.GetPathRoot(checkingFolders[folderIndex]).Replace(":", "").Replace("\\", "").Replace("/", "")).AvailableFreeSpace > Convert.ToInt64(diskLimitsDelete[folderIndex]))
                {
                    return true;
                }
                else return false;
            }
        }
        void SummonDirectoryLineman(string targetFolder) {
            try
            {
                for (int j = 0; j < new DirectoryInfo(targetFolder).EnumerateFiles().Count(); j++)
                {
                    cleanupFileList.Add(new DirectoryInfo(targetFolder).EnumerateFiles().ElementAt(j));
                }
            }
            catch (Exception e)
            {
                //throw не нужен, так как мы не хотим остановки сервиса
                log.Error("Возникла ошибка при перечислении подпапок и файлов в папке "+targetFolder+" Подробности приведены ниже.\r\n"+e.Message.ToString());
                return; //Это решение может казаться неочевидным. В данном случае мы избегаем "вложенного исключения", чётко ограничивая исключения реально проблемными папками. Из проблемных папок рекурсивный метод просто выйдет, ещё на этапе перечисления файлов.
            }//Потому мы собственно и переместили перечислитель папок и повторный вызов обходчика вниз, после проверки, т. к. общие правила распространяются и на дочерей этой папки, и если файлы и папки перечислить невозможно, то и пойти вглубь - тем более.
            for (int i = 0; i < new DirectoryInfo(targetFolder).EnumerateDirectories().Count(); i++) {
                SummonDirectoryLineman(new DirectoryInfo(targetFolder).EnumerateDirectories().ElementAt(i).FullName);
            }
        }
        void GoCheck(object source, ElapsedEventArgs e) {
            if (CheckInterval())
            {
                log.Info("Срок проверки подошёл, сейчас начнётся проверка доступного места на дисках.");
                for (int f=0;f<checkingFolders.Count();f++){
                    if (CheckFolderRootDiskCongested(f)) {
                        log.Warn("На этом диске не хватает места. Сейчас будет начата очистка этого диска.");
                        StartCleanUp(checkingFolders[f],f);
                    }
                    else {
                        log.Info("На проверяемом диске место есть.");
                    }
                }
            }
            else
            {
                log.Info("Нет нужды в проверке дисков! Срок ещё не подошёл.");
            }
            thisConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            thisConfiguration.AppSettings.Settings.Remove("timeLastLaunch");
            thisConfiguration.AppSettings.Settings.Add("timeLastLaunch", DateTime.Now.ToString());
            thisConfiguration.Save(ConfigurationSaveMode.Full);
        }
        protected override void OnStart(string[] args)//Первый аргумент - периодичность - 1 день, неделя, месяц  (здесь и далее - 30 дней) и год (здесь и далее - 365 дней). Второй аргумент - диск. Третий аргумент - объём. Четвёртый аргумент - папки на Включение.
        {
            serviceTimer = new Timer();
            thisConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            log.Info("Служба запущена " + DateTime.Now.ToString());
            
            GetParams();
            try
            {
                if (checkingIntervalType == "Hours")
                {
                    serviceTimer.Interval = 3600000 * checkingInterval;//Час в миллисекундах
                    serviceTimer.Start();
                }
                else if (checkingIntervalType == "Minutes")
                {
                    serviceTimer.Interval = 60000 * checkingInterval;//Минута в миллисекундах
                    serviceTimer.Start();
                }

            }
            catch (Exception e)
            {
                log.Error(e.Message + e.InnerException.Message);
            }
            serviceTimer.Elapsed += new ElapsedEventHandler(GoCheck);
            log.Info("Начинаю проверку");
                GoCheck(this,null);
        }
        Boolean CheckInterval() {
            if (checkingIntervalType == "Days")
            {
                if ((DateTime.Now - timeLastLaunch).Days > checkingInterval)
                {
                    return true;
                }
                else { return false; }
            }
            else if (checkingIntervalType == "Hours") {
                if ((DateTime.Now - timeLastLaunch).Hours> checkingInterval)
                {
                    return true;
                }
                else { return false; }
            }
            else if (checkingIntervalType == "Minutes")
            {
                if ((DateTime.Now - timeLastLaunch).Minutes > checkingInterval)
                {
                    return true;
                }
                else { return false; }
            }else             return false;
        }
        List<Int64> castToList(string whatToCast) {
            List<Int64> thisCast= new List<Int64> {  };
            string[] castStringsList = whatToCast.TrimEnd(';').Replace("%",System.IO.Path.GetTempPath()).Split(';');
            for (int i=0;i<castStringsList.Count();i++) {
                thisCast.Add(Convert.ToInt64(castStringsList[i].Substring(castStringsList[i].IndexOf("*") + 1)));
            }
            return thisCast;
        }
            void GetParams() {
            try
            {
                thisConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
                checkingIntervalType = thisConfiguration.AppSettings.Settings["checkingIntervalType"].Value;
            checkingFolders = MultiStringToArray(thisConfiguration.AppSettings.Settings["checkingFolders"].Value);
            checkingInterval = Convert.ToInt32(thisConfiguration.AppSettings.Settings["checkingInterval"].Value);
            diskLimitsCheck = castToList(thisConfiguration.AppSettings.Settings["diskLimitsCheck"].Value);
            diskLimitsDelete = castToList(thisConfiguration.AppSettings.Settings["diskLimitsDelete"].Value);
                if (thisConfiguration.AppSettings.Settings["timeLastLaunch"].Value == "")
                {
                    timeLastLaunch = DateTime.MinValue;
                }
                else timeLastLaunch = DateTime.Parse(thisConfiguration.AppSettings.Settings["timeLastLaunch"].Value);
            }
            catch (Exception e)
            {
                log.Fatal(e.Message);
            }
        }
        protected override void OnStop()
        {
            log.Info("Служба остановилась в " + DateTime.Now.ToString());

        }
    }
}
