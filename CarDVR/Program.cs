﻿using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace CarDVR
{
	static class Program
	{
		public static MainForm mainform;
		public static SettingsImpl settings = new SettingsImpl();

		class MyAppContext : ApplicationContext
		{
			public MyAppContext()
			{
				mainform = new MainForm();
			}
		}

		[STAThread]
		static void Main()
		{
			int processCount = 0;

			foreach (Process p in Process.GetProcesses())
				if (p.ProcessName == Process.GetCurrentProcess().ProcessName)
				{
					if (++processCount == 2)
						return;
				}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MyAppContext());
		}
	}

	public class Logger
	{
		static public void log(string s)
		{
			//using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"CarDvr.log", true))
			//{
			//    file.Write(DateTime.Now.ToString() + ": ");
			//    file.WriteLine(s);
			//}
		}
	}

}
