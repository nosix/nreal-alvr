using UnityEngine;
using UnityEngine.UI;

namespace Alvr
{
    public class StatusMessage : MonoBehaviour
    {
        [SerializeField] private AlvrClient alvrClient;
        [SerializeField] private Text statusMessageText;

        private void OnEnable()
        {
            statusMessageText.text = "Searching for a ALVR server...";
        }

        public void OnEventOccured(string eventJson)
        {
            var e = new ClientEvent();
            JsonUtility.FromJsonOverwrite(eventJson, e);
            switch (e.type)
            {
                case "Initial":
                    gameObject.SetActive(true);
                    break;
                case "ServerFound":
                    var serverFoundEvent = new ClientEventServerFound();
                    JsonUtility.FromJsonOverwrite(eventJson, serverFoundEvent);
                    statusMessageText.text = $"Discover the server in {serverFoundEvent.ipaddr}.";
                    break;
                case "Connected":
                    var connectedEvent = new ClientEventConnected();
                    JsonUtility.FromJsonOverwrite(eventJson, connectedEvent);
                    statusMessageText.text =
                        $"Connected to the server.\nThe dashboard URL is {connectedEvent.settings.dashboard_url}";
                    break;
                case "StreamStart":
                    gameObject.SetActive(false);
                    break;
                case "Error":
                    var errorEvent = new ClientEventError();
                    JsonUtility.FromJsonOverwrite(eventJson, errorEvent);
                    SetError(errorEvent.error.type, errorEvent.error.cause);
                    break;
            }
        }

        private void SetError(string errorType, string cause)
        {
            statusMessageText.text = errorType switch
            {
                "NetworkUnreachable" => "The server is unreachable.",
                "ClientUntrusted" => $"The client is not trusted.\nRegister '{alvrClient.GetHostName()}' with the server.",
                "IncompatibleVersions" => $"The server version is not {AlvrVersion.MajorVersion}.x.x.",
                "TimeoutSetUpStream" => "The stream could not be started.",
                "ServerDisconnected" => "It have been disconnected from the server.",
                "SystemError" => $"System Error: {cause}",
                _ => statusMessageText.text
            };
        }
    }
}