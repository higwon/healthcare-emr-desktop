using System;
using HealthNote.Desktop.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class DemoOptionsTests
    {
        [TestMethod]
        public void DefaultAndExplicitLoopback_AreAccepted()
        {
            Assert.AreEqual("http://127.0.0.1:5078/", DemoOptions.Parse(Array.Empty<string>()).ApiBaseUri.AbsoluteUri);
            DemoOptions options = DemoOptions.Parse(new[] { "--smoke", "--api-base-url", "http://127.0.0.1:5800/" });
            Assert.IsTrue(options.Smoke);
            Assert.AreEqual(5800, options.ApiBaseUri.Port);
        }

        [TestMethod]
        [DataRow("https://127.0.0.1:5078/")]
        [DataRow("http://example.com/")]
        [DataRow("http://localhost:5078/")]
        [DataRow("http://127.0.0.1:5078/api/")]
        [DataRow("http://user:secret@127.0.0.1:5078/")]
        [DataRow("http://127.0.0.1:5078/?token=secret")]
        [DataRow("http://127.0.0.1:5078/#secret")]
        [DataRow("http://127.0.0.1:0/")]
        [DataRow("not-a-uri")]
        public void UnsafeOrMalformedAddress_IsRejected(string address)
        {
            Assert.ThrowsExactly<ArgumentException>(() => DemoOptions.Parse(new[] { "--api-base-url", address }));
        }

        [TestMethod]
        public void UnknownMissingAndDuplicateOptions_AreRejected()
        {
            Assert.ThrowsExactly<ArgumentException>(() => DemoOptions.Parse(new[] { "--unknown" }));
            Assert.ThrowsExactly<ArgumentException>(() => DemoOptions.Parse(new[] { "--api-base-url" }));
            Assert.ThrowsExactly<ArgumentException>(() => DemoOptions.Parse(new[] {
                "--api-base-url", "http://127.0.0.1:5078/", "--api-base-url", "http://127.0.0.1:5080/" }));
        }
    }
}
