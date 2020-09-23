using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Accessibility;
using WebAutoType;

namespace MSAALister
{
	class Program
	{
		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", EntryPoint = "FindWindowEx")]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();


		private const uint OBJID_WINDOW = 0x00000000;
		private const uint OBJID_SELF = 0x00000000;
		private const uint OBJID_SYSMENU = 0xFFFFFFFF;
		private const uint OBJID_TITLEBAR = 0xFFFFFFFE;
		private const uint OBJID_MENU = 0xFFFFFFFD;
		private const uint OBJID_CLIENT = 0xFFFFFFFC;
		private const uint OBJID_VSCROLL = 0xFFFFFFFB;
		private const uint OBJID_HSCROLL = 0xFFFFFFFA;
		private const uint OBJID_SIZEGRIP = 0xFFFFFFF9;
		private const uint OBJID_CARET = 0xFFFFFFF8;
		private const uint OBJID_CURSOR = 0xFFFFFFF7;
		private const uint OBJID_ALERT = 0xFFFFFFF6;
		private const uint OBJID_SOUND = 0xFFFFFFF5;
		private const uint OBJID_NATIVEOM = 0xFFFFFFF0;

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

		private static readonly HashSet<IntPtr> sProcessedWindowHandles = new HashSet<IntPtr> { IntPtr.Zero };
		private static StreamWriter sFile;

		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("MSAA Lister");

			string path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "MSAA.txt");
			Console.WriteLine("Writing to: " + path);
			using (sFile = File.CreateText(path))
			{
				EnumWindows(EnumWindows, IntPtr.Zero);
			}

