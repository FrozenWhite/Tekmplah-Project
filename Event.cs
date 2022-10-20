namespace Teknomli
{
    internal interface IEvent
    {
        public struct Event
        {
            public string script;
            public string trigger;
            public string name;
        }

        void SetEvent();
        void RemoveEvent();
    }
}
