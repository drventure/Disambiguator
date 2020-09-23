using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WebAutoType
{
	/// <summary>
	/// Class to retrive URL from specified window (if it's a browser window)
	/// </summary>
	public static class WebBrowserUrl
	{
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		/// <summary>Temporary holder during enumeration</summary>
		private static List<IntPtr> sTopLevelBrowserWindowHandles;

		/// <summary>
		/// Gets URLs for all top-level supported browser windows
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<String> GetTopLevelBrowserWindowUrls()
		{
			sTopLevelBrowserWindowHandles = new List<IntPtr>();
			EnumWindows(EnumWindows, IntPtr.Zero);
			var urls = new List<String>(sTopLevelBrowserWindowHandles.Count);
			foreach (var hwnd in sTopLevelBrowserWindowHandles)
			{
				var windowUrl = BrowserUrlReader.Create(hwnd).GetWindowUrl();
				if (!String.IsNullOrEmpty(windowUrl))
				{
					urls.Add(windowUrl);
				}
			}
			return urls;
		}

		private static bool EnumWindows(IntPtr hWnd, IntPtr lParam)
		{
			if (IsWindowVisible(hWnd) && BrowserUrlReader.IsWindowHandleSupportedBrowser(hWnd))
			{
				sTopLevelBrowserWindowHandles.Add(hWnd);
			}
			return true;
		}

		/// <summary>
		/// Gets the URL from the browser with the current focus. If there is no current focus, falls back on trying to get the active URL from
		/// the fallback top-level window handle specified.
		/// 
		/// If the current focus is detected to be in a password field, passwordFieldFocussed is set true.
		/// </summary>
		internal static string GetFocusedBrowserUrl(ChromeAccessibilityWinEventHook chromeAccessibility, IntPtr hwnd, out bool passwordFieldFocussed)
		{
			var browserUrlReader = BrowserUrlReader.Create(hwnd);
			if (browserUrlReader is ChromeBrowserUrlReader)
			{
				((ChromeBrowserUrlReader)browserUrlReader).ChromeAccessibilityWinEventHook = chromeAccessibility;
			}

			return browserUrlReader.GetBrowserFocusUrl(out passwordFieldFocussed);
		}

		internal static void GetFocusedBrowserInfo(ChromeAccessibilityWinEventHook chromeAccessibility, out string selectedText, out string url, out string title)
		{
			var browserUrlReader = BrowserUrlReader.Create(GetForegroundWindow());
			if (browserUrlReader is ChromeBrowserUrlReader)
			{
				((ChromeBrowserUrlReader)browserUrlReader).ChromeAccessibilityWinEventHook = chromeAccessibility;
			}

			if (browserUrlReader == null)
			{
				selectedText = null;
				url = null;
				title = null;
			}
			else
			{
				url = browserUrlReader.GetBrowserFocusUrlWithInfo(out title, out selectedText);
			}
		}
	}
}