			System.Diagnostics.Process.Start(path);
		}

		private static bool EnumWindows(IntPtr hWnd, IntPtr lParam)
		{
			var className = GetClassName(hWnd);

			sFile.WriteLine("{0}[{1}]", hWnd.ToString("X"), className);

			if (className == "MozillaWindowClass")
			{
				WriteUrl(GetFirefoxUrl(hWnd));
			}
			else if (className == "IEFrame")
			{
				WriteUrl(GetInternetExplorerUrl(hWnd));
			}
			else if (className == "ApplicationFrameWindow")
			{
				WriteUrl(GetEdgeUrl(hWnd));
			}
			else if (className.StartsWith("Chrome_WidgetWin_") ||
			         className.StartsWith("YandexBrowser_WidgetWin_"))
			{
				WriteUrl(GetChromeUrl(hWnd));
			}

			if (SupportedTopLevelWindowClasses.Contains(className))
			{
				try
				{
					var accessibleObject = AccessibleObjectHelper.GetAccessibleObjectFromWindow(hWnd);

					try
					{
						LogAccessibleObject(0, hWnd, accessibleObject);
					}
					catch (Exception ex)
					{
						sFile.WriteLine("Exception: " + ex.Message);
					}

					LogChildren(accessibleObject, 1);
				}
				catch (Exception ex)
				{
					sFile.WriteLine("Exception: " + ex.Message);
				}
			}
			return true;
		}

		private static void WriteUrl(string url)
		{
			if (url != null)
			{
				sFile.WriteLine("URL found: " + url);
			}
		}

		private static string GetClassName(IntPtr hWnd)
		{
			var classNameBuilder = new StringBuilder(256);
			GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			var className = classNameBuilder.ToString();
			return className;
		}

		private static readonly Stack<IAccessible> sLogStack = new Stack<IAccessible>();

		private static void LogChildren(IAccessible accessibleObject, int depth)
		{
			var hWnd = AccessibleObjectHelper.GetWindowHandleFromAccessibleObject(accessibleObject);
			sProcessedWindowHandles.Add(hWnd);

			sLogStack.Push(accessibleObject);

			var children = AccessibleObjectHelper.GetChildren(accessibleObject).ToList();

			if (children.Any())
			{
				sFile.WriteLine(new String(' ', depth) + "==Accessible Children==");
			}

			foreach (var child in children)
			{
				if (!sLogStack.Contains(child, new AccessibleObjectComparer())) // Avoid infinite recursion
				{
					var childWindowHandle = AccessibleObjectHelper.GetWindowHandleFromAccessibleObject(child);
					sProcessedWindowHandles.Add(childWindowHandle);

					try
					{
						LogAccessibleObject(depth, childWindowHandle, child);
					}
					catch (Exception ex)
					{
						sFile.WriteLine("Exception: " + ex.Message);
					}
					LogChildren(child, depth + 1);
				}
			}

			var first = true;
			if (hWnd != IntPtr.Zero)
			{
				var childWindowHandle = IntPtr.Zero;
				do
				{
					childWindowHandle = FindWindowEx(hWnd, childWindowHandle, null, null);

					if (!sProcessedWindowHandles.Contains(childWindowHandle))
					{
						if (first)
						{
							first = false;
							sFile.WriteLine(new String(' ', depth) + "==Window Children==");
						}

						sProcessedWindowHandles.Add(childWindowHandle);
						try
						{
							var child = AccessibleObjectHelper.GetAccessibleObjectFromWindow(childWindowHandle);
							LogAccessibleObject(depth, childWindowHandle, child);
							LogChildren(child, depth + 1);
						}
						catch (Exception ex)
						{
							sFile.WriteLine("Exception: " + ex.Message);
						}
					}

				} while (childWindowHandle != IntPtr.Zero);
			}

			if (!first || children.Any())
			{
				sFile.WriteLine(new String(' ', depth) + "====");
			}

			sLogStack.Pop();
		}

		private class AccessibleObjectComparer : IEqualityComparer<IAccessible>
		{
			public bool Equals(IAccessible x, IAccessible y)
			{
				return ReferenceEquals(x, y) || (Marshal.GetIUnknownForObject(x) == Marshal.GetIUnknownForObject(y));
			}

			public int GetHashCode(IAccessible obj)
			{
				return obj.GetHashCode();
			}
		}

		private static void LogAccessibleObject(int depth, IntPtr hWnd, IAccessible child)
		{
			sFile.Write(new String(' ', depth));
			string value;
			try
			{
				value = child.accValue[0];
			}
			catch (Exception)
			{
				value = "";
			}

			string roleString;

			try
			{
				roleString = child.accRole[0] as string;

				if (child.accRole[0] is int)
				{
					roleString = Enum.GetName(typeof(AccessibleRole), (int)child.accRole[0]);
				}
			}
			catch (NullReferenceException)
			{
				roleString = "<undefined role>";
			}

			int left, width, top, height;
			try
			{
				child.accLocation(out left, out top, out width, out height);
			}
			catch (Exception)
			{
				left = width = top = height = -1;
			}

			string name;
			try
			{
				name = child.accName[0];
			}
			catch (NullReferenceException)
			{
				name = "<no name>";
			}
			
			sFile.WriteLine("{0}[{1}]:{2}={3}, Role:{4}, State:{5}, Pos:{6},{7}", hWnd.ToString("X"), GetClassName(hWnd), name, value, roleString, child.accState[0], left, top);
		}


		[DllImport("User32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, WM nMsg, int wParam, int lParam);

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private const int SELFLAG_TAKEFOCUS = 1;

		private static string GetFirefoxUrl(IntPtr hwnd)
		{
			var propertyPage = AccessibleObjectHelper.FindChild(AccessibleObjectHelper.FindChild(AccessibleObjectHelper.FindChild(AccessibleObjectHelper.GetAccessibleObjectFromWindow(hwnd),
				role: AccessibleRole.Application),
					role: AccessibleRole.Grouping,
					hasNotState: AccessibleStates.Invisible),
						role: AccessibleRole.PropertyPage,
						hasNotState: AccessibleStates.Offscreen /*(inactive tab)*/);

			var browser = AccessibleObjectHelper.FindChild(propertyPage, customRole: "browser, http://www.mozilla.org/keymaster/gatekeeper/there.is.only.xul") // Firefox 59+
			           ?? AccessibleObjectHelper.FindChild(propertyPage, customRole: "browser"); // Firefox <59

			var doc = AccessibleObjectHelper.FindChild(browser, role: AccessibleRole.Document);

			return doc?.accValue[0];
		}

		private static string GetInternetExplorerUrl(IntPtr hwnd)
		{
			var ieServerHwnd = FindDescendantWindows(hwnd, "Internet Explorer_Server").First();
			var ieServer = AccessibleObjectHelper.GetAccessibleObjectFromWindow(ieServerHwnd);

			if (ieServer == null)
			{
				return null;
			}
			/*
			var test = ieServer.accFocus as IAccessible;
			LogAccessibleObject(0, hwnd, test);

			ieServer = AccessibleObjectHelper.FindAncestor(test, AccessibleRole.Client);
			*/
			return ieServer.accName[0];
		}

		private static string GetChromeUrl(IntPtr hwnd)
		{
			var chromeRenderHwnd = FindDescendantWindows(hwnd, "Chrome_RenderWidgetHostHWND").FirstOrDefault();
			if (chromeRenderHwnd == IntPtr.Zero)
			{
				return null;
			}
			var doc = AccessibleObjectHelper.FindChild(AccessibleObjectHelper.GetAccessibleObjectFromWindow(chromeRenderHwnd),
				role: AccessibleRole.Document);
			if (doc == null)
			{
				return null;
			}
			
			var test = doc.accFocus as IAccessible;
			if (test != null)
			{
				LogAccessibleObject(0, hwnd, test);
				return doc.accValue[0];
			}

			return null;
		}

		private static string GetEdgeUrl(IntPtr hwnd)
		{
			var coreWindowHwnd = FindDescendantWindows(hwnd, "Windows.UI.Core.CoreWindow").FirstOrDefault();
			if (coreWindowHwnd == IntPtr.Zero)
			{
				return null;
			}
			var addressBar = AccessibleObjectHelper.FindChild(AccessibleObjectHelper.FindChild(AccessibleObjectHelper.GetAccessibleObjectFromWindow(coreWindowHwnd),
				role: AccessibleRole.Window),
					role: AccessibleRole.Text);
			if (addressBar == null)
			{
				return null;
			}
			return addressBar.accValue[0];
		}

		private static IEnumerable<IntPtr> FindDescendantWindows(IntPtr windowHandle, string className)
		{
			var results = new List<IntPtr>();

			// Recurse into children
			var anyChildren = false;
			// First list any results
			var childWindowHandle = IntPtr.Zero;
			do
			{
				childWindowHandle = FindWindowEx(windowHandle, childWindowHandle, null, null);
				if (childWindowHandle != IntPtr.Zero)
				{
					anyChildren = true;
					results.AddRange(FindDescendantWindows(childWindowHandle, className));
				}
			} while (childWindowHandle != IntPtr.Zero);

			if (anyChildren)
			{
				// Now add any results at this level
				childWindowHandle = IntPtr.Zero;
				do
				{
					childWindowHandle = FindWindowEx(windowHandle, childWindowHandle, className, null);
					if (childWindowHandle != IntPtr.Zero)
					{
						results.Add(childWindowHandle);
					}
				} while (childWindowHandle != IntPtr.Zero);
			}

			return results;
		}
	}
}
