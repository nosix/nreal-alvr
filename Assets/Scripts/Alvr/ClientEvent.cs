// ReSharper disable UnassignedField.Global
// ReSharper disable InconsistentNaming
using System;

namespace Alvr
{
    [Serializable]
    public class ClientEvent
    {
        public string type;
    }

    [Serializable]
    public class ClientEventServerFound
    {
        public string ipaddr;
    }

    [Serializable]
    public class ClientEventConnected
    {
        public Settings settings;

        [Serializable]
        public class Settings
        {
            public float fps;
            public Codec codec;
            public bool realtime;
            public string dashboard_url;
        }

        [Serializable]
        public class Codec
        {
            public string type;
        }

    }

    [Serializable]
    public class ClientEventError
    {
        public Error error;

        [Serializable]
        public class Error
        {
            public string type;
            public string cause;
        }
    }
}