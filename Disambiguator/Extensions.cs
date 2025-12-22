using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Disambiguator
{
    internal static class Extensions
    {

        internal static Rectangle GetBounds(this AutomationElement uiaObject) 
        {
            var bounds = Rectangle.Empty;
            object r = uiaObject.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            if (!object.ReferenceEquals(r, null))
            {
                // BoundingRectangleProperty returns a System.Windows.Rect from UIAutomationTypes
                // We need to use reflection to access its properties to avoid the dynamic type issue
                var rectType = r.GetType();
                var x = (double)rectType.GetProperty("X").GetValue(r, null);
                var y = (double)rectType.GetProperty("Y").GetValue(r, null);
                var width = (double)rectType.GetProperty("Width").GetValue(r, null);
                var height = (double)rectType.GetProperty("Height").GetValue(r, null);
                var isEmpty = (bool)rectType.GetProperty("IsEmpty").GetValue(r, null);

                if (!isEmpty)
                {
                    bounds = new Rectangle((int)x, (int)y, (int)width, (int)height);
                }
            }
            return bounds;
        }
    }
}
