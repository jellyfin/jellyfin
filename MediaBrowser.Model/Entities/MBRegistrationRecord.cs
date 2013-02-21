using System;

namespace Mediabrowser.Model.Entities
{
    public class MBRegistrationRecord
    {
        public DateTime ExpirationDate = DateTime.MinValue;
        public bool IsRegistered = false;
        public bool RegChecked = false;
        public bool RegError = false;
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