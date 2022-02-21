using Alvr;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Editor
{
    public class EventJsonTest
    {
        [Test]
        public void TestInitial()
        {
            const string eventJson = @"
                {
                  ""type"": ""Initial""
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "Initial");
        }

        [Test]
        public void TestServerFound()
        {
            const string eventJson = @"
                {
                  ""type"": ""ServerFound"",
                  ""ipaddr"": ""192.168.1.1""
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "ServerFound");
            var value = new ClientEventServerFound();
            JsonUtility.FromJsonOverwrite(eventJson, value);
            Assert.AreEqual(value.ipaddr, "192.168.1.1");
        }

        [Test]
        public void TestConnectedWithoutFfrParam()
        {
            const string eventJson = @"
                {
                  ""type"": ""Connected"",
                  ""settings"": {
                    ""fps"": 60.0,
                    ""codec"": { ""type"": ""H264"" },
                    ""realtime"": true,
                    ""dashboard_url"": ""http://192.168.1.1:8082/"",
                    ""ffr_param"": null
                  }
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "Connected");
            var value = new ClientEventConnected();
            JsonUtility.FromJsonOverwrite(eventJson, value);
            Assert.AreEqual(value.settings.fps, 60);
            Assert.AreEqual(value.settings.codec.type, "H264");
            Assert.AreEqual(value.settings.realtime, true);
            Assert.AreEqual(value.settings.dashboard_url, "http://192.168.1.1:8082/");
        }

        [Test]
        public void TestConnected()
        {
            const string eventJson = @"
                {
                  ""type"": ""Connected"",
                  ""settings"": {
                    ""fps"": 60.0,
                    ""codec"": { ""type"": ""H264"" },
                    ""realtime"": true,
                    ""dashboard_url"": ""http://192.168.1.1:8082/"",
                    ""ffr_param"": {
                      ""eye_width"": 1920,
                      ""eye_height"": 1080,
                      ""center_size_x"": 1.0,
                      ""center_size_y"": 2.0,
                      ""center_shift_x"": 3.0 ,
                      ""center_shift_y"": 4.0,
                      ""edge_ratio_x"": 5.0,
                      ""edge_ratio_y"": 6.0
                    }
                  }
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "Connected");
            var value = new ClientEventConnected();
            JsonUtility.FromJsonOverwrite(eventJson, value);
            Assert.AreEqual(value.settings.fps, 60);
            Assert.AreEqual(value.settings.codec.type, "H264");
            Assert.AreEqual(value.settings.realtime, true);
            Assert.AreEqual(value.settings.dashboard_url, "http://192.168.1.1:8082/");
        }

        [Test]
        public void TestStreamStart()
        {
            const string eventJson = @"
                {
                  ""type"": ""StreamStart""
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "StreamStart");
        }

        [Test]
        public void TestServerRestart()
        {
            const string eventJson = @"
                {
                  ""type"": ""ServerRestart""
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "ServerRestart");
        }

        [Test]
        public void TestError()
        {
            const string eventJson = @"
                {
                  ""type"": ""Error"",
                  ""error"": {
                    ""type"": ""NetworkUnreachable""
                  }
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "Error");
            var value = new ClientEventError();
            JsonUtility.FromJsonOverwrite(eventJson, value);
            Assert.AreEqual(value.error.type, "NetworkUnreachable");
            Assert.AreEqual(value.error.cause, null);
        }

        [Test]
        public void TestErrorWithCause()
        {
            const string eventJson = @"
                {
                  ""type"": ""Error"",
                  ""error"": {
                    ""type"": ""ServerDisconnected"",
                    ""cause"": ""any_cause""
                  }
                }
            ";
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            Assert.AreEqual(e.type, "Error");
            var value = new ClientEventError();
            JsonUtility.FromJsonOverwrite(eventJson, value);
            Assert.AreEqual(value.error.type, "ServerDisconnected");
            Assert.AreEqual(value.error.cause, "any_cause");
        }
    }
}