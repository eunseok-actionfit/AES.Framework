using System.Text;


namespace AES.Tools.Helpers
{
    public static class HexUtils
    {
        public static string ToHexString(params string[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            foreach (string s in inputs)
            {
                if (s == null) continue;
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                foreach (byte b in bytes)
                    sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }

        public static string ToHexStringWithDelimiter(string delimiter, params string[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(inputs[i]);
                    foreach (byte b in bytes)
                        sb.AppendFormat("{0:X2}", b);
                }

                if (i < inputs.Length - 1 && delimiter != null)
                {
                    byte[] dBytes = Encoding.UTF8.GetBytes(delimiter);
                    foreach (byte b in dBytes)
                        sb.AppendFormat("{0:X2}", b);
                }
            }

            return sb.ToString();
        }
    }
}