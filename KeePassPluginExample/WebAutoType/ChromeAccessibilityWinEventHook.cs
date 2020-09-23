using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WebAutoType
{
	/// <summary>
	/// Handles accessibility events as if a screen reader were running. Chrome detects this to automatically enable accessibility features.
	/// </summary>
	internal class ChromeAccessibilityWinEventHook : IDisposable
	{
		private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		[DllImport("user32.dll")]
		private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		[DllImport("user32.dll")]
		private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		[DllImport("User32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint nMsg, int wParam, int lParam);


		private const uint WINEVENT_OUTOFCONTEXT = 0;
		private const uint EVENT_SYSTEM_ALERT = 0x0002;

		private const uint WM_GETOBJECT = 0x3D;

		// ref: http://www.chromium.org/developers/design-documents/accessibility
		private const int GOOGLE_CHROME_ACCESSIBILITY_OBJECT_ID = 1;

		private readonly IntPtr mWinEventHook;
		private readonly WinEventDelegate mEventDelegate;

		public ChromeAccessibilityWinEventHook()
		{
			mEventDelegate = OnEventReceived;
			mWinEventHook = SetWinEventHook(EVENT_SYSTEM_ALERT, EVENT_SYSTEM_ALERT, IntPtr.Zero, mEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
		}

		private void OnEventReceived(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (idObject == GOOGLE_CHROME_ACCESSIBILITY_OBJECT_ID)
			{
				EventReceived = true;
				SendMessage(hwnd, WM_GETOBJECT, 0, idObject);
			}
		}

		public void Dispose()
		{
			UnhookWinEvent(mWinEventHook);
		}

		/// <summary>
		/// This flag is set true whenever an event is received.
		/// Set it false before the period of interest.
		/// </summary>
		public bool EventReceived { get; set; }
	}
}
