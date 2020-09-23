using System;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Disambiguator;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class MiscTests
    {
        [TestMethod]
        public void ParseTest()
        {
            var depthItems = new Dictionary<int, int>();
            int depth = 0;

            string input = "Title{app:testing}{title:{one more test}}and more";

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    depth++;
                    depthItems.Add(depth, i);
                }
                else if (input[i] == '}')
                {
                    int item = depthItems[depth];
                    depthItems.Remove(depth);
                    depth--;
                    System.Diagnostics.Debug.Write($"Found item at ({item}, {i}) -> ");
                    System.Diagnostics.Debug.WriteLine(input.Substring(item + 1, i - item - 1));
                }
            }
        }
    }
}
