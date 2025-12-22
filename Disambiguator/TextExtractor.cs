using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace Disambiguator
{
    /// <summary>
    /// Extracts Text content from an hwnd
    /// if it's a WebView control, try various methods, 
    /// but if not, just try image to text
    /// </summary>
    internal static class TextExtractor
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        /// <summary>
        /// Attempts to extract the content from a control given its window handle
        /// </summary>
        /// <param name="hWnd">Window handle of a control</param>
        /// <returns>Text content as string, or empty string if extraction fails</returns>
        internal static string ExtractText(UIElement uiElement)
        {
            if (uiElement == null) return string.Empty;
            if (uiElement.hWnd == IntPtr.Zero) return string.Empty;  

            DisambiguatorExt.Debug("Attempting to extract Text from control HWND: " + uiElement.hWnd.ToString("X"));
            try
            {
                // Try multiple approaches to extract Text

                // Approach 1: Try to get HTML via UI Automation Value pattern
                string text = TryGetHtmlViaValuePattern(uiElement);
                if (!string.IsNullOrEmpty(text)) return text;

                // Approach 2: Try to traverse document via UI Automation
                text = TryGetHtmlViaDocumentTraversal(uiElement);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                // Approach 3: Try to get visible text content
                text = TryGetVisibleText(uiElement);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                DisambiguatorExt.Debug("Error extracting Text from control: " + ex.ToString());
                return string.Empty;
            }
        }


        /// <summary>
        /// Try to get HTML content using UI Automation Value pattern
        /// </summary>
        private static string TryGetHtmlViaValuePattern(UIElement uiElement)
        {
            if (uiElement.uiaObject == null) return string.Empty;

            try
            {

                // Look for document element
                AutomationElement documentElement = uiElement.uiaObject.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
                );

                if (documentElement != null)
                {
                    DisambiguatorExt.Debug("Found Document Element for control HWND: " + uiElement.hWnd.ToString("X"));
                    // Try to get Value pattern which may contain HTML
                    object patternObj;
                    if (documentElement.TryGetCurrentPattern(ValuePattern.Pattern, out patternObj))
                    {
                        ValuePattern valuePattern = patternObj as ValuePattern;
                        if (valuePattern != null)
                        {
                            DisambiguatorExt.Debug("Extracted Value: " + valuePattern.Current.Value);
                            return valuePattern.Current.Value;
                        }
                    }

                    // Try to get Text pattern
                    if (documentElement.TryGetCurrentPattern(TextPattern.Pattern, out patternObj))
                    {
                        TextPattern textPattern = patternObj as TextPattern;
                        if (textPattern != null)
                        {
                            TextPatternRange[] ranges = textPattern.GetVisibleRanges();
                            if (ranges != null && ranges.Length > 0)
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (TextPatternRange range in ranges)
                                {
                                    sb.Append(range.GetText(-1));
                                }
                                DisambiguatorExt.Debug("Extracted Text Pattern: " + sb.ToString());
                                return sb.ToString();
                            }
                        }
                    }
                }

                DisambiguatorExt.Debug("Unable to extract text via Value Pattern.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                DisambiguatorExt.Debug("Error in TryGetHtmlViaValuePattern: " + ex.ToString());
                return string.Empty;
            }
        }


        /// <summary>
        /// Try to get HTML by traversing the document structure
        /// </summary>
        private static string TryGetHtmlViaDocumentTraversal(UIElement uiElement)
        {
            if (uiElement.uiaObject == null) return string.Empty;

            try
            {
        
                // Find all text elements and hyperlinks
                StringBuilder htmlBuilder = new StringBuilder();

                // Search for all text and hyperlink elements
                Condition condition = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
                );

                AutomationElementCollection elements = uiElement.uiaObject.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement child in elements)
                {
                    try
                    {
                        string name = child.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            htmlBuilder.AppendLine(name);
                        }

                        // Try to get help text which might contain more info
                        string helpText = child.GetCurrentPropertyValue(AutomationElement.HelpTextProperty) as string;
                        if (!string.IsNullOrWhiteSpace(helpText))
                        {
                            htmlBuilder.AppendLine(helpText);
                        }
                    }
                    catch
                    {
                        // Skip elements that fail
                        continue;
                    }
                }

                DisambiguatorExt.Debug("Extracted Text via document traversal: " + htmlBuilder.ToString());
                return htmlBuilder.ToString();
            }
            catch (Exception ex)
            {
                DisambiguatorExt.Debug("Error in TryGetHtmlViaDocumentTraversal: " + ex.ToString());
                return string.Empty;
            }
        }


        /// <summary>
        /// Try to get visible text content from the WebView2 control
        /// </summary>
        private static string TryGetVisibleText(UIElement uiElement)
        {
            if (uiElement.uiaObject == null) return string.Empty;   

            try
            {
                if (!uiElement.Bounds.IsEmpty)
                {
                    // Use the existing BoundsToText functionality
                    var text = new BoundsToText(uiElement.Bounds).Convert();
                    DisambiguatorExt.Debug("Extracted Visible Text: " + text);
                    return text;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                DisambiguatorExt.Debug("Error in TryGetVisibleText: " + ex.ToString());
                return string.Empty;
            }
        }


        /// <summary>
        /// Helper method to determine if a window handle belongs to a WebView2 control
        /// </summary>
        /// <param name="hWnd">Window handle to check</param>
        /// <returns>True if the window is likely a WebView2 control</returns>
        public static bool IsWebView2Control(IntPtr hWnd)
        {
            try
            {
                StringBuilder className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                string classNameStr = className.ToString().ToLower();

                // WebView2 uses Chrome/Edge class names
                return classNameStr.Contains("chrome") ||
                       classNameStr.Contains("webview") ||
                       classNameStr.Contains("edge");
            }
            catch
            {
                return false;
            }
        }
    }
}