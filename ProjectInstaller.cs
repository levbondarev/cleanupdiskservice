using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;
namespace CleanupDiskService_STC_ControlSystems
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;//Запускаем службу от имени Пользователя (т. к. используем КуррентУзер регистр);
            this.serviceInstaller1.Description = "Данная Служба контролирует наличие свободного места на диске по Вашим или заводским настройкам и удаляет застарелые файлы, снабжая свои действия кратким повременным описанием (журналом)."; //Типа описания небольшого
            this.serviceInstaller1.DisplayName = "Служба контроля места на диске от НИЦ Системы Управления"; //Так она отображается в списке
            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;
            this.serviceInstaller1.ServiceName = "CleanupDiskService";
            this.serviceInstaller1.AfterInstall += ServiceInstaller_AfterInstall;

        }
        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            //ServiceController sc = new ServiceController("Служба контроля места на диске от НИЦ Системы Управления");
            //sc.Start();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
