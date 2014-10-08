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
}