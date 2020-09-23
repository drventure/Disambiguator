using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WebAutoType
{
	// This class taken from: http://stackoverflow.com/questions/3568513/how-to-create-keyboard-shortcut-in-windows-that-call-function-in-my-app/3569097#3569097
	// And tweaked with answers in: http://stackoverflow.com/questions/15434505/key-capture-using-global-hotkey-in-c-sharp
	// And logic from KeePass HotKeyManager
	public class HotKeyManager
	{
		public static event EventHandler<HotKeyEventArgs> HotKeyPressed;

		public static int RegisterHotKey(Keys keys)
		{
			int id = System.Threading.Interlocked.Increment(ref _id);

			KeyModifiers modifiers = 0;
			if ((keys & Keys.Shift) != Keys.None) modifiers |= KeyModifiers.Shift;
			if ((keys & Keys.Alt) != Keys.None) modifiers |= KeyModifiers.Alt;
			if ((keys & Keys.Control) != Keys.None) modifiers |= KeyModifiers.Control;

			RegisterHotKey(_wnd.Handle, id, (uint)modifiers, (uint)(keys & Keys.KeyCode));
			return id;
		}

		public static bool UnregisterHotKey(int id)
		{
			return UnregisterHotKey(_wnd.Handle, id);
		}

		protected static void OnHotKeyPressed(HotKeyEventArgs e)
		{
			if (HotKeyManager.HotKeyPressed != null)
			{
				HotKeyManager.HotKeyPressed(null, e);
			}
		}

		private static MessageWindow _wnd = new MessageWindow();

		private class MessageWindow : NativeWindow, IDisposable
		{
			public MessageWindow()
			{
				CreateHandle(new CreateParams());
			}

			public void Dispose()
			{
				DestroyHandle();
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_HOTKEY)
				{
					HotKeyEventArgs e = new HotKeyEventArgs(m.LParam);
					HotKeyManager.OnHotKeyPressed(e);
				}

				base.WndProc(ref m);
			}

			private const int WM_HOTKEY = 0x312;
		}

		[DllImport("user32")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private static int _id = 0;
	}


	public class HotKeyEventArgs : EventArgs
	{
		public readonly Keys Key;
		public readonly KeyModifiers Modifiers;

		public HotKeyEventArgs(Keys key, KeyModifiers modifiers)
		{
			this.Key = key;
			this.Modifiers = modifiers;
		}

		public HotKeyEventArgs(IntPtr hotKeyParam)
		{
			uint param = (uint)hotKeyParam.ToInt64();
			Key = (Keys)((param & 0xffff0000) >> 16);
			Modifiers = (KeyModifiers)(param & 0x0000ffff);
		}
	}

	[Flags]
	public enum KeyModifiers
	{
		Alt = 1,
		Control = 2,
		Shift = 4,
		Windows = 8,
		NoRepeat = 0x4000
	}
}