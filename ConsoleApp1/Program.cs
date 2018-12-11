using System;
using System.ServiceProcess;
using System.IO;
using System.Net.NetworkInformation;

namespace ConsoleApp1
{
	class Program
	{

		static void Main(string[] args)
		{

			IpLoaderFromFile loader = new IpLoaderFromFile();
			loader.openAndReadIpFile();
			//Console.ReadKey();
			//Console.WriteLine("Введите имя службы");
			//string serviceName = Console.ReadLine();
			//Console.WriteLine("Введите IP компьютера");
			//string computerIp = Console.ReadLine();

			//WindowsServiceController controller = new WindowsServiceController(serviceName,computerIp);
			//OptionSelector selector = new OptionSelector(controller);
			//selector.ShowMessage("Выполнить другое дейтвие?");
			
		}
	}

	delegate void ShowMessage(string message);



	class IpLoaderFromFile
	{
		String fileOutput;

		public void openAndReadIpFile()
		{
			try
			{
				using (StreamReader streamReader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"ip.txt"))
				{
					fileOutput = streamReader.ReadToEnd();
					ParseAndPingIpFile(fileOutput);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		public void ParseAndPingIpFile(String fileOutPut)
		{
			IpAddressPingChecker ipPing = new IpAddressPingChecker();
			ServiceLogWriter logWriter = new ServiceLogWriter();
			String[] ipAdresses = fileOutPut.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			foreach (string ip in ipAdresses)
			{
				if (ipPing.PingReamoteIp(ip) != "TimedOut" && ipPing.PingReamoteIp(ip) != "HostUnReacheble")
				{
					CheckServiceStatusWithIpFromFile(ip);
				}
				else
				{
					
					logWriter.WriteLogFile(" "+ip + " TimedOut Check Network Connection");
					Console.WriteLine(" " + ip + " TimedOut Check Network Connection");
					logWriter.WriteLogFile("");
				}
			}
		}

		private void CheckServiceStatusWithIpFromFile(string ip)
		{

			WindowsServiceStatusOnStartChecker checker = new WindowsServiceStatusOnStartChecker();
			Console.WriteLine(ip + ": ");
			checker.CheckServiceStatus(ip);
		}


	}

	class WindowsServiceStatusOnStartChecker
	{
		string[] ServicesNamesArray = { "Transport", "Transport-Monitoring", "Transport-Updater", "JBOSS_SVC",
		"TapiSrv"};
		WindowsServiceController controller;
		ServiceLogWriter logWriter = new ServiceLogWriter();



		public void CheckServiceStatus(string ip)
		{
			foreach (string svc in ServicesNamesArray)
			{
				controller = new WindowsServiceController(svc, ip);
				logWriter.WriteLogFile(" Host: "+ ip + ", " + svc + ": " + controller.GetServiceStatus());
				Console.WriteLine(" Host: " + ip + ", " + svc + ": " + controller.GetServiceStatus());

				if(controller.GetServiceStatus() == "Stopped")
				{
					controller.StartService();
					
				}
			}

			CrystalServiceChecker crystalService = new CrystalServiceChecker();
			crystalService.CheckCrystalDeployment(ip);
		}
	}


	class OptionSelector {

		private string optionNumber;
		public OptionSelector(WindowsServiceController controller)
		{
			ShowMessage("Выберите действие:");
			ShowMessage("1: Start");
			ShowMessage("2: Stop");
			ShowMessage("3: Restart");
			ShowMessage("4: Status");
			this.optionNumber = Console.ReadLine();
			SelectAction(controller);
		}

		public void SelectAction(WindowsServiceController controller)
		{
			switch (optionNumber)
			{
				case "1":
					controller.StartService();
					break;
				case "2":
					controller.StopService();
					break;
				case "3":
					controller.RestartService();
					break;
				case "4":
					ShowMessage("Status: "+controller.GetServiceStatus());
					Console.ReadKey();
					break;


			}

		}

		public void ShowMessage(string message)
		{
			Console.WriteLine(message);
			
		}
	}


	class WindowsServiceController
	{
		private string serviceName;
		private string computerIp;
		private ServiceLogWriter log;
		public WindowsServiceController(string _serviceName, string _computerIp)
		{
			this.serviceName = _serviceName;
			this.computerIp = _computerIp;
			log = new ServiceLogWriter();
		}


		public void RestartService()
		{

			using (ServiceController service = new ServiceController(serviceName, computerIp))
			{
				try
				{
					service.Stop();
					service.WaitForStatus(ServiceControllerStatus.Stopped);
					service.Start();
					service.WaitForStatus(ServiceControllerStatus.Running);
				}
				catch (Exception ex)
				{
					
					log.WriteLogFile(ex.Message);
				}

			}


		}

		public void StartService()
		{
			
			using (ServiceController service = new ServiceController(serviceName, computerIp))
			{
				try
				{
					service.Start();
					service.WaitForStatus(ServiceControllerStatus.Running);
					Console.WriteLine("Service started: " + serviceName);
					log.WriteLogFile("Host: " + computerIp + " " + serviceName + " was stoped, now started");
					//Console.ReadKey();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					Console.ReadKey();
					log.WriteLogFile("Host: "+computerIp+", "+ ex.Message);
				}
			}


		}

		public void StopService()
		{

			using (ServiceController service = new ServiceController(serviceName, computerIp))
			{
				try
				{
					service.Stop();
					service.WaitForStatus(ServiceControllerStatus.Stopped);
					Console.WriteLine("Service Stopped: " + serviceName);
					
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					Console.ReadKey();
					log.WriteLogFile(ex.Message);
				}
			}
		}

		public string GetServiceStatus()
		{
			using (ServiceController service = new ServiceController(serviceName, computerIp))
			{
				try
				{
					return service.Status.ToString();
				}
				catch(Exception ex)
				{
					IpAddressPingChecker pingChecker = new IpAddressPingChecker();
					//Console.WriteLine(ex.Message);
					//log.WriteLogFile(ex.Message);
					return ex.Message;
				}
			}
		}
	}

	class IpAddressPingChecker
	{
		public string PingReamoteIp(string ip)
		{
			Ping ping = new Ping();
			PingReply pingReply = ping.Send(ip);
			return pingReply.Status.ToString();
		}
	}

	class ServiceLogWriter
	{
		private DateTime currentDateTime;
		protected string logFile, filePath;

		public ServiceLogWriter(){
			currentDateTime = DateTime.Now;
			logFile = currentDateTime.ToShortDateString()+"_"+currentDateTime.Hour + ".log";
			filePath = AppDomain.CurrentDomain.BaseDirectory + @"\Logs\"+logFile;
		
		}

		protected bool CheckIsLogFileExistOnCurrentDate()
		{

			if (File.Exists(filePath))
				return true;
			else
				return false;

		}

		private void CreateFileAndWriteFirstLine(string logText)
		{
			
			File.WriteAllText(filePath, logText + Environment.NewLine);
		}

		public void WriteLogFile(string logText)
		{
			if (CheckIsLogFileExistOnCurrentDate())
			{
				File.AppendAllText(filePath, currentDateTime.ToString() + logText + Environment.NewLine);
			}
			else
			{
				CreateFileAndWriteFirstLine(currentDateTime.ToString() + logText);
			}
			
		}
	}

	class ServiceLogCleaner
	{
		private DateTime currentDateTime;
		private string logFileName;
		private string logFilePath;

		public ServiceLogCleaner()
		{
			currentDateTime = DateTime.Now;
			logFileName = currentDateTime.ToShortDateString() + "_" + currentDateTime.Hour + ".log";
		}

		private void GetCurrentDateTime()
		{

		}
		
		string[] FindLogFilesToDelete()
		{
			throw new Exception("Доделай этот кусок кода");
		}

		private void DeleteLogFiles()
		{

		}
	}

	class CrystalServiceChecker : ServiceLogWriter
	{
		public CrystalServiceChecker()
		{
			
			
		}


		public void CheckCrystalDeployment(string ip)
		{

			filePath = @"\\" + ip + @"\C$\Program Files (x86)\SetRetail10\standalone\deployments\Set10.ear.deployed";
			if (CheckIsLogFileExistOnCurrentDate())
			{
				filePath = AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + logFile;
				WriteLogFile(" Host: " + ip + ",  set10 deployed");
				Console.WriteLine(" Host: " + ip + ",  set10 deployed");
			}
			else
			{

				CheckCrystalDeploymentInX86System(ip);
			}

			WriteLogFile(""); //отступ, что бы было удобнее читать

		}


		public void CheckCrystalDeploymentInX86System(string ip)
		{
			filePath = @"\\" + ip + @"\C$\Program Files\SetRetail10\standalone\deployments\Set10.ear.deployed";
			if (CheckIsLogFileExistOnCurrentDate())
			{
				filePath = AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + logFile;
				WriteLogFile(" Host: " + ip + ",  set10 deployed");
				Console.WriteLine(" Host: " + ip + ",  set10 deployed");
			}
			else
			{
				filePath = AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + logFile;
				WriteLogFile(" Host: " + ip + ",  set10 failed");
				Console.WriteLine(" Host: " + ip + ",  set10 failed");
			}

		}

	}



}
