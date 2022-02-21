using System;
using System.Diagnostics.CodeAnalysis;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace Alvr
{
    [RequireComponent(typeof(AlvrClient))]
    public class ClientEventMonitor : MonoBehaviour
    {
        [SerializeField] private UnityEvent<string> onEventOccured;

        private AlvrClient _alvrClient;
        private ClientEventObserver _eventObserver;

        private void Awake()
        {
            _alvrClient = GetComponent<AlvrClient>();
            var eventSubject = new Subject<string>();
            eventSubject
                .ObserveOnMainThread()
                .Subscribe(eventJson =>
                {
                    onEventOccured.Invoke(eventJson);
                });
            _eventObserver = new ClientEventObserver(eventSubject);
            _alvrClient.SetEventObserver(_eventObserver);
        }

        private void OnDestroy()
        {
            _alvrClient.SetEventObserver(null);
        }
    }

    // Implement interface on Android
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class ClientEventObserver : AndroidJavaProxy
    {
        private readonly IObserver<string> observer;

        public ClientEventObserver(IObserver<string> observer) : base("io.github.alvr.android.lib.ClientEventObserver")
        {
            this.observer = observer;
        }

        public void onEventOccurred(string eventJson)
        {
            observer.OnNext(eventJson);
        }
    }
}