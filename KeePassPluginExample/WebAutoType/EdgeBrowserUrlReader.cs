using System;
using System.Linq;
using System.Windows.Forms;
using Accessibility;

namespace WebAutoType
{
	internal class EdgeBrowserUrlReader : BrowserUrlReader
	{
		public EdgeBrowserUrlReader(IntPtr hwnd) : base(hwnd)
		{
		}

		public override string GetWindowUrl()
		{
			var coreWindowHwnd = FindDescendantWindows(mHwnd, "Windows.UI.Core.CoreWindow").FirstOrDefault();
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
			var address = addressBar.accValue[0];
			if (!address.Contains("://"))
			{
				address = "http://" + address; // If we can't tell if it is https or http, default to less secure assumption (insufficient justification to assume secure)
			}

			return address;
		}

		public override string GetBrowserFocusUrl(out bool passwordFieldFocussed)
		{
			// Not supported
			passwordFieldFocussed = false;
			return GetWindowUrl();
		}

		public override string GetBrowserFocusUrlWithInfo(out string title, out string selectedText)
		{
			// Not supported
			title = null;
			selectedText = null;
			return GetWindowUrl();
		}

		protected override IAccessible GetDocument()
		{
			return null;
		}
	}
}