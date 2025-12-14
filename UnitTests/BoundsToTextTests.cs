using System;
using System.Drawing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disambiguator;

namespace UnitTests
{
    [TestClass]
    public class BoundsToTextTests
    {
        [TestMethod]
        public void Convert_WithEmptyBounds_ReturnsEmptyString()
        {
            var boundsToText = new BoundsToText(Rectangle.Empty);

            var result = boundsToText.Convert();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_WithScreenCapture_ReturnsSomeText()
        {
            var screenBounds = new Rectangle(0, 0, 300, 100);
            var boundsToText = new BoundsToText(screenBounds);

            var result = boundsToText.Convert();

            result.Should().NotBeNull();
        }
    }
}
