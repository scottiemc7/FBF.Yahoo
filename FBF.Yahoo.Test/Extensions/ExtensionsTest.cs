using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FBF.Yahoo.Extensions;

namespace FBF.Yahoo.Test.Extensions
{
    [TestClass]
    public class ExtensionsTest
    {
        [TestMethod]
        public void UriExtensionsToUriWithSortedQueryString()
        {
            string sortedQuery = "http://www.testurl.com/path/to/something?paramA=valA&paramB=valB&zed=valZ";
            string unsortedQuery = "http://www.testurl.com/path/to/something?paramB=valB&zed=valZ&paramA=valA";
            Uri testUri = new Uri(unsortedQuery);

            Assert.AreEqual(sortedQuery, testUri.ToUriWithSortedQueryString());
        }

        [TestMethod]
        public void UriExtensionsToUriWithoutQueryString()
        {
            string uriWithQuery = "http://www.testurl.com/path/to/something?paramA=valA&paramB=valB&zed=valZ";
            string uriWithoutQuery = "http://www.testurl.com/path/to/something";
            Uri testUri = new Uri(uriWithQuery);

            Assert.AreEqual(uriWithoutQuery, testUri.ToUriWithoutQueryString());
        }
    }
}
