﻿using BPUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BlueIrisWebserverExtensions
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			WindowsServiceInitOptions options = new WindowsServiceInitOptions();
			options.RunForDebugging = true;

			AppInit.WindowsService<MainService>(options);
		}
	}
}
