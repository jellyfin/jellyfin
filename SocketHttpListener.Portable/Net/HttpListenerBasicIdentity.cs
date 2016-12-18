using System.Security.Principal;

namespace SocketHttpListener.Net
{
    public class HttpListenerBasicIdentity : GenericIdentity
    {
        string password;

        public HttpListenerBasicIdentity(string username, string password)
            : base(username, "Basic")
        {
            this.password = password;
        }

        public virtual string Password
        {
            get { return password; }
        }
    }

    public class GenericIdentity : IIdentity
    {
        private string m_name;
        private string m_type;

        public GenericIdentity(string name)
        {
            if (name == null)
                throw new System.ArgumentNullException("name");

            m_name = name;
            m_type = "";
        }

        public GenericIdentity(string name, string type)
        {
            if (name == null)
                throw new System.ArgumentNullException("name");
            if (type == null)
                throw new System.ArgumentNullException("type");

            m_name = name;
            m_type = type;
        }

        public virtual string Name
        {
            get
            {
                return m_name;
            }
        }

        public virtual string AuthenticationType
        {
            get
            {
                return m_type;
            }
        }

        public virtual bool IsAuthenticated
        {
            get
            {
                return !m_name.Equals("");
            }
        }
    }
}
