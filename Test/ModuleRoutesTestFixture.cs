﻿namespace Core.Test
{
    using Core;
    using Microsoft.Owin.Testing;
    using NUnit.Framework;
    using System.IO;
    using System.Net;
    using UI;
    using System.Linq;

    [TestFixture]
    public class ModuleRouteTestFixture 
    {
        protected TestServer _server;

        string[] GetEmbeddedResourcePaths() { return typeof(Hooker).Assembly.GetManifestResourceNames(); }

        string[] GetNonEmbeddedContentPaths() { return new string[] { "UI.Hooker.cs" };}

        [TestFixtureSetUp]
        public virtual void TestFixtureSetup()
        {
            _server = TestServer.Create<Startup>();
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            _server.Dispose();
        }

        [Category("Expecting Status Code 200")]
        [Test, Description("Send an HTTP request to Microsoft.Owin.StaticFiles for a embedded resource file in a different assembly (UI.dll) and verify the response.")]
        public void CanRetrieveEmbeddedResourceFromSeparateAssemblyWithHttp([ValueSource("GetEmbeddedResourcePaths")]string embeddedResourcePath)
        {
            // request the embedded resource served via HTTP from Owin.StaticFiles
            var response = _server.HttpClient.GetAsync(embeddedResourcePath.Replace("UI.", "/")).Result;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                
            MemoryStream assemblyMemoryStream = new MemoryStream();
                typeof(Hooker).Assembly.GetManifestResourceStream(embeddedResourcePath).CopyTo(assemblyMemoryStream);
            MemoryStream httpMemoryStream  = new MemoryStream();
                response.Content.ReadAsStreamAsync().Result.CopyTo(httpMemoryStream);

            // compare content returned via HTTP with manually retrieved from assembly by the [TestFixture]
            Assert.IsTrue(CompareMemoryStreams(assemblyMemoryStream, httpMemoryStream));
        }

        [Category("Expecting Status Code 404")]
        [Test, Description("Send an HTTP request to Microsoft.Owin.StaticFiles for a file in a different assembly (UI.dll) that is not an embedded resource and verify the response.")]
        public void CannotRetrieveOtherFiles([ValueSource("GetNonEmbeddedContentPaths")]string nonEmbeddedResourcePath)
        {
            // request the embedded resource served via HTTP from Owin.StaticFiles
            var response = _server.HttpClient.GetAsync(nonEmbeddedResourcePath.Replace("UI.", "/")).Result;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private static bool CompareMemoryStreams(MemoryStream ms1, MemoryStream ms2)
        {
            if (ms1.Length != ms2.Length)
                return false;

            ms1.Position = 0;
            ms2.Position = 0;

            var msArray1 = ms1.ToArray();
            var msArray2 = ms2.ToArray();

            return msArray1.SequenceEqual(msArray2);
        }
    }
}
