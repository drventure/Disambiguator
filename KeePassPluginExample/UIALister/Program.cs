using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace UIALister
{

	class Program
	{
		[DllImport("user32.dll", EntryPoint = "FindWindowEx")]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsWindowEnabled(IntPtr hWnd);

		private static string[] SupportedTopLevelWindowClasses = new[] 
		{
			"MozillaUIWindowClass",
			"MozillaWindowClass",
			"IEFrame",
			"OperaWindowClass",
			"ApplicationFrameWindow", // Edge, or, unfortunately, any Metro app
			// Chrome may append any number to this, but to search for a specific class name, which can't use wildcards, just use the first few.
			"Chrome_WidgetWin_0",
			"Chrome_WidgetWin_1",
			"Chrome_WidgetWin_2",
			"Chrome_WidgetWin_3",
			// Yandex browser is just Chrome, but renamed
			"YandexBrowser_WidgetWin_0",
			"YandexBrowser_WidgetWin_1",
			"YandexBrowser_WidgetWin_2",
			"YandexBrowser_WidgetWin_3",
		};

		private static HashSet<IntPtr> processedWindowHandles = new HashSet<IntPtr> { IntPtr.Zero };

		static void Main(string[] args)
		{
			Console.WriteLine("UIA Lister");
			string path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "UIA.txt");
			Console.WriteLine("Writing to: " + path);
			using(var file = File.CreateText(path))
			{
				foreach (AutomationElement window in AutomationElement.RootElement.FindAll(TreeScope.Children, PropertyCondition.TrueCondition))
				{
					var className = window.Current.ClassName;
					file.WriteLine("," + window.Current.NativeWindowHandle.ToString("X") + "," + className);
					if (SupportedTopLevelWindowClasses.Contains(className))
					{
						Console.WriteLine("Listing contents of " + window.Current.Name);
						try
						{
							LogChildren(file, window, 1);
						}
						catch (Exception ex)
						{
							file.WriteLine("Exception: " + ex.Message);
						}
					}
				}	
			}

			System.Diagnostics.Process.Start(path);
		}

		private static void LogChildren(StreamWriter file, AutomationElement element, int depth)
		{
			var handle = (IntPtr)element.Current.NativeWindowHandle;
			processedWindowHandles.Add(handle);

			var children = new List<AutomationElement>(element.FindAll(TreeScope.Children, PropertyCondition.TrueCondition).OfType<AutomationElement>());
			foreach (var child in children)
			{
				processedWindowHandles.Add((IntPtr)child.Current.NativeWindowHandle);
			}

			if (handle != IntPtr.Zero)
			{
				var childWindowHandle = IntPtr.Zero;
				do
				{
					childWindowHandle = FindWindowEx(handle, childWindowHandle, null, null);

					if (!processedWindowHandles.Contains(childWindowHandle))
					{
						try
						{
							children.Add(AutomationElement.FromHandle(childWindowHandle));
						}
						catch (Exception ex)
						{
							file.WriteLine("Exception: " + ex.Message);
						}
					}

				} while (childWindowHandle != IntPtr.Zero);
			}

			foreach (AutomationElement child in children)
			{
				var childHandle = (IntPtr)child.Current.NativeWindowHandle;
				file.WriteLine(new String(' ', depth) + "," + childHandle.ToString("X") + "," + child.Current.ClassName + "," + child.Current.Name + "," + child.Current.AutomationId + "," + GetValueOrDefault(child, "") + "," + (child.Current.IsEnabled ? "UIA-Enabled" : "UIA-Disabled") + "," + (childHandle != IntPtr.Zero ? (IsWindowEnabled(childHandle) ? "hWnd:Enabled" : "hWnd:Disabled") : "hWnd:null"));

				LogChildren(file, child, depth + 1);
			}

			
		}

		private static string GetValueOrDefault(AutomationElement element, string defaultValue)
		{
			object valueElementObject;
			if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valueElementObject))
			{
				var valuePattern = valueElementObject as ValuePattern;
				if (valuePattern != null)
				{
					return valuePattern.Current.Value;
				}
			}
			return defaultValue;
		}
	}
}
