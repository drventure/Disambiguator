using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Accessibility;

namespace WebAutoType
{
	internal static class AccessibleObjectHelper
	{
		[DllImport("oleacc.dll", ExactSpelling = true, PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		private static extern object AccessibleObjectFromWindow(
			IntPtr hwnd,
			uint dwObjectID,
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

		[DllImport("oleacc.dll")]
		private static extern uint WindowFromAccessibleObject(IAccessible pacc, out IntPtr phwnd);

		[DllImport("oleacc.dll")]
		private static extern uint AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren, [Out] object[] rgvarChildren, out int pcObtained);

		private static readonly Guid IID_IAccessible = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");

		private const int NAVDIR_FIRSTCHILD = 7;
		private const uint OBJID_CLIENT = 0xFFFFFFFC;
		private const uint OBJID_CARET = 0xFFFFFFF8;

		public static IAccessible GetAccessibleObjectFromWindow(IntPtr hwnd, uint objectID = 0)
		{
			return (IAccessible)AccessibleObjectFromWindow(hwnd, objectID, IID_IAccessible);
		}

		public static IEnumerable<IAccessible> GetChildren(IAccessible parent)
		{
			var children = new object[parent.accChildCount];

			int count;
			var result = AccessibleChildren(parent, 0, children.Length, children, out count);
			if (result != 0 && result != 1)
			{
				return new IAccessible[0];
			}
			if (count == 1 && children[0] is int)
			{
				var child = parent.accNavigate(NAVDIR_FIRSTCHILD, 0) as IAccessible;
				if (child == null)
				{
					return new IAccessible[0];
				}
				return new[] { child };
			}
			return children.OfType<IAccessible>();
		}

		public static IntPtr GetWindowHandleFromAccessibleObject(IAccessible accessibleObject)
		{
			IntPtr hwnd;
			WindowFromAccessibleObject(accessibleObject, out hwnd);
			return hwnd;
		}

		public static IAccessible FindChild(IAccessible parent, AccessibleRole? role = null, string customRole = null, AccessibleStates? hasState = null, AccessibleStates? hasNotState = null)
		{
			if (parent == null)
			{
				return null;
			}

			var children = GetChildren(parent).ToList();
			foreach (var child in children)
			{
				if (AccessibleObjectMatchesConditions(child, role, customRole, hasState, hasNotState))
				{
					return child;
				}
			}
			return null;
		}

		public static IAccessible FindAncestor(IAccessible child, AccessibleRole? role = null, string customRole = null, AccessibleStates? hasState = null, AccessibleStates? hasNotState = null)
		{
			var parent = child;
			while(parent != null)
			{
				if (AccessibleObjectMatchesConditions(parent, role, customRole, hasState, hasNotState))
				{
					return parent;
				}
				parent = parent.accParent as IAccessible;
			}
			return null;
		}

		private static bool AccessibleObjectMatchesConditions(IAccessible accessibleObject, AccessibleRole? role, string customRole, AccessibleStates? hasState = null, AccessibleStates? hasNotState = null)
		{
			try
			{
				object actualRole;
				try
				{
					actualRole = accessibleObject.accRole[0];
				}
				catch (COMException)
				{
					actualRole = "";
				}

				return (role == null || actualRole.Equals((int)role)) &&
						(customRole == null || actualRole.Equals(customRole)) &&
						(hasState == null || HasState(accessibleObject, hasState.Value)) &&
						(hasNotState == null || !HasState(accessibleObject, hasNotState.Value));
			}
			catch (NullReferenceException)
			{
				return false;
			}
		}

		public static bool HasState(IAccessible accessibleObject, AccessibleStates state)
		{
			return ((AccessibleStates)accessibleObject.accState[0] & state) != 0;
		}

		public static string SafeGetValue(IAccessible accessibleObject)
		{
			try
			{
				return accessibleObject.accValue[0];
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static string SafeGetName(IAccessible accessibleObject)
		{
			try
			{
				return accessibleObject.accName[0];
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
