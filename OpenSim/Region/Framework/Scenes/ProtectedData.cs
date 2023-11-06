using System.IO;

namespace OpenSim.Region.Framework.Scenes
{
    public class ProtectedData
    {
        private string value;
        private string pass = "";

        public ProtectedData(string value, string pass)
        {
            this.value = value;
            this.pass = pass;
        }
        
        private ProtectedData(){}

        public string testAndGetValue(string pass)
        {
            if (!IsProtected) return value;
            else if (this.pass == pass) return value;
            else return "";
        }

        public bool IsProtected
        {
            get
            {
                return (pass != "");
            }
        }

        public string val
        {
            get
            {
                return value;
            }
        }

        public bool test(string pass)
        {
            if (!IsProtected) return true;
            else if (this.pass == pass) return true;
            else return false;
        }

        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(value);
                    bw.Write(pass);
                    return ms.ToArray();
                }
            }
        }

        public static ProtectedData deserialize(byte[] inf)
        {
            ProtectedData pd = new ProtectedData();
            using (MemoryStream ms = new MemoryStream(inf))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    pd.value = br.ReadString();
                    pd.pass = br.ReadString();

                    return pd;
                }
            }
        }
    }
}