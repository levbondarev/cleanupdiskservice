using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Configuration;
using System.Windows.Forms;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cleanup_SRCCS_Config
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static public int checkingInterval = 1;
        static public string checkingIntervalType = "Days";
        static public List <string> checkingFolders;
        static public List <long> diskLimitsCheck;
        static public List <long> diskLimitsDelete;
        static public List <long> diskLimitsCurrent;
        static Configuration serviceConfig;
        static int selectedIndex;
        List<Int64> castToList(string whatToCast)
        {
            List<Int64> thisCast = new List<Int64> { };
            string[] castStringsList = whatToCast.TrimEnd(';').Split(';');
            for (int i = 0; i < castStringsList.Count(); i++)
            {
                thisCast.Add(Convert.ToInt64(castStringsList[i].Substring(castStringsList[i].IndexOf("*") + 1)));
            }
            return thisCast;
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                checkingInterval = Convert.ToInt32(textBoxInterval.Text);
            }
            catch (Exception xc)
            {
                System.Windows.MessageBox.Show(xc.Message.ToString());
                return;
            }
            serviceConfig.AppSettings.Settings["checkingInterval"].Value = checkingInterval.ToString();
            serviceConfig.AppSettings.Settings["checkingIntervalType"].Value = checkingIntervalType.ToString();
            string prepositionedFolders = "";
            string prepositionedCheckingSizes = "";
            string prepositionedDeleteSizes = "";
            for (int i = 0; i < checkingFolders.Count; i++) {
                prepositionedFolders = prepositionedFolders + checkingFolders[i].Replace("\\","\\\\") + ";";
                prepositionedCheckingSizes = prepositionedCheckingSizes + System.IO.Path.GetPathRoot(checkingFolders[i]).ToString().Replace(":","").Replace("/","").Replace("\\","")+"*"+diskLimitsCheck.ElementAt(i).ToString()+";";
                prepositionedDeleteSizes = prepositionedDeleteSizes +System.IO.Path.GetPathRoot(checkingFolders[i]).ToString().Replace(":", "").Replace("/", "").Replace("\\", "") + "*"+diskLimitsDelete.ElementAt(i).ToString()+";";
            }
            prepositionedFolders = prepositionedFolders + ";";
            serviceConfig.AppSettings.Settings["checkingFolders"].Value = prepositionedFolders;
            serviceConfig.AppSettings.Settings["diskLimitsCheck"].Value = prepositionedCheckingSizes;
            serviceConfig.AppSettings.Settings["diskLimitsDelete"].Value = prepositionedDeleteSizes;
            serviceConfig.Save(ConfigurationSaveMode.Full);
        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            button3_Click(this, new RoutedEventArgs());
            Close(); 
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            serviceConfig = ConfigurationManager.OpenExeConfiguration("CleanupDiskService_SRC_ControlSystems.exe");
            checkingInterval = Convert.ToInt32(serviceConfig.AppSettings.Settings["checkingInterval"].Value);
            if (checkingInterval == 0) { checkingInterval = 1; }
            checkingFolders = MultiStringToArray(serviceConfig.AppSettings.Settings["checkingFolders"].Value).ToList();
            diskLimitsCheck = castToList(serviceConfig.AppSettings.Settings["diskLimitsCheck"].Value);
            diskLimitsDelete = castToList(serviceConfig.AppSettings.Settings["diskLimitsDelete"].Value);
            diskLimitsCurrent = new List<long>();
            for (int i=0; i<checkingFolders.Count; i++) {
                if (checkingFolders[i]=="") {
                    checkingFolders[i] = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
                }
                if (diskLimitsCheck.ElementAt(i)==0) {
                    diskLimitsCheck[i] = 268453456;
                }
                if (diskLimitsDelete.ElementAt(i) == 0)
                {
                    diskLimitsDelete[i] = 268453456;
                }
                diskLimitsCurrent.Add(Convert.ToInt64(new DriveInfo(System.IO.Path.GetPathRoot(checkingFolders[i]).Replace("\\", "").Replace(":", "").Replace("/", "")).TotalSize));
                listView.Items.Add(checkingFolders[i]);
            }
            if (checkingIntervalType == "Days")
            {
                comboBox.SelectedIndex = 0;
            }
            else if(checkingIntervalType == "Hours") {
                comboBox.SelectedIndex = 1;
            }
            else if (checkingIntervalType == "Minutes")
            {
                comboBox.SelectedIndex = 2;
            }
            textBoxInterval.Text = checkingInterval.ToString();
            listView.SelectedIndex = 0;
            selectedIndex = 0;
        }
        string[] MultiStringToArray(string multiString)
        {
            return multiString.TrimEnd(';').Replace("\\\\","\\").Replace("%",System.IO.Path.GetTempPath()).Split( ';');
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex!=-1) {
                if (selectedIndex == listView.SelectedIndex) {
                    selectedIndex = 0;
                }
                if (listView.Items.Count!=0) {
                    checkingFolders.RemoveAt(listView.SelectedIndex);
                    diskLimitsCheck.RemoveAt(listView.SelectedIndex);
                    diskLimitsDelete.RemoveAt(listView.SelectedIndex);
                    diskLimitsCurrent.RemoveAt(listView.SelectedIndex);
                    listView.Items.RemoveAt(listView.SelectedIndex);
                }
            }
        }
        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItems.Count == 1)
            {
                selectedIndex= listView.SelectedIndex;
                textBoxFolder.Text = listView.SelectedItem.ToString();
                sliderCheck.Maximum = diskLimitsCurrent[selectedIndex];
                sliderCheck.Value = diskLimitsCheck[selectedIndex];
                sliderDelete.Maximum = diskLimitsCurrent[selectedIndex];
                sliderDelete.Value = diskLimitsDelete[selectedIndex];
                labelCheck.Content = "При " + Convert.ToInt32(diskLimitsCheck[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsCheck[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов начать чистку)";
                labelDelete.Content = "При " + Convert.ToInt32(diskLimitsDelete[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsDelete[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов закончить чистку)";
            }
        }
        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            try
            {
                FBD.SelectedPath = textBoxFolder.Text;
            }
            catch (Exception)
            {
               System.Windows.MessageBox.Show("Неправильно указана папка!");
            }
            FBD.ShowDialog();
            textBoxFolder.Text = FBD.SelectedPath;
        }
        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(textBoxFolder.Text))
            {
                checkingFolders.Add(textBoxFolder.Text);
                diskLimitsCheck.Add(268435456);
                diskLimitsDelete.Add( 268435456);
                diskLimitsCurrent.Add(new DriveInfo(System.IO.Path.GetPathRoot(textBoxFolder.Text).Replace(":", "").Replace("/", "").Replace("\\", "")).TotalSize);
                listView.Items.Add(textBoxFolder.Text);
            }
            else {
                System.Windows.MessageBox.Show("Такой папки нет");
            }
        }
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                checkingIntervalType = "Days";
            }
            else if (comboBox.SelectedIndex == 1 )
            {
                checkingIntervalType = "Hours";
            }
            else if (comboBox.SelectedIndex ==2)
            {
                checkingIntervalType = "Minutes";
            }
        }
        private void slider_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if ((selectedIndex > 0) && (selectedIndex < listView.Items.Count))
            {
                diskLimitsCheck[selectedIndex] = Convert.ToInt64(sliderCheck.Value);
                listView.SelectedIndex = selectedIndex;
                labelCheck.Content = "При " + Convert.ToInt32(diskLimitsCheck[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsCheck[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов начать чистку)";
            }
        }
        private void sliderDelete_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if ((selectedIndex > 0) && (selectedIndex < listView.Items.Count))
            {
                diskLimitsDelete[selectedIndex] = Convert.ToInt64(sliderDelete.Value);
                listView.SelectedIndex = selectedIndex;
                labelDelete.Content = "При " + Convert.ToInt32(diskLimitsDelete[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsDelete[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов закончить чистку)";
            }
        }

        private void sliderCheck_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((selectedIndex > 0) && (selectedIndex < listView.Items.Count))
            {
                diskLimitsCheck[selectedIndex] = Convert.ToInt64(sliderCheck.Value);
                listView.SelectedIndex = selectedIndex;
                labelCheck.Content = "При " + Convert.ToInt32(diskLimitsCheck[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsCheck[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов начать чистку)";
            }
        }

        private void sliderDelete_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((selectedIndex > 0) && (selectedIndex < listView.Items.Count))
            {
                diskLimitsDelete[selectedIndex] = Convert.ToInt64(sliderDelete.Value);
                listView.SelectedIndex = selectedIndex;
                labelDelete.Content = "При " + Convert.ToInt32(diskLimitsDelete[selectedIndex] * 100 / diskLimitsCurrent[selectedIndex]).ToString() + "% (" + diskLimitsDelete[selectedIndex].ToString() + "/" + diskLimitsCurrent[selectedIndex].ToString() + " байтов закончить чистку)";
            }
        }
    }
}
