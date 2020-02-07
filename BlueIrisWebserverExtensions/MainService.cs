using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BlueIrisWebserverExtensions
{
	public partial class MainService : ServiceBase
	{
		public static Settings settings;

		public static WebServer webServer;

		public MainService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			webServer?.Stop();
			webServer = new WebServer();
			webServer.Start();
		}

		protected override void OnStop()
		{
			webServer?.Stop();
		}
	}
}
