using System;

namespace MediaBrowser.Model.Entities
{
    public class MBRegistrationRecord
    {
        public DateTime ExpirationDate { get; set; }
        public bool IsRegistered { get; set;}
        public bool RegChecked { get; set; }
        public bool RegError { get; set; }
        private bool? _isInTrial;
        public bool TrialVersion
        {
            get
            {
                if (_isInTrial == null)
                {
                    if (!RegChecked) return false; //don't set this until we've successfully obtained exp date
                    _isInTrial = ExpirationDate > DateTime.Now;
                }
                return (_isInTrial.Value && !IsRegistered);
            }
        }
        public bool IsValid
        {
            get { return !RegChecked || (IsRegistered || TrialVersion); }
        }
    }
}