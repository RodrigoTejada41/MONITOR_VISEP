using System;
namespace Visep
{
    public sealed class Session
    {
        public string User
        {
            get;
            private set;
        }
        public string Role
        {
            get;
            private set;
        }
        internal Session(string u,string r)
        {
            User=u;
            Role=r;
        }
    }
    public sealed class Client
    {
        public string Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public string Account
        {
            get;
            set;
        }
        public string Address
        {
            get;
            set;
        }
        public string Contacts
        {
            get;
            set;
        }
        public string Equipment
        {
            get;
            set;
        }
        public string Zones
        {
            get;
            set;
        }
    }
    public sealed class Incident
    {
        public string Id
        {
            get;
            set;
        }
        public string Account
        {
            get;
            set;
        }
        public string ClientName
        {
            get;
            set;
        }
        public string Code
        {
            get;
            set;
        }
        public string Zone
        {
            get;
            set;
        }
        public string Partition
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
        public string Owner
        {
            get;
            set;
        }
        public string Actions
        {
            get;
            set;
        }
        public string Raw
        {
            get;
            set;
        }
        public DateTime ReceivedUtc
        {
            get;
            set;
        }
        public DateTime? OriginUtc
        {
            get;
            set;
        }
        public bool IsSimulation
        {
            get;
            set;
        }
    }
    public sealed class LegacyEvent
    {
        public string Id { get; internal set; }
        public string SourceClientId { get; internal set; }
        public string Account { get; internal set; }
        public string ClientName { get; internal set; }
        public string Code { get; internal set; }
        public string Zone { get; internal set; }
        public string OccurredLocal { get; internal set; }
        public string Detail { get; internal set; }
    }
    public sealed class AuditEntry
    {
        public DateTime AtUtc
        {
            get;
            set;
        }
        public string User
        {
            get;
            set;
        }
        public string Action
        {
            get;
            set;
        }
        public string Target
        {
            get;
            set;
        }
    }
}
