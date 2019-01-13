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

        public virtual string Password => password;
    }

    public class GenericIdentity : IIdentity
    {
        private string m_name;
        private string m_type;

        public GenericIdentity(string name)
        {
            if (name == null)
                throw new System.ArgumentNullException(nameof(name));

            m_name = name;
            m_type = "";
        }

        public GenericIdentity(string name, string type)
        {
            if (name == null)
                throw new System.ArgumentNullException(nameof(name));
            if (type == null)
                throw new System.ArgumentNullException(nameof(type));

            m_name = name;
            m_type = type;
        }

        public virtual string Name => m_name;

        public virtual string AuthenticationType => m_type;

        public virtual bool IsAuthenticated => !m_name.Equals("");
    }
}
