using System;

namespace MediaBrowser.Model.Entities
{
    public class MBRegistrationRecord
    {
        public DateTime ExpirationDate { get; set; }
        public bool IsRegistered { get; set; }
        public bool RegChecked { get; set; }
        public bool RegError { get; set; }
        public bool TrialVersion { get; set; }
        public bool IsValid { get; set; }
    }

    public class SupporterInfo
    {
        public string Email { get; set; }
        public string SupporterKey { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PlanType { get; set; }
        public bool IsActiveSupporter { get; set; }
        public bool IsExpiredSupporter { get; set; }
    }
}