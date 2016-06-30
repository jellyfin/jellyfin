using MediaBrowser.Model.Logging;
using System;
using System.Collections;
using System.Security.Cryptography;
using MediaBrowser.Server.Mono.Security;

namespace MediaBrowser.Server.Mono.Networking
{
    internal class CertificateGenerator
    {
        private const string MonoTestRootAgency = "<RSAKeyValue><Modulus>v/4nALBxCE+9JgEC0LnDUvKh6e96PwTpN4Rj+vWnqKT7IAp1iK/JjuqvAg6DQ2vTfv0dTlqffmHH51OyioprcT5nzxcSTsZb/9jcHScG0s3/FRIWnXeLk/fgm7mSYhjUaHNI0m1/NTTktipicjKxo71hGIg9qucCWnDum+Krh/k=</Modulus><Exponent>AQAB</Exponent><P>9jbKxMXEruW2CfZrzhxtull4O8P47+mNsEL+9gf9QsRO1jJ77C+jmzfU6zbzjf8+ViK+q62tCMdC1ZzulwdpXQ==</P><Q>x5+p198l1PkK0Ga2mRh0SIYSykENpY2aLXoyZD/iUpKYAvATm0/wvKNrE4dKJyPCA+y3hfTdgVag+SP9avvDTQ==</Q><DP>ISSjCvXsUfbOGG05eddN1gXxL2pj+jegQRfjpk7RAsnWKvNExzhqd5x+ZuNQyc6QH5wxun54inP4RTUI0P/IaQ==</DP><DQ>R815VQmR3RIbPqzDXzv5j6CSH6fYlcTiQRtkBsUnzhWmkd/y3XmamO+a8zJFjOCCx9CcjpVuGziivBqi65lVPQ==</DQ><InverseQ>iYiu0KwMWI/dyqN3RJYUzuuLj02/oTD1pYpwo2rvNCXU1Q5VscOeu2DpNg1gWqI+1RrRCsEoaTNzXB1xtKNlSw==</InverseQ><D>nIfh1LYF8fjRBgMdAH/zt9UKHWiaCnc+jXzq5tkR8HVSKTVdzitD8bl1JgAfFQD8VjSXiCJqluexy/B5SGrCXQ49c78NIQj0hD+J13Y8/E0fUbW1QYbhj6Ff7oHyhaYe1WOQfkp2t/h+llHOdt1HRf7bt7dUknYp7m8bQKGxoYE=</D></RSAKeyValue>";

        internal static void CreateSelfSignCertificatePfx(
            string fileName,
            string hostname,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            byte[] sn = Guid.NewGuid().ToByteArray();
            string subject = string.Format("CN={0}", hostname);
            string issuer = subject;
            DateTime notBefore = DateTime.Now.AddDays(-2);
            DateTime notAfter = DateTime.Now.AddYears(10);

            RSA issuerKey = RSA.Create();
            issuerKey.FromXmlString(MonoTestRootAgency);
            RSA subjectKey = RSA.Create();

            // serial number MUST be positive
            if ((sn[0] & 0x80) == 0x80)
                sn[0] -= 0x80;

            issuer = subject;
            issuerKey = subjectKey;

            X509CertificateBuilder cb = new X509CertificateBuilder(3);
            cb.SerialNumber = sn;
            cb.IssuerName = issuer;
            cb.NotBefore = notBefore;
            cb.NotAfter = notAfter;
            cb.SubjectName = subject;
            cb.SubjectPublicKey = subjectKey;

            // signature
            cb.Hash = "SHA256";
            byte[] rawcert = cb.Sign(issuerKey);

            PKCS12 p12 = new PKCS12();


            ArrayList list = new ArrayList();
            // we use a fixed array to avoid endianess issues 
            // (in case some tools requires the ID to be 1).
            list.Add(new byte[4] { 1, 0, 0, 0 });
            Hashtable attributes = new Hashtable(1);
            attributes.Add(PKCS9.localKeyId, list);

            p12.AddCertificate(new X509Certificate(rawcert), attributes);

            p12.AddPkcs8ShroudedKeyBag(subjectKey, attributes);
            p12.SaveToFile(fileName);
        }
    }
}
