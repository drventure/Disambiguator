using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Accessibility;

namespace WebAutoType
{
	internal abstract class BrowserUrlReader
	{
		[DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		#region Factory
		public static BrowserUrlReader Create(IntPtr hwnd)
		{
			if (hwnd != IntPtr.Zero)
			{
				var className = GetWindowClassName(hwnd);

				if (className == "MozillaWindowClass") // new FF, 8.0 etc
				{
					return new FirefoxBrowserUrlReader(hwnd);
				}
				else if (className == "IEFrame")
				{
					return new InternetExplorerBrowserUrlReader(hwnd);
				}
				else if (className.StartsWith("Chrome_WidgetWin_") ||
						 className.StartsWith("YandexBrowser_WidgetWin_"))
				{
					return new ChromeBrowserUrlReader(hwnd);
				}
				else if (className == "ApplicationFrameWindow") // Edge (or, unfortunately, any other Metro app))
				{
					return new EdgeBrowserUrlReader(hwnd);
				}
			}
			return null;
		}

		public static bool IsWindowHandleSupportedBrowser(IntPtr hWnd)
		{
			var className = GetWindowClassName(hWnd);

			if (className == "MozillaWindowClass" ||
				className == "IEFrame" ||
				className.StartsWith("Chrome_WidgetWin_") || // Special case for Chrome which may append any number to the class name
				className.StartsWith("YandexBrowser_WidgetWin_") || // Yandex is just a renamed Chrome
				className == "ApplicationFrameWindow") // Edge (or, unfortunately, any other Metro app))
			{
				return true;
			}

			return false;
		}

		private static string GetWindowClassName(IntPtr hWnd)
		{
			// Pre-allocate 256 characters, since this is the maximum class name length.
			var classNameBuilder = new StringBuilder(256);
			// Get the window class name
			GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			var className = classNameBuilder.ToString();
			return className;
		}
		#endregion

		protected readonly IntPtr mHwnd;

		protected BrowserUrlReader(IntPtr hwnd)
		{
			mHwnd = hwnd;
		}

		/// <summary>
		/// Gets the URL of the top level browser window, ignoring keyboard focus
		/// </summary>
		public virtual string GetWindowUrl()
		{
			var doc = GetDocument();
			return doc == null ? null : GetDocumentUrl(doc);
		}

		/// <summary>
		/// Gets the URL of the frame of the browser that has the focus, and the focussed element
		/// </summary>
		public virtual string GetBrowserFocusUrl(out bool passwordFieldFocussed)
		{
			var windowDoc = GetDocument();
			if (windowDoc == null)
			{
				passwordFieldFocussed = false;
				return null;
			}
			var focusedElement = windowDoc.accFocus as IAccessible;
			if (focusedElement == null)
			{
				passwordFieldFocussed = false;
				return GetDocumentUrl(windowDoc);
			}
			passwordFieldFocussed = AccessibleObjectHelper.HasState(focusedElement, AccessibleStates.Protected);

			var focusDoc = GetElementParentDocument(focusedElement);
			if (focusDoc == null)
			{
				return GetDocumentUrl(windowDoc);
			}

			return GetDocumentUrl(focusDoc);
		}

		public virtual string GetBrowserFocusUrlWithInfo(out string title, out string selectedText)
		{
			var windowDoc = GetDocument();
			if (windowDoc == null)
			{
				title = null;
				selectedText = null;

				return null;
			}
			var focusedElement = windowDoc.accFocus as IAccessible;
			if (focusedElement == null)
			{
				selectedText = null;

				title = GetDocumentTitle(windowDoc);
				return GetDocumentUrl(windowDoc);
			}
			if ((int)focusedElement.accRole[0] == (int)AccessibleRole.Text)
			{
				selectedText = AccessibleObjectHelper.SafeGetValue(focusedElement);
			}
			else
			{
				selectedText = null;
			}

			var focusDoc = GetElementParentDocument(focusedElement) ?? windowDoc;

			title = GetDocumentTitle(focusDoc);
			return GetDocumentUrl(focusDoc);
		}

		protected abstract IAccessible GetDocument();

		protected virtual string GetDocumentUrl(IAccessible document)
		{
			return document.accValue[0];
		}

		protected virtual string GetDocumentTitle(IAccessible document)
		{
			return document.accName[0];
		}

		protected virtual IAccessible GetElementParentDocument(IAccessible element)
		{
			return AccessibleObjectHelper.FindAncestor(element, 
				role: AccessibleRole.Document,
				hasState: AccessibleStates.Focusable);
		}

		protected IEnumerable<IntPtr> FindDescendantWindows(IntPtr windowHandle, string className)
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
